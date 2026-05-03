using System;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Entities;
using PrisonLife.Game;
using PrisonLife.Managers;
using PrisonLife.Models;
using UnityEngine;

namespace PrisonLife.Facilities
{
    /// <summary>
    /// 죄수 처리 칸. buffer 와 sink, buffer anchor (시각상 buffer 위치) 를 외부에서 Init.
    /// holder 타입별 동작:
    ///   Player (IsPlayerControlled): InputZone 과 동일한 full logic (어느 zone 에 있어도 같은 행동)
    ///   Worker (IsPlayerControlled=false): hand → sink (priority) / buffer → sink (fallback) 만 (deposit X)
    /// 모든 transfer 는 ItemFlowManager.Fly 로 시각화.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class HandcuffProcessZone : MonoBehaviour
    {
        [Header("Resource")]
        [SerializeField] private ResourceType expectedResourceType = ResourceType.Handcuff;
        [SerializeField, Min(0.02f)] private float transferIntervalSeconds = 0.2f;

        [Header("Flight Effect")]
        [SerializeField, Min(0.05f)] private float flightDurationSeconds = 0.25f;
        [SerializeField, Min(0f)] private float flightArcHeight = 1.0f;
        [SerializeField] private Vector3 holderFlightOriginOffset = new Vector3(0f, 1.0f, 0f);

        private BoxCollider triggerBoxCollider;
        private Rigidbody kinematicRigidbody;

        private StockpileModel sharedBufferStockpile;
        private IResourceSink downstreamSink;
        private Transform bufferAnchor;

        private readonly HashSet<IInventoryHolder> holdersInZone = new();
        private readonly Dictionary<Collider, IInventoryHolder> colliderToHolderCache = new();
        private float transferAccumulator;

        private void Awake()
        {
            triggerBoxCollider = GetComponent<BoxCollider>();
            triggerBoxCollider.isTrigger = true;

            kinematicRigidbody = GetComponent<Rigidbody>();
            kinematicRigidbody.isKinematic = true;
            kinematicRigidbody.useGravity = false;
        }

        public void Init(StockpileModel _sharedBufferStockpile, IResourceSink _downstreamSink, Transform _bufferAnchor)
        {
            sharedBufferStockpile = _sharedBufferStockpile;
            bufferAnchor = _bufferAnchor;

            if (_downstreamSink != null && _downstreamSink.InputType != expectedResourceType)
            {
                Debug.LogError(
                    $"[HandcuffProcessZone] {name}: 자원 타입 불일치 — zone={expectedResourceType}, sink={_downstreamSink.InputType}.");
                downstreamSink = null;
                return;
            }
            downstreamSink = _downstreamSink;
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
            if (holdersInZone.Count == 0) return;

            transferAccumulator += Time.deltaTime;
            if (transferAccumulator < transferIntervalSeconds) return;
            transferAccumulator = 0f;

            foreach (IInventoryHolder holder in holdersInZone)
            {
                if (holder == null || holder.Inventory == null) continue;

                bool success = holder.IsPlayerControlled
                    ? TryPlayerFullLogic(holder)
                    : TryProcessOnly(holder);

                if (success) return;
            }
        }

        private bool TryPlayerFullLogic(IInventoryHolder _holder)
        {
            bool sinkReady = downstreamSink != null && downstreamSink.CanAcceptOne();
            int handCount = _holder.Inventory.GetCount(expectedResourceType);

            // Rule 1: sink ready + hand → hand → sink (head prisoner)
            if (sinkReady && handCount > 0)
            {
                if (_holder.Inventory.TryRemove(expectedResourceType, 1))
                {
                    downstreamSink.TryAcceptOne();
                    LaunchFlightDecoration(GetHolderPosition(_holder), GetSinkPosition(), ResolveSinkArrivalCallback());
                    return true;
                }
            }

            // Rule 2: sink ready + hand 빔 + buffer 있음 → buffer → sink
            if (sinkReady && handCount == 0 && sharedBufferStockpile != null && sharedBufferStockpile.HasStock)
            {
                if (sharedBufferStockpile.TryRemoveOne())
                {
                    downstreamSink.TryAcceptOne();
                    LaunchFlightDecoration(GetBufferPosition(), GetSinkPosition(), ResolveSinkArrivalCallback());
                    return true;
                }
            }

            // Rule 3: sink 미ready + hand 있음 + buffer 여유 → hand → buffer
            if (!sinkReady && handCount > 0 && sharedBufferStockpile != null && sharedBufferStockpile.HasSpace)
            {
                if (_holder.Inventory.TryRemove(expectedResourceType, 1))
                {
                    sharedBufferStockpile.TryAdd(1);
                    LaunchFlightDecoration(GetHolderPosition(_holder), GetBufferPosition(), null);
                    return true;
                }
            }

            return false;
        }

        private bool TryProcessOnly(IInventoryHolder _holder)
        {
            // Worker: hand → sink 우선, buffer → sink 차선. deposit 안 함.
            if (downstreamSink == null || !downstreamSink.CanAcceptOne()) return false;

            int handCount = _holder.Inventory.GetCount(expectedResourceType);
            if (handCount > 0)
            {
                if (_holder.Inventory.TryRemove(expectedResourceType, 1))
                {
                    downstreamSink.TryAcceptOne();
                    LaunchFlightDecoration(GetHolderPosition(_holder), GetSinkPosition(), ResolveSinkArrivalCallback());
                    return true;
                }
            }

            if (sharedBufferStockpile != null && sharedBufferStockpile.HasStock)
            {
                if (sharedBufferStockpile.TryRemoveOne())
                {
                    downstreamSink.TryAcceptOne();
                    LaunchFlightDecoration(GetBufferPosition(), GetSinkPosition(), ResolveSinkArrivalCallback());
                    return true;
                }
            }

            return false;
        }

        private void LaunchFlightDecoration(Vector3 _fromPosition, Vector3 _toPosition, Action _onArrival)
        {
            // model 업데이트는 즉시 완료한 상태 — 비행은 시각 효과 + 도착 시점에 prisoner 등 destination side 후처리.
            ItemFlowManager flow = SystemManager.Instance != null ? SystemManager.Instance.ItemFlow : null;
            if (flow == null)
            {
                _onArrival?.Invoke();
                return;
            }
            flow.Fly(expectedResourceType, _fromPosition, _toPosition, flightDurationSeconds, flightArcHeight, _onArrival);
        }

        private Action ResolveSinkArrivalCallback()
        {
            // sink (head prisoner) 가 비행 도착 시점에 자기 in-flight 카운트를 감소시켜 phase 전환 결정.
            if (downstreamSink == null) return null;
            Transform anchor = downstreamSink.AnchorTransform;
            if (anchor == null) return null;
            Prisoner head = anchor.GetComponent<Prisoner>();
            return head != null ? new Action(head.OnHandcuffArrived) : null;
        }

        private Vector3 GetHolderPosition(IInventoryHolder _holder)
        {
            return _holder.Transform != null
                ? _holder.Transform.position + holderFlightOriginOffset
                : transform.position;
        }

        private Vector3 GetBufferPosition()
        {
            return bufferAnchor != null ? bufferAnchor.position : transform.position;
        }

        private Vector3 GetSinkPosition()
        {
            if (downstreamSink == null) return transform.position;
            Transform anchor = downstreamSink.AnchorTransform;
            return anchor != null ? anchor.position + holderFlightOriginOffset : transform.position;
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
    }
}
