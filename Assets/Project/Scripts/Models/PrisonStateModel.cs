using PrisonLife.Reactive;

namespace PrisonLife.Models
{
    public class PrisonStateModel
    {
        public ReactiveProperty<int> CurrentInmateCount { get; } = new(0);
        public ReactiveProperty<int> MaxInmateCapacity { get; } = new(0);
        public ReactiveProperty<bool> IsExpansionPurchased { get; } = new(false);

        public bool HasFreeSlot => CurrentInmateCount.Value < MaxInmateCapacity.Value;

        public bool TryAdmitOne()
        {
            if (!HasFreeSlot) return false;
            CurrentInmateCount.Value++;
            return true;
        }

        public void IncreaseCapacity(int _additionalSlots)
        {
            if (_additionalSlots <= 0) return;
            MaxInmateCapacity.Value += _additionalSlots;
        }
    }
}
