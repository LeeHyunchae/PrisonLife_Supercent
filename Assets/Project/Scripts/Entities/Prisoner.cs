using System;
using System.Collections.Generic;
using PrisonLife.Controllers.Prisoner;
using PrisonLife.Models;
using PrisonLife.Movement;
using PrisonLife.Reactive;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace PrisonLife.Entities
{
    /// <summary>
    /// 죄수 엔티티. NavMeshAgent 기반 이동.
    /// spawn → 큐 슬롯 → (수갑 받음) → 셀 경로 waypoint 순서 따라 이동 → cell admit.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Prisoner : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Material waitingMaterial;
        [SerializeField] private Material processedMaterial;
        [SerializeField] private TMP_Text requiredCountLabel;

        [Header("Movement Tuning")]
        [SerializeField] private float rotationLerpRate = 15f;

        private NavMeshAgent navMeshAgent;
        private PrisonerModel model;
        private NavMeshMover mover;
        private PrisonerSequencer sequencer;

        public event Action<Prisoner> FullyProcessed;
        public event Action<Prisoner> EnteredCell;
        public event Action<Prisoner> FirstArrivedAtQueue;

        public PrisonerModel Model => model;
        public bool CanReceiveHandcuff => sequencer != null && sequencer.CanReceiveHandcuff;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updateRotation = false;
            navMeshAgent.updatePosition = true;
        }

        public void Init(int _requiredHandcuffs, Transform _initialQueueSlot, IReadOnlyList<Transform> _cellPathWaypoints)
        {
            model = new PrisonerModel(_requiredHandcuffs);
            mover = new NavMeshMover(navMeshAgent, transform, rotationLerpRate);
            sequencer = new PrisonerSequencer(model, mover, _initialQueueSlot, _cellPathWaypoints);

            sequencer.OnFullyProcessed += HandleFullyProcessed;
            sequencer.OnEnteredCell += HandleEnteredCell;
            sequencer.OnFirstArrivedAtQueue += HandleFirstArrivedAtQueue;

            model.Phase
                .Subscribe(UpdateBodyMaterial)
                .AddTo(this);

            model.ReceivedHandcuffs
                .Subscribe(UpdateRequiredCountLabel)
                .AddTo(this);
        }

        public void AssignQueueSlot(Transform _newSlot)
        {
            sequencer?.AssignQueueSlot(_newSlot);
        }

        public void ReceiveOneHandcuff()
        {
            sequencer?.ReceiveOneHandcuff();
        }

        public void OnHandcuffArrived()
        {
            sequencer?.OnHandcuffArrived();
        }

        private void Update()
        {
            sequencer?.Tick(Time.deltaTime);

            if (navMeshAgent != null && navMeshAgent.hasPath)
            {
                ApplyRotationTowardsVelocity();
            }
        }

        private void OnDestroy()
        {
            if (sequencer != null)
            {
                sequencer.OnFullyProcessed -= HandleFullyProcessed;
                sequencer.OnEnteredCell -= HandleEnteredCell;
                sequencer.OnFirstArrivedAtQueue -= HandleFirstArrivedAtQueue;
            }
        }

        private void HandleFullyProcessed()
        {
            FullyProcessed?.Invoke(this);
        }

        private void HandleEnteredCell()
        {
            EnteredCell?.Invoke(this);
        }

        private void HandleFirstArrivedAtQueue()
        {
            FirstArrivedAtQueue?.Invoke(this);
        }

        private void UpdateBodyMaterial(PrisonerPhase _phase)
        {
            if (bodyRenderer == null) return;
            bool isProcessed = _phase == PrisonerPhase.WalkingToCell || _phase == PrisonerPhase.Inside;
            Material target = isProcessed ? processedMaterial : waitingMaterial;
            if (target != null) bodyRenderer.material = target;
        }

        private void UpdateRequiredCountLabel(int _received)
        {
            if (requiredCountLabel == null || model == null) return;
            requiredCountLabel.text = $"{_received} / {model.RequiredHandcuffs}";
            requiredCountLabel.gameObject.SetActive(_received < model.RequiredHandcuffs);
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
