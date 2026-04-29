using System.Collections.Generic;
using PrisonLife.Core;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 시설 → 자원 보유자 방향 트리거 존. transferIntervalSeconds 마다 1개 이동.
    /// 시설 측은 IResourceSource 를 통해 dispense 한다.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ResourceOutputZone : MonoBehaviour
    {
        [SerializeField] ResourceType resourceType;
        [SerializeField, Min(0.02f)] float transferIntervalSeconds = 0.1f;

        BoxCollider triggerBoxCollider;
        Rigidbody kinematicRigidbody;
        IResourceSource source;

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

        public void Init(IResourceSource _source)
        {
            if (_source == null)
            {
                source = null;
                return;
            }

            if (_source.OutputType != resourceType)
            {
                Debug.LogError(
                    $"[ResourceOutputZone] {name}: 자원 타입 불일치 — zone={resourceType}, source={_source.OutputType}.");
                source = null;
                return;
            }

            source = _source;
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
            if (source == null) return;
            if (holdersInZone.Count == 0) return;

            transferAccumulator += Time.deltaTime;
            if (transferAccumulator < transferIntervalSeconds) return;
            transferAccumulator = 0f;

            foreach (var holder in holdersInZone)
            {
                if (holder == null) continue;
                var inventory = holder.Inventory;
                if (inventory == null) continue;
                if (!source.HasAvailable()) return;
                if (!inventory.CanAdd(resourceType, 1)) continue;

                if (source.TryProvideOne())
                {
                    inventory.TryAdd(resourceType, 1);
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
