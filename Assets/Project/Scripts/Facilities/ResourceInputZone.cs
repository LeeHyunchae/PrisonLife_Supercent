using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 자원 보유자 → 시설 방향 트리거 존. transferIntervalSeconds 마다 1개 이동.
    /// source 측 즉시 차감 + 비행 시각효과 → 도착 시 sink 가 +1 (ItemFlowManager.Fly).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ResourceInputZone : MonoBehaviour
    {
        [Header("Resource")]
        [SerializeField] private ResourceType resourceType;
        [SerializeField, Min(0.02f)] private float transferIntervalSeconds = 0.1f;

        [Header("Flight Effect")]
        [SerializeField, Min(0.05f)] private float flightDurationSeconds = 0.25f;
        [SerializeField, Min(0f)] private float flightArcHeight = 1.0f;
        [SerializeField] private Vector3 holderFlightOriginOffset = new Vector3(0f, 1.0f, 0f);

        [Header("Sound")]
        [SerializeField] private SoundType depositSoundType = SoundType.None;

        // 자원 종류별 flight throttle — Money 는 5원당 1지폐, 그 외는 1:1.
        private int FlightUnitInterval => resourceType == ResourceType.Money ? GameValueConstants.MoneyValuePerItem : 1;

        // Money 자원은 1원씩 빠르게 빠지는 효과를 위해 짧은 주기 사용.
        private float ResolvedTransferIntervalSeconds => resourceType == ResourceType.Money
            ? GameValueConstants.MoneyTransferIntervalSeconds
            : transferIntervalSeconds;

        private BoxCollider triggerBoxCollider;
        private Rigidbody kinematicRigidbody;
        private IResourceSink sink;

        private readonly HashSet<IInventoryHolder> holdersInZone = new();
        private readonly Dictionary<Collider, IInventoryHolder> colliderToHolderCache = new();

        private float transferAccumulator;
        private int flightAccumulator;

        private void Awake()
        {
            triggerBoxCollider = GetComponent<BoxCollider>();
            triggerBoxCollider.isTrigger = true;

            kinematicRigidbody = GetComponent<Rigidbody>();
            kinematicRigidbody.isKinematic = true;
            kinematicRigidbody.useGravity = false;
        }

        public void Init(IResourceSink _sink)
        {
            if (_sink == null)
            {
                sink = null;
                return;
            }

            if (_sink.InputType != resourceType)
            {
                Debug.LogError(
                    $"[ResourceInputZone] {name}: 자원 타입 불일치 — zone={resourceType}, sink={_sink.InputType}.");
                sink = null;
                return;
            }

            sink = _sink;
        }

        private void OnTriggerEnter(Collider _other)
        {
            IInventoryHolder holder = ResolveInventoryHolder(_other);
            if (holder == null) return;
            holdersInZone.Add(holder);
        }

        private void OnTriggerExit(Collider _other)
        {
            if (!colliderToHolderCache.TryGetValue(_other, out IInventoryHolder holder)) return;
            holdersInZone.Remove(holder);
            colliderToHolderCache.Remove(_other);
        }

        private void Update()
        {
            if (sink == null) return;
            if (holdersInZone.Count == 0) return;

            transferAccumulator += Time.deltaTime;
            if (transferAccumulator < ResolvedTransferIntervalSeconds) return;
            transferAccumulator = 0f;

            foreach (IInventoryHolder holder in holdersInZone)
            {
                if (holder == null) continue;
                InventoryModel inventory = holder.Inventory;
                if (inventory == null) continue;
                if (inventory.GetCount(resourceType) <= 0) continue;
                if (!sink.CanAcceptOne()) return;

                if (!inventory.TryRemove(resourceType, 1)) continue;
                sink.TryAcceptOne();

                flightAccumulator++;
                if (flightAccumulator >= FlightUnitInterval)
                {
                    flightAccumulator = 0;
                    PlayDepositSound();
                    LaunchFlightDecoration(holder);
                }
                return;
            }
        }

        private void LaunchFlightDecoration(IInventoryHolder _holder)
        {
            // model 업데이트는 즉시 완료한 상태 — 비행은 순수 시각 효과 (도착 시점 부수효과 없음).
            ItemFlowManager flow = SystemManager.Instance != null ? SystemManager.Instance.ItemFlow : null;
            if (flow == null) return;

            Vector3 fromPosition = _holder.Transform != null
                ? _holder.Transform.position + holderFlightOriginOffset
                : transform.position;
            Vector3 toPosition = transform.position;

            flow.Fly(
                resourceType,
                fromPosition,
                toPosition,
                flightDurationSeconds,
                flightArcHeight,
                null);
        }

        private IInventoryHolder ResolveInventoryHolder(Collider _collider)
        {
            if (_collider == null) return null;
            if (colliderToHolderCache.TryGetValue(_collider, out IInventoryHolder cached)) return cached;

            IInventoryHolder direct = _collider.GetComponent<IInventoryHolder>();
            IInventoryHolder holder = direct != null ? direct : _collider.GetComponentInParent<IInventoryHolder>();
            colliderToHolderCache[_collider] = holder;
            return holder;
        }

        private void PlayDepositSound()
        {
            if (depositSoundType == SoundType.None) return;
            SoundManager sound = SystemManager.Instance != null ? SystemManager.Instance.Sound : null;
            sound?.PlayOneShot(depositSoundType);
        }
    }
}
