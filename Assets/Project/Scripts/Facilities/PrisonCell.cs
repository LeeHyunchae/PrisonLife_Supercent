using System;
using System.Collections.Generic;
using PrisonLife.Entities;
using PrisonLife.Game;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 감옥 본체. 초기 슬롯 수 만큼 PrisonStateModel.MaxInmateCapacity 를 세팅.
    /// AdmitPrisoner 는 죄수를 grid 위치에 배치 + AI/agent 정지. 확장 시 capacity 증가 + visual swap.
    /// grid 는 cols 고정, rows 무한 확장. 인덱스가 늘어나면 +Z 방향으로 줄이 추가됨.
    /// 슬롯 위치는 자기 transform 을 원점으로 col 중앙 정렬, row 는 0행이 -((initialRows-1)/2) 위치에서 시작.
    /// </summary>
    public class PrisonCell : MonoBehaviour
    {
        [Header("Capacity")]
        [SerializeField, Min(1)] private int initialCapacity = 20;

        [Header("Path")]
        [SerializeField] private Transform[] cellPathWaypoints;

        [Header("Grid Layout")]
        [SerializeField, Min(1)] private int gridCols = 5;
        [SerializeField, Min(1)] private int initialGridRows = 4;
        [SerializeField] private Vector2 cellSpacing = new Vector2(0.6f, 0.6f);

        [Header("Visual Swap")]
        [SerializeField] private GameObject beforeExpansionVisual;
        [SerializeField] private GameObject afterExpansionVisual;

        [Header("UI")]
        [SerializeField] private TMP_Text inmateCountLabel;

        private IDisposable currentInmateCountSubscription;
        private IDisposable maxInmateCapacitySubscription;

        public IReadOnlyList<Transform> CellPathWaypoints => cellPathWaypoints;

        private void Awake()
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.Prison == null)
            {
                Debug.LogError("[PrisonCell] SystemManager / PrisonStateModel 미초기화 상태로 동작 불가.");
                return;
            }

            systemManager.Prison.MaxInmateCapacity.Value = initialCapacity;
            ApplyExpansionVisual(false);

            if (inmateCountLabel != null)
            {
                currentInmateCountSubscription = systemManager.Prison.CurrentInmateCount
                    .Subscribe(_ => UpdateInmateCountLabel());
                maxInmateCapacitySubscription = systemManager.Prison.MaxInmateCapacity
                    .Subscribe(_ => UpdateInmateCountLabel());
            }
        }

        private void OnDestroy()
        {
            currentInmateCountSubscription?.Dispose();
            currentInmateCountSubscription = null;
            maxInmateCapacitySubscription?.Dispose();
            maxInmateCapacitySubscription = null;
        }

        private void UpdateInmateCountLabel()
        {
            if (inmateCountLabel == null) return;
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.Prison == null) return;

            int current = systemManager.Prison.CurrentInmateCount.Value;
            int max = systemManager.Prison.MaxInmateCapacity.Value;
            inmateCountLabel.text = $"{current} / {max}";
        }

        public bool AdmitPrisoner(Prisoner _prisoner)
        {
            if (_prisoner == null) return false;

            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.Prison == null) return false;

            int slotIndex = systemManager.Prison.CurrentInmateCount.Value;
            if (!systemManager.Prison.TryAdmitOne()) return false;

            DisableMovement(_prisoner);
            PlaceAtSlot(_prisoner, slotIndex);
            return true;
        }

        public void Expand(int _additionalSlots)
        {
            SystemManager systemManager = SystemManager.Instance;
            if (systemManager == null || systemManager.Prison == null) return;
            systemManager.Prison.IncreaseCapacity(_additionalSlots);
            ApplyExpansionVisual(true);
        }

        private void ApplyExpansionVisual(bool _expanded)
        {
            if (beforeExpansionVisual != null) beforeExpansionVisual.SetActive(!_expanded);
            if (afterExpansionVisual != null) afterExpansionVisual.SetActive(_expanded);
        }

        private void DisableMovement(Prisoner _prisoner)
        {
            NavMeshAgent agent = _prisoner.GetComponent<NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            // Prisoner.Update 의 sequencer.Tick 정지 — Phase 가 Inside 라 무해하지만 명확히 비활성.
            _prisoner.enabled = false;
        }

        private void PlaceAtSlot(Prisoner _prisoner, int _slotIndex)
        {
            if (_slotIndex < 0) return;

            int col = _slotIndex % gridCols;
            int row = _slotIndex / gridCols;

            // col 은 중앙 정렬, row 는 초기 그리드의 0행이 중앙 정렬되도록 offset.
            // 인덱스가 initialGridRows 를 넘어가면 자연스럽게 +Z 방향으로 줄이 추가됨.
            float localX = (col - (gridCols - 1) * 0.5f) * cellSpacing.x;
            float localZ = (row - (initialGridRows - 1) * 0.5f) * cellSpacing.y;

            Vector3 localOffset = new Vector3(localX, 0f, localZ);
            Vector3 worldPosition = transform.TransformPoint(localOffset);
            _prisoner.transform.SetPositionAndRotation(worldPosition, transform.rotation);
        }
    }
}
