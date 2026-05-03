using System;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Controllers.Prisoner
{
    public class PrisonerSequencer
    {
        private readonly PrisonerModel model;
        private readonly IMover mover;
        private readonly IReadOnlyList<Transform> cellPathWaypoints;
        private Transform queueSlotTransform;
        private int currentCellWaypointIndex;
        private int incomingHandcuffsInFlight;

        public event Action OnFullyProcessed;
        public event Action OnEnteredCell;
        public event Action OnFirstArrivedAtQueue;

        private bool hasFiredFirstArrival;

        public bool CanReceiveHandcuff =>
            model.Phase.Value == PrisonerPhase.WaitingAtQueue && !model.IsFulfilled;

        public PrisonerSequencer(
            PrisonerModel _model,
            IMover _mover,
            Transform _initialQueueSlot,
            IReadOnlyList<Transform> _cellPathWaypoints)
        {
            model = _model;
            mover = _mover;
            queueSlotTransform = _initialQueueSlot;
            cellPathWaypoints = _cellPathWaypoints;

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
            incomingHandcuffsInFlight++;
            // 비행 도착(OnHandcuffArrived) 까지 phase 전환 보류 — 마지막 수갑 visual 이 도착해야 cell 로 출발.
        }

        public void OnHandcuffArrived()
        {
            if (incomingHandcuffsInFlight > 0) incomingHandcuffsInFlight--;
            TryStartWalkingToCell();
        }

        private void TryStartWalkingToCell()
        {
            if (model.Phase.Value != PrisonerPhase.WaitingAtQueue) return;
            if (!model.IsFulfilled) return;
            if (incomingHandcuffsInFlight > 0) return;

            model.Phase.Value = PrisonerPhase.WalkingToCell;
            currentCellWaypointIndex = 0;
            SetMoverToCurrentCellWaypoint();
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
                        if (!hasFiredFirstArrival)
                        {
                            hasFiredFirstArrival = true;
                            OnFirstArrivedAtQueue?.Invoke();
                        }
                    }
                    break;

                case PrisonerPhase.WalkingToCell:
                    if (mover.HasArrivedAtDestination)
                    {
                        AdvanceCellWaypoint();
                    }
                    break;
            }
        }

        private void AdvanceCellWaypoint()
        {
            currentCellWaypointIndex++;

            if (cellPathWaypoints == null || currentCellWaypointIndex >= cellPathWaypoints.Count)
            {
                model.Phase.Value = PrisonerPhase.Inside;
                OnEnteredCell?.Invoke();
                return;
            }

            SetMoverToCurrentCellWaypoint();
        }

        private void SetMoverToCurrentCellWaypoint()
        {
            if (cellPathWaypoints == null || cellPathWaypoints.Count == 0) return;
            if (currentCellWaypointIndex < 0 || currentCellWaypointIndex >= cellPathWaypoints.Count) return;

            Transform waypoint = cellPathWaypoints[currentCellWaypointIndex];
            if (waypoint == null) return;

            mover.SetDestination(waypoint.position);
        }
    }
}
