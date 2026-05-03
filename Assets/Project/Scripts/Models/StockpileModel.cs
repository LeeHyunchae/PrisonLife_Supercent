using PrisonLife.Core;
using PrisonLife.Reactive;
using UnityEngine;

namespace PrisonLife.Models
{
    /// <summary>
    /// 재사용 stockpile. 단일 ResourceType + Count/Capacity + IResourceSink/IResourceSource 어댑터.
    /// HandcuffContainer 의 광석/수갑 칸, MoneyOutput, 추후 PurchaseZone 비용 칸 등에 모두 사용.
    /// </summary>
    public class StockpileModel
    {
        public ResourceType ResourceType { get; }
        public ReactiveProperty<int> Count { get; } = new(0);
        public ReactiveProperty<int> Capacity { get; }

        public IResourceSink Sink { get; }
        public IResourceSource Source { get; }

        public StockpileModel(ResourceType _resourceType, int _initialCapacity)
        {
            ResourceType = _resourceType;
            Capacity = new ReactiveProperty<int>(Mathf.Max(1, _initialCapacity));
            Sink = new StockpileSinkAdapter(this);
            Source = new StockpileSourceAdapter(this);
        }

        public bool HasStock => Count.Value > 0;
        public bool HasSpace => Count.Value < Capacity.Value;

        public bool TryAdd(int _amount = 1)
        {
            if (_amount <= 0) return false;
            int newValue = Mathf.Min(Count.Value + _amount, Capacity.Value);
            if (newValue == Count.Value) return false;
            Count.Value = newValue;
            return true;
        }

        public bool TryRemoveOne()
        {
            if (!HasStock) return false;
            Count.Value--;
            return true;
        }

        public void SetCapacity(int _newCapacity)
        {
            int clamped = Mathf.Max(1, _newCapacity);
            if (Capacity.Value == clamped) return;
            Capacity.Value = clamped;
            if (Count.Value > clamped) Count.Value = clamped;
        }

        private sealed class StockpileSinkAdapter : IResourceSink
        {
            private readonly StockpileModel owner;
            public StockpileSinkAdapter(StockpileModel _owner) { owner = _owner; }
            public ResourceType InputType => owner.ResourceType;
            public bool CanAcceptOne() => owner.HasSpace;
            public bool TryAcceptOne() => owner.TryAdd(1);
            public Transform AnchorTransform => null;
        }

        private sealed class StockpileSourceAdapter : IResourceSource
        {
            private readonly StockpileModel owner;
            public StockpileSourceAdapter(StockpileModel _owner) { owner = _owner; }
            public ResourceType OutputType => owner.ResourceType;
            public bool HasAvailable() => owner.HasStock;
            public bool TryProvideOne() => owner.TryRemoveOne();
        }
    }
}
