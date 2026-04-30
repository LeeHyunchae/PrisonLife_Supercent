using System.Collections.Generic;
using PrisonLife.Controllers.Miner;
using PrisonLife.Core;
using PrisonLife.Facilities;
using PrisonLife.Movement;
using UnityEngine;
using UnityEngine.AI;

namespace PrisonLife.Entities
{
    /// <summary>
    /// 광부 일꾼 엔티티 facade. NavMeshMover + MinerWorkerAI 합성.
    /// MinerHirePurchaseZone 이 Init 으로 광석 풀과 컨테이너 ore sink 를 주입.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class MinerWorker : MonoBehaviour
    {
        [Header("Movement Tuning")]
        [SerializeField] private float rotationLerpRate = 15f;

        [Header("AI Tuning")]
        [SerializeField, Min(0.05f)] private float miningDurationSeconds = 0.7f;
        [SerializeField, Min(0.05f)] private float waitDurationSeconds = 0.3f;

        private NavMeshAgent navMeshAgent;
        private NavMeshMover mover;
        private MinerWorkerAI ai;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updateRotation = false;
            navMeshAgent.updatePosition = true;
        }

        public void Init(IList<MineableRock> _rockPool, IResourceSink _oreSink)
        {
            mover = new NavMeshMover(navMeshAgent, transform, rotationLerpRate);
            ai = new MinerWorkerAI(mover, _rockPool, _oreSink, miningDurationSeconds, waitDurationSeconds);
            ai.Start();
        }

        private void Update()
        {
            ai?.Tick(Time.deltaTime);

            if (navMeshAgent != null && navMeshAgent.hasPath)
            {
                ApplyRotationTowardsVelocity();
            }
        }

        private void ApplyRotationTowardsVelocity()
        {
            var velocity = navMeshAgent.velocity;
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontal.sqrMagnitude < 0.0001f) return;

            var targetRotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationLerpRate * Time.deltaTime);
        }
    }
}
