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
        private enum State
        {
            Wait,
            MoveToMine,
            Mining,
            SendOre,
        }

        private readonly IMover mover;
        private readonly IList<MineableRock> rockPool;
        private readonly IResourceSink oreSink;
        private readonly float miningDurationSeconds;
        private readonly float waitDurationSeconds;

        private State state;
        private MineableRock targetRock;
        private float stateTimer;

        public MinerWorkerAI(
            IMover _mover,
            IList<MineableRock> _rockPool,
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

            var rock = PickRandomAvailableRock();
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

        private MineableRock PickRandomAvailableRock()
        {
            if (rockPool == null || rockPool.Count == 0) return null;

            int startIndex = Random.Range(0, rockPool.Count);
            for (int i = 0; i < rockPool.Count; i++)
            {
                int idx = (startIndex + i) % rockPool.Count;
                var rock = rockPool[idx];
                if (rock != null && rock.IsAvailableForMining.Value) return rock;
            }
            return null;
        }
    }
}
