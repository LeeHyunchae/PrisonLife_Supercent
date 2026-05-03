using PrisonLife.Core;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Controllers.PrisonerWorker
{
    /// <summary>
    /// 죄수 처리 일꾼 자동화 FSM (POCO).
    /// 워커가 IInventoryHolder 라서 zone 트리거가 자동 transfer.
    /// 흐름: 컨테이너 출력 → 손에 수갑 적재 → InputZone 으로 가서 buffer 에 적재 → ProcessZone 으로 가서 buffer drain → 컨테이너 복귀.
    ///
    /// 6 state:
    ///   GoToContainerOutput → 도착 → Collecting
    ///   Collecting        → 손 가득 또는 컨테이너 비었고 손에 일부 있음 → GoToInputZone
    ///   GoToInputZone     → 도착 → Depositing
    ///   Depositing        → 손 비거나 buffer 가 가득 차서 더 못 적재 → GoToProcessZone
    ///   GoToProcessZone   → 도착 → Draining
    ///   Draining          → buffer + 손 모두 빔 → GoToContainerOutput
    ///
    /// Draining 안전망: 진행 변화가 일정 시간 없으면 stuck 으로 간주하고 우회 — sink (head 죄수)
    /// 의 in-flight 카운트나 phase 에 묶여 ProcessZone 에서 영구 대기하는 deadlock 방어.
    /// </summary>
    public class PrisonerWorkerAI
    {
        private const float DrainStuckTimeoutSeconds = 8f;

        private enum State
        {
            GoToContainerOutput,
            Collecting,
            GoToInputZone,
            Depositing,
            GoToProcessZone,
            Draining,
        }

        private readonly IInventoryHolder holder;
        private readonly IMover mover;

        private readonly Transform containerOutputZoneTransform;
        private readonly StockpileModel containerHandcuffStockpile;

        private readonly Transform inputZoneTransform;
        private readonly StockpileModel inputZoneBufferStockpile;

        private readonly Transform processZoneTransform;

        private State state;

        private float drainStuckAccumulator;
        private int lastDrainProgressSnapshot;

        public PrisonerWorkerAI(
            IInventoryHolder _holder,
            IMover _mover,
            Transform _containerOutputZoneTransform,
            StockpileModel _containerHandcuffStockpile,
            Transform _inputZoneTransform,
            StockpileModel _inputZoneBufferStockpile,
            Transform _processZoneTransform)
        {
            holder = _holder;
            mover = _mover;
            containerOutputZoneTransform = _containerOutputZoneTransform;
            containerHandcuffStockpile = _containerHandcuffStockpile;
            inputZoneTransform = _inputZoneTransform;
            inputZoneBufferStockpile = _inputZoneBufferStockpile;
            processZoneTransform = _processZoneTransform;
        }

        public void Start()
        {
            EnterGoToContainerOutput();
        }

        public void Tick(float _deltaTime)
        {
            switch (state)
            {
                case State.GoToContainerOutput: TickArrival(State.Collecting); break;
                case State.Collecting: TickCollecting(); break;
                case State.GoToInputZone: TickArrival(State.Depositing); break;
                case State.Depositing: TickDepositing(); break;
                case State.GoToProcessZone: TickArrival(State.Draining); break;
                case State.Draining: TickDraining(_deltaTime); break;
            }
        }

        private void TickArrival(State _onArrival)
        {
            if (mover != null && mover.HasArrivedAtDestination)
            {
                state = _onArrival;
            }
        }

        private void TickCollecting()
        {
            if (holder?.Inventory == null) return;

            if (holder.Inventory.IsAtCapacity(ResourceType.Handcuff))
            {
                EnterGoToInputZone();
                return;
            }

            // 컨테이너 비고 손에 일부라도 있으면 부분 적재로 출발
            bool containerHasStock = containerHandcuffStockpile != null && containerHandcuffStockpile.HasStock;
            bool hasAnyHandcuff = holder.Inventory.GetCount(ResourceType.Handcuff) > 0;
            if (!containerHasStock && hasAnyHandcuff)
            {
                EnterGoToInputZone();
            }
        }

        private void TickDepositing()
        {
            if (holder?.Inventory == null) return;

            // 손 비면 적재 완료 → ProcessZone 으로 이동해서 drain 감시
            if (holder.Inventory.GetCount(ResourceType.Handcuff) <= 0)
            {
                EnterGoToProcessZone();
                return;
            }

            // buffer 가 가득 차서 더 못 적재 → 손에 든 채로 ProcessZone 으로 (priority 로 head 에 직접 줄 것)
            bool bufferHasSpace = inputZoneBufferStockpile != null && inputZoneBufferStockpile.HasSpace;
            if (!bufferHasSpace)
            {
                EnterGoToProcessZone();
            }
        }

        private void TickDraining(float _deltaTime)
        {
            if (holder?.Inventory == null) return;

            int currentHandCount = holder.Inventory.GetCount(ResourceType.Handcuff);
            bool bufferEmpty = inputZoneBufferStockpile == null || !inputZoneBufferStockpile.HasStock;

            // 정상 종료 — 손/buffer 모두 비면 컨테이너로 복귀 (다 쓸 때까지 ProcessZone 에 머무는 게 의도된 패턴).
            if (currentHandCount <= 0 && bufferEmpty)
            {
                drainStuckAccumulator = 0f;
                EnterGoToContainerOutput();
                return;
            }

            // stuck 감지 — drain 진행 변화 없는 시간 누적.
            // hand + (buffer 보유 여부) 조합으로 단순 진행 스냅샷을 만들고, 변화 없으면 sink 가 막힌 것으로 본다.
            int currentProgressSnapshot = currentHandCount + (bufferEmpty ? 0 : 1);
            if (currentProgressSnapshot != lastDrainProgressSnapshot)
            {
                drainStuckAccumulator = 0f;
                lastDrainProgressSnapshot = currentProgressSnapshot;
            }
            else
            {
                drainStuckAccumulator += _deltaTime;
            }

            if (drainStuckAccumulator < DrainStuckTimeoutSeconds) return;

            // timeout 도달 — deadlock 방어 우회 경로.
            drainStuckAccumulator = 0f;
            if (currentHandCount > 0)
            {
                // 손에 들고 있는 채 막혔으면 InputZone 으로 가서 buffer 적재 시도.
                EnterGoToInputZone();
            }
            else
            {
                // 손 비고 buffer 만 남았는데 처리 안 되면 컨테이너로 돌아가 다른 사이클 시작.
                EnterGoToContainerOutput();
            }
        }

        private void EnterGoToContainerOutput()
        {
            if (mover != null && containerOutputZoneTransform != null)
            {
                mover.SetDestination(containerOutputZoneTransform.position);
            }
            state = State.GoToContainerOutput;
        }

        private void EnterGoToInputZone()
        {
            if (mover != null && inputZoneTransform != null)
            {
                mover.SetDestination(inputZoneTransform.position);
            }
            state = State.GoToInputZone;
        }

        private void EnterGoToProcessZone()
        {
            // ProcessZone 에 새로 진입하므로 stuck 추적 reset.
            drainStuckAccumulator = 0f;
            lastDrainProgressSnapshot = -1;

            if (mover != null && processZoneTransform != null)
            {
                mover.SetDestination(processZoneTransform.position);
            }
            state = State.GoToProcessZone;
        }
    }
}
