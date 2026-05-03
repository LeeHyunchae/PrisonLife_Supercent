using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Facilities;
using UnityEngine;

namespace PrisonLife.Controllers.Miner
{
    /// <summary>
    /// 광부 일꾼 자동 채굴 FSM (POCO). MinerWorker Mono 가 owning + Tick 위임.
    /// 광부는 광석을 직접 적재하지 않고, 채굴 성공 즉시 외부 sink (HandcuffContainer 의 ore stockpile) 로 1개 흘려보낸다.
    /// </summary>
    public class MinerWorkerAI
    {
        // 가장 가까운 N 개 후보 중 무작위 picking — 광부 다수가 동시에 같은 광석으로 몰리는 시각적 어색함 완화.
        private const int ClosestCandidatePoolSize = 3;

        private enum State
        {
            Wait,
            MoveToMine,
            Mining,
            SendOre,
        }

        private readonly IMover mover;
        private readonly IReadOnlyList<MineableRock> rockPool;
        private readonly IResourceSink oreSink;
        private readonly float miningDurationSeconds;
        private readonly float waitDurationSeconds;

        // 정렬용 재사용 버퍼 (매 picking 마다 new 안 하게 GC 절약).
        private readonly List<(MineableRock Rock, float SqrDistance)> closestCandidatesBuffer = new();

        private State state;
        private MineableRock targetRock;
        private float stateTimer;

        public MinerWorkerAI(
            IMover _mover,
            IReadOnlyList<MineableRock> _rockPool,
            IResourceSink _oreSink,
            float _miningDurationSeconds = 0.7f,
            float _waitDurationSeconds = 0.3f)
        {
            mover = _mover;
            rockPool = _rockPool;
            oreSink = _oreSink;
            miningDurationSeconds = _miningDurationSeconds;
            waitDurationSeconds = _waitDurationSeconds;
        }

        public void Start()
        {
            // 즉시 다음 Tick 에서 광석을 picking 하도록 Wait 만료 상태로 시작
            state = State.Wait;
            stateTimer = waitDurationSeconds;
            targetRock = null;
        }

        public void Tick(float _deltaTime)
        {
            switch (state)
            {
                case State.Wait: TickWait(_deltaTime); break;
                case State.MoveToMine: TickMoveToMine(); break;
                case State.Mining: TickMining(_deltaTime); break;
                case State.SendOre: TickSendOre(); break;
            }
        }

        private void TickWait(float _deltaTime)
        {
            stateTimer += _deltaTime;
            if (stateTimer < waitDurationSeconds) return;

            MineableRock rock = PickClosestAvailableRock();
            if (rock == null)
            {
                stateTimer = 0f;
                return;
            }

            targetRock = rock;
            if (mover != null) mover.SetDestination(targetRock.transform.position);
            state = State.MoveToMine;
        }

        private void TickMoveToMine()
        {
            if (targetRock == null || !targetRock.IsAvailableForMining.Value)
            {
                EnterWait();
                return;
            }

            if (mover != null && mover.HasArrivedAtDestination)
            {
                stateTimer = 0f;
                state = State.Mining;
            }
        }

        private void TickMining(float _deltaTime)
        {
            if (targetRock == null || !targetRock.IsAvailableForMining.Value)
            {
                EnterWait();
                return;
            }

            stateTimer += _deltaTime;
            if (stateTimer < miningDurationSeconds) return;

            if (targetRock.TryDeplete())
            {
                state = State.SendOre;
            }
            else
            {
                EnterWait();
            }
        }

        private void TickSendOre()
        {
            if (oreSink != null && oreSink.CanAcceptOne())
            {
                oreSink.TryAcceptOne();
            }
            // sink 공간 없으면 그냥 버림 (스펙상 광부는 광석을 적재하지 않음)
            EnterWait();
        }

        private void EnterWait()
        {
            targetRock = null;
            stateTimer = 0f;
            state = State.Wait;
        }

        private MineableRock PickClosestAvailableRock()
        {
            if (rockPool == null || rockPool.Count == 0) return null;

            Vector3 currentPosition = mover != null ? mover.CurrentPosition : Vector3.zero;

            closestCandidatesBuffer.Clear();
            for (int i = 0; i < rockPool.Count; i++)
            {
                MineableRock candidate = rockPool[i];
                if (candidate == null) continue;
                if (!candidate.IsAvailableForMining.Value) continue;

                float sqrDistanceToCandidate = (candidate.transform.position - currentPosition).sqrMagnitude;
                closestCandidatesBuffer.Add((candidate, sqrDistanceToCandidate));
            }

            if (closestCandidatesBuffer.Count == 0) return null;

            // 가까운 순 정렬 후 상위 N 개 중 무작위 — 여러 광부가 동시에 같은 광석으로 몰리는 현상 완화.
            closestCandidatesBuffer.Sort(CompareByDistanceAscending);

            int candidatePoolSize = Mathf.Min(ClosestCandidatePoolSize, closestCandidatesBuffer.Count);
            int pickIndex = Random.Range(0, candidatePoolSize);
            return closestCandidatesBuffer[pickIndex].Rock;
        }

        private static int CompareByDistanceAscending(
            (MineableRock Rock, float SqrDistance) _a,
            (MineableRock Rock, float SqrDistance) _b)
        {
            return _a.SqrDistance.CompareTo(_b.SqrDistance);
        }
    }
}
