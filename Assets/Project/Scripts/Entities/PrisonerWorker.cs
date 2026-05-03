using System.Collections.Generic;
using PrisonLife.Controllers.PrisonerWorker;
using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using PrisonLife.Movement;
using PrisonLife.View.World;
using UnityEngine;
using UnityEngine.AI;

namespace PrisonLife.Entities
{
    /// <summary>
    /// 죄수 처리 일꾼 entity facade. NavMeshMover + PrisonerWorkerAI 합성.
    /// IInventoryHolder 라서 ResourceOutputZone/InputZone 이 자동으로 transfer 매개.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PrisonerWorker : MonoBehaviour, IInventoryHolder
    {
        [Header("Hand Stack Visual")]
        [SerializeField] private Transform handStackAnchor;
        [SerializeField] private Vector3 handcuffStackOffsetStep = new Vector3(0f, 0.18f, 0f);

        [Header("Capacity")]
        [SerializeField, Min(1)] private int handcuffCapacity = 10;

        [Header("Movement Tuning")]
        [SerializeField] private float rotationLerpRate = 15f;

        private NavMeshAgent navMeshAgent;
        private NavMeshMover mover;
        private InventoryModel inventory;
        private PrisonerWorkerAI ai;
        private StackVisualizer handcuffStackVisualizer;

        public InventoryModel Inventory => inventory;
        public Transform Transform => transform;
        public bool IsPlayerControlled => false;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updateRotation = false;
            navMeshAgent.updatePosition = true;
        }

        public void Init(
            Transform _containerOutputZoneTransform,
            StockpileModel _containerHandcuffStockpile,
            Transform _inputZoneTransform,
            StockpileModel _inputZoneBufferStockpile,
            Transform _processZoneTransform)
        {
            Dictionary<ResourceType, int> capacities = new Dictionary<ResourceType, int>
            {
                { ResourceType.Handcuff, handcuffCapacity },
            };
            inventory = new InventoryModel(capacities);

            mover = new NavMeshMover(navMeshAgent, transform, rotationLerpRate);

            ai = new PrisonerWorkerAI(
                this,
                mover,
                _containerOutputZoneTransform,
                _containerHandcuffStockpile,
                _inputZoneTransform,
                _inputZoneBufferStockpile,
                _processZoneTransform);

            SystemManager systemManager = SystemManager.Instance;
            PoolManager pool = systemManager != null ? systemManager.Pool : null;
            handcuffStackVisualizer = new StackVisualizer(
                inventory.ObserveCount(ResourceType.Handcuff),
                handStackAnchor,
                ResourceType.Handcuff,
                handcuffStackOffsetStep,
                pool);

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

        private void OnDestroy()
        {
            handcuffStackVisualizer?.Dispose();
            handcuffStackVisualizer = null;
        }

        private void ApplyRotationTowardsVelocity()
        {
            Vector3 velocity = navMeshAgent.velocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontal.sqrMagnitude < 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationLerpRate * Time.deltaTime);
        }
    }
}
