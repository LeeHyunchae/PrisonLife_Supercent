using System.Collections.Generic;
using PrisonLife.Controllers.PrisonerWorker;
using PrisonLife.Core;
using PrisonLife.Game;
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
        [SerializeField, Min(1)] private int handcuffCapacity = 6;

        [Header("Movement Tuning")]
        [SerializeField] private float rotationLerpRate = 15f;

        private NavMeshAgent navMeshAgent;
        private NavMeshMover mover;
        private InventoryModel inventory;
        private PrisonerWorkerAI ai;
        private StackVisualizer handcuffStackVisualizer;

        public InventoryModel Inventory => inventory;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updateRotation = false;
            navMeshAgent.updatePosition = true;
        }

        public void Init(
            Transform _handcuffOutputZoneTransform,
            Transform _prisonerProcessZoneTransform,
            StockpileModel _handcuffStockpile)
        {
            var capacities = new Dictionary<ResourceType, int>
            {
                { ResourceType.Handcuff, handcuffCapacity },
            };
            inventory = new InventoryModel(capacities);

            mover = new NavMeshMover(navMeshAgent, transform, rotationLerpRate);

            ai = new PrisonerWorkerAI(
                this,
                mover,
                _handcuffOutputZoneTransform,
                _prisonerProcessZoneTransform,
                _handcuffStockpile);

            var registry = SystemManager.Instance != null ? SystemManager.Instance.ResourceItems : null;
            handcuffStackVisualizer = new StackVisualizer(
                inventory.ObserveCount(ResourceType.Handcuff),
                handStackAnchor,
                registry != null ? registry.GetPrefab(ResourceType.Handcuff) : null,
                handcuffStackOffsetStep);

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
