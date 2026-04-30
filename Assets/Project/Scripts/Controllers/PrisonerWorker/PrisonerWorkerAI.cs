using PrisonLife.Core;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Controllers.PrisonerWorker
{
    /// <summary>
    /// 죄수 처리 일꾼 자동화 FSM (POCO).
    /// 직접 transfer 는 없음. 워커가 IInventoryHolder 라서, 출력/처리 zone 의 트리거 로직이 자동으로
    /// 워커 인벤토리와 컨테이너 / head 죄수 사이를 매개한다.
    /// 워커 AI 는 이동 + 대기 + 떠나는 시점만 관리.
    /// </summary>
    public class PrisonerWorkerAI
    {
        private enum State
        {
            GoToOutput,
            Collecting,
            GoToProcessZone,
            Delivering,
        }

        private readonly IInventoryHolder holder;
        private readonly IMover mover;
        private readonly Transform handcuffOutputZoneTransform;
        private readonly Transform prisonerProcessZoneTransform;
        private readonly StockpileModel handcuffStockpile;

        private State state;

        public PrisonerWorkerAI(
            IInventoryHolder _holder,
            IMover _mover,
            Transform _handcuffOutputZoneTransform,
            Transform _prisonerProcessZoneTransform,
            StockpileModel _handcuffStockpile)
        {
            holder = _holder;
            mover = _mover;
            handcuffOutputZoneTransform = _handcuffOutputZoneTransform;
            prisonerProcessZoneTransform = _prisonerProcessZoneTransform;
            handcuffStockpile = _handcuffStockpile;
        }

        public void Start()
        {
            EnterGoToOutput();
        }

        public void Tick(float _deltaTime)
        {
            switch (state)
            {
                case State.GoToOutput: TickGoToOutput(); break;
                case State.Collecting: TickCollecting(); break;
                case State.GoToProcessZone: TickGoToProcessZone(); break;
                case State.Delivering: TickDelivering(); break;
            }
        }

        private void TickGoToOutput()
        {
            if (mover != null && mover.HasArrivedAtDestination)
            {
                state = State.Collecting;
            }
        }

        private void TickCollecting()
        {
            if (holder?.Inventory == null) return;

            // 만적 → 즉시 처리칸으로
            if (holder.Inventory.IsAtCapacity(ResourceType.Handcuff))
            {
                EnterGoToProcessZone();
                return;
            }

            // 컨테이너가 비었고 일부라도 들고 있으면 부분 적재로 출발 (계속 기다리지 않음)
            bool containerHasStock = handcuffStockpile != null && handcuffStockpile.HasStock;
            bool hasAnyHandcuff = holder.Inventory.GetCount(ResourceType.Handcuff) > 0;
            if (!containerHasStock && hasAnyHandcuff)
            {
                EnterGoToProcessZone();
            }
        }

        private void TickGoToProcessZone()
        {
            if (mover != null && mover.HasArrivedAtDestination)
            {
                state = State.Delivering;
            }
        }

        private void TickDelivering()
        {
            if (holder?.Inventory == null) return;

            // 다 비우면 출력 zone 으로 복귀
            if (holder.Inventory.GetCount(ResourceType.Handcuff) <= 0)
            {
                EnterGoToOutput();
            }
        }

        private void EnterGoToOutput()
        {
            if (mover != null && handcuffOutputZoneTransform != null)
            {
                mover.SetDestination(handcuffOutputZoneTransform.position);
            }
            state = State.GoToOutput;
        }

        private void EnterGoToProcessZone()
        {
            if (mover != null && prisonerProcessZoneTransform != null)
            {
                mover.SetDestination(prisonerProcessZoneTransform.position);
            }
            state = State.GoToProcessZone;
        }
    }
}
