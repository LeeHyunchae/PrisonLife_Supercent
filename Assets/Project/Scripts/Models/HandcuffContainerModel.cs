using PrisonLife.Core;
using PrisonLife.Reactive;

namespace PrisonLife.Models
{
    public class HandcuffContainerModel
    {
        public ReactiveProperty<int> StoredOreCount { get; } = new(0);
        public ReactiveProperty<int> MaxOreStorage { get; } = new(20);
        public ReactiveProperty<int> ProducedHandcuffCount { get; } = new(0);
        public ReactiveProperty<int> MaxHandcuffStorage { get; } = new(8);
        public ReactiveProperty<float> ProductionPeriodSeconds { get; } = new(1.0f);

        public IResourceSink OreSink { get; }
        public IResourceSource HandcuffSource { get; }

        public HandcuffContainerModel()
        {
            OreSink = new OreInputAdapter(this);
            HandcuffSource = new HandcuffOutputAdapter(this);
        }

        public bool HasOreSpace => StoredOreCount.Value < MaxOreStorage.Value;
        public bool HasStoredOre => StoredOreCount.Value > 0;
        public bool HasHandcuffSpace => ProducedHandcuffCount.Value < MaxHandcuffStorage.Value;
        public bool HasProducedHandcuff => ProducedHandcuffCount.Value > 0;

        public bool TryAddOre()
        {
            if (!HasOreSpace) return false;
            StoredOreCount.Value++;
            return true;
        }

        public bool TryConsumeOreForProduction()
        {
            if (!HasStoredOre) return false;
            StoredOreCount.Value--;
            return true;
        }

        public bool TryAddHandcuff()
        {
            if (!HasHandcuffSpace) return false;
            ProducedHandcuffCount.Value++;
            return true;
        }

        public bool TryRemoveHandcuff()
        {
            if (!HasProducedHandcuff) return false;
            ProducedHandcuffCount.Value--;
            return true;
        }

        sealed class OreInputAdapter : IResourceSink
        {
            readonly HandcuffContainerModel owner;
            public OreInputAdapter(HandcuffContainerModel _owner) { owner = _owner; }
            public ResourceType InputType => ResourceType.Ore;
            public bool CanAcceptOne() => owner.HasOreSpace;
            public bool TryAcceptOne() => owner.TryAddOre();
        }

        sealed class HandcuffOutputAdapter : IResourceSource
        {
            readonly HandcuffContainerModel owner;
            public HandcuffOutputAdapter(HandcuffContainerModel _owner) { owner = _owner; }
            public ResourceType OutputType => ResourceType.Handcuff;
            public bool HasAvailable() => owner.HasProducedHandcuff;
            public bool TryProvideOne() => owner.TryRemoveHandcuff();
        }
    }
}
