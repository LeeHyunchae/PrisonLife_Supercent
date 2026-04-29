using System;
using PrisonLife.Core;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Controllers.Prisoner
{
    public class PrisonerSequencer
    {
        readonly PrisonerModel model;
        readonly IMover mover;
        Transform queueSlotTransform;
        Transform cellTransform;

        public event Action OnFullyProcessed;
        public event Action OnEnteredCell;

        public bool CanReceiveHandcuff =>
            model.Phase.Value == PrisonerPhase.WaitingAtQueue && !model.IsFulfilled;

        public PrisonerSequencer(
            PrisonerModel _model,
            IMover _mover,
            Transform _initialQueueSlot,
            Transform _cellTransform)
        {
            model = _model;
            mover = _mover;
            queueSlotTransform = _initialQueueSlot;
            cellTransform = _cellTransform;

            model.Phase.Value = PrisonerPhase.WalkingToQueue;
            if (queueSlotTransform != null) mover.SetDestination(queueSlotTransform.position);
        }

        public void AssignQueueSlot(Transform _newSlot)
        {
            queueSlotTransform = _newSlot;
            if (queueSlotTransform == null) return;

            if (model.Phase.Value == PrisonerPhase.WaitingAtQueue ||
                model.Phase.Value == PrisonerPhase.WalkingToQueue)
            {
                model.Phase.Value = PrisonerPhase.WalkingToQueue;
                mover.SetDestination(queueSlotTransform.position);
            }
        }

        public void ReceiveOneHandcuff()
        {
            if (!CanReceiveHandcuff) return;
            model.ReceivedHandcuffs.Value++;
            if (!model.IsFulfilled) return;

            model.Phase.Value = PrisonerPhase.WalkingToCell;
            if (cellTransform != null) mover.SetDestination(cellTransform.position);
            OnFullyProcessed?.Invoke();
        }

        public void Tick(float _deltaTime)
        {
            if (mover == null) return;

            switch (model.Phase.Value)
            {
                case PrisonerPhase.WalkingToQueue:
                    if (mover.HasArrivedAtDestination)
                    {
                        model.Phase.Value = PrisonerPhase.WaitingAtQueue;
                    }
                    break;

                case PrisonerPhase.WalkingToCell:
                    if (mover.HasArrivedAtDestination)
                    {
                        model.Phase.Value = PrisonerPhase.Inside;
                        OnEnteredCell?.Invoke();
                    }
                    break;
            }
        }
    }
}
