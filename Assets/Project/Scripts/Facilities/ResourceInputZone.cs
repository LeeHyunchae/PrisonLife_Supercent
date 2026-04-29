using System.Collections.Generic;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 자원 보유자 → 시설 방향 트리거 존. transferIntervalSeconds 마다 1개 이동.
    /// 시설 측은 IResourceSink 를 통해 receive 한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ResourceInputZone : MonoBehaviour
    {
        [SerializeField] ResourceType resourceType;
        [SerializeField, Min(0.02f)] float transferIntervalSeconds = 0.1f;

        BoxCollider triggerBoxCollider;
        Rigidbody kinematicRigidbody;
        IResourceSink sink;

        readonly HashSet<IInventoryHolder> holdersInZone = new();
        readonly Dictionary<Collider, IInventoryHolder> colliderToHolderCache = new();

        float transferAccumulator;

        void Awake()
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

        void OnTriggerEnter(Collider _other)
        {
            var holder = ResolveInventoryHolder(_other);
            if (holder == null) return;
            holdersInZone.Add(holder);
        }

        void OnTriggerExit(Collider _other)
        {
            if (!colliderToHolderCache.TryGetValue(_other, out var holder)) return;
            holdersInZone.Remove(holder);
            colliderToHolderCache.Remove(_other);
        }

        void Update()
        {
            if (sink == null) return;
            if (holdersInZone.Count == 0) return;

            transferAccumulator += Time.deltaTime;
            if (transferAccumulator < transferIntervalSeconds) return;
            transferAccumulator = 0f;

            foreach (var holder in holdersInZone)
            {
                if (holder == null) continue;
                var inventory = holder.Inventory;
                if (inventory == null) continue;
                if (inventory.GetCount(resourceType) <= 0) continue;
                if (!sink.CanAcceptOne()) return;

                if (inventory.TryRemove(resourceType, 1))
                {
                    sink.TryAcceptOne();
                }
            }
        }

        IInventoryHolder ResolveInventoryHolder(Collider _collider)
        {
            if (_collider == null) return null;
            if (colliderToHolderCache.TryGetValue(_collider, out var cached)) return cached;

            var direct = _collider.GetComponent<IInventoryHolder>();
            var holder = direct ?? _collider.GetComponentInParent<IInventoryHolder>();
            colliderToHolderCache[_collider] = holder;
            return holder;
        }
    }
}
