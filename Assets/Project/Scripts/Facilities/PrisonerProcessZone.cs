using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Game;
using PrisonLife.Models;
using PrisonLife.View.World;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 죄수 처리칸. 일반 ResourceInputZone 과 달리 내부 Stockpile 버퍼를 보유.
    /// 전송 우선순위 (매 transferIntervalSeconds 마다 1개 이동):
    ///   downstream sink (head 죄수) ready 인 경우
    ///     1. holder 손에 자원 있음 → holder 손 → sink   (priority)
    ///     2. 없으면 buffer 에 stock 있음 → buffer → sink (fallback)
    ///   sink not ready 인 경우
    ///     3. holder 손에 자원 + buffer 여유 → holder 손 → buffer 적재
    /// downstream sink 는 PrisonerQueueManager 가 Init 으로 주입 (HeadPrisonerHandcuffSink).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class PrisonerProcessZone : MonoBehaviour
    {
        [Header("Resource")]
        [SerializeField] private ResourceType expectedResourceType = ResourceType.Handcuff;
        [SerializeField, Min(1)] private int bufferCapacity = 8;
        [SerializeField, Min(0.02f)] private float transferIntervalSeconds = 0.2f;

        [Header("Buffer Stack Visual")]
        [SerializeField] private Vector3 bufferStackOffsetStep = new Vector3(0f, 0.18f, 0f);

        private BoxCollider triggerBoxCollider;
        private Rigidbody kinematicRigidbody;
        private IResourceSink downstreamSink;

        private readonly HashSet<IInventoryHolder> holdersInZone = new();
        private readonly Dictionary<Collider, IInventoryHolder> colliderToHolderCache = new();
        private float transferAccumulator;

        private StackVisualizer bufferStockVisualizer;

        public StockpileModel BufferStockpile { get; private set; }

        private void Awake()
        {
            triggerBoxCollider = GetComponent<BoxCollider>();
            triggerBoxCollider.isTrigger = true;

            kinematicRigidbody = GetComponent<Rigidbody>();
            kinematicRigidbody.isKinematic = true;
            kinematicRigidbody.useGravity = false;

            BufferStockpile = new StockpileModel(expectedResourceType, bufferCapacity);
        }

        private void Start()
        {
            var registry = SystemManager.Instance != null ? SystemManager.Instance.ResourceItems : null;
            bufferStockVisualizer = new StackVisualizer(
                BufferStockpile.Count,
                transform,
                registry != null ? registry.GetPrefab(expectedResourceType) : null,
                bufferStackOffsetStep);
        }

        private void OnDestroy()
        {
            bufferStockVisualizer?.Dispose();
            bufferStockVisualizer = null;
        }

        public void Init(IResourceSink _downstreamSink)
        {
            if (_downstreamSink == null)
            {
                downstreamSink = null;
                return;
            }

            if (_downstreamSink.InputType != expectedResourceType)
            {
                Debug.LogError(
                    $"[PrisonerProcessZone] {name}: 자원 타입 불일치 — zone={expectedResourceType}, sink={_downstreamSink.InputType}.");
                downstreamSink = null;
                return;
            }

            downstreamSink = _downstreamSink;
        }

        private void OnTriggerEnter(Collider _other)
        {
            var holder = ResolveInventoryHolder(_other);
            if (holder == null) return;
            holdersInZone.Add(holder);
        }

        private void OnTriggerExit(Collider _other)
        {
            if (!colliderToHolderCache.TryGetValue(_other, out var holder)) return;
            holdersInZone.Remove(holder);
            colliderToHolderCache.Remove(_other);
        }

        private void Update()
        {
            if (downstreamSink == null) return;

            transferAccumulator += Time.deltaTime;
            if (transferAccumulator < transferIntervalSeconds) return;
            transferAccumulator = 0f;

            // holder (플레이어 / 죄수 일꾼) 가 zone 에 1명 이상 있어야 어떤 transfer 도 일어남.
            // 비어있으면 buffer 가 차있어도 죄수가 그냥 대기.
            if (holdersInZone.Count == 0) return;

            bool sinkReady = downstreamSink.CanAcceptOne();

            if (sinkReady)
            {
                // 1. holder 손에 든 자원 → sink (우선)
                var holderWithItem = FindHolderWithItem();
                if (holderWithItem != null)
                {
                    if (holderWithItem.Inventory.TryRemove(expectedResourceType, 1))
                    {
                        downstreamSink.TryAcceptOne();
                    }
                    return;
                }

                // 2. buffer → sink (차선)
                if (BufferStockpile.HasStock)
                {
                    if (BufferStockpile.TryRemoveOne())
                    {
                        downstreamSink.TryAcceptOne();
                    }
                }
            }
            else
            {
                // 3. sink 미준비 — holder 손 → buffer 적재
                var holderWithItem = FindHolderWithItem();
                if (holderWithItem == null) return;
                if (!BufferStockpile.HasSpace) return;

                if (holderWithItem.Inventory.TryRemove(expectedResourceType, 1))
                {
                    BufferStockpile.TryAdd(1);
                }
            }
        }

        private IInventoryHolder FindHolderWithItem()
        {
            foreach (var holder in holdersInZone)
            {
                if (holder == null) continue;
                if (holder.Inventory == null) continue;
                if (holder.Inventory.GetCount(expectedResourceType) > 0) return holder;
            }
            return null;
        }

        private IInventoryHolder ResolveInventoryHolder(Collider _collider)
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
