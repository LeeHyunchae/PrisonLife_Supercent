using System;
using PrisonLife.Controllers.Prisoner;
using PrisonLife.Models;
using PrisonLife.Movement;
using PrisonLife.Reactive;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace PrisonLife.Entities
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class Prisoner : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] Material waitingMaterial;
        [SerializeField] Material processedMaterial;
        [SerializeField] TMP_Text requiredCountLabel;

        [Header("Movement Tuning")]
        [SerializeField] float rotationLerpRate = 15f;

        NavMeshAgent navMeshAgent;
        PrisonerModel model;
        NavMeshMover mover;
        PrisonerSequencer sequencer;

        public event Action<Prisoner> FullyProcessed;
        public event Action<Prisoner> EnteredCell;

        public PrisonerModel Model => model;
        public bool CanReceiveHandcuff => sequencer != null && sequencer.CanReceiveHandcuff;

        void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updateRotation = false;
            navMeshAgent.updatePosition = true;
        }

        public void Init(int _requiredHandcuffs, Transform _initialQueueSlot, Transform _cellTransform)
        {
            model = new PrisonerModel(_requiredHandcuffs);
            mover = new NavMeshMover(navMeshAgent, transform, rotationLerpRate);
            sequencer = new PrisonerSequencer(model, mover, _initialQueueSlot, _cellTransform);

            sequencer.OnFullyProcessed += HandleFullyProcessed;
            sequencer.OnEnteredCell += HandleEnteredCell;

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

        void Update()
        {
            sequencer?.Tick(Time.deltaTime);

            if (navMeshAgent != null && navMeshAgent.hasPath)
            {
                ApplyRotationTowardsVelocity();
            }
        }

        void OnDestroy()
        {
            if (sequencer != null)
            {
                sequencer.OnFullyProcessed -= HandleFullyProcessed;
                sequencer.OnEnteredCell -= HandleEnteredCell;
            }
        }

        void HandleFullyProcessed()
        {
            FullyProcessed?.Invoke(this);
        }

        void HandleEnteredCell()
        {
            EnteredCell?.Invoke(this);
        }

        void UpdateBodyMaterial(PrisonerPhase _phase)
        {
            if (bodyRenderer == null) return;
            bool isProcessed = _phase == PrisonerPhase.WalkingToCell || _phase == PrisonerPhase.Inside;
            var target = isProcessed ? processedMaterial : waitingMaterial;
            if (target != null) bodyRenderer.material = target;
        }

        void UpdateRequiredCountLabel(int _received)
        {
            if (requiredCountLabel == null || model == null) return;
            requiredCountLabel.text = $"{_received} / {model.RequiredHandcuffs}";
        }

        void ApplyRotationTowardsVelocity()
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
