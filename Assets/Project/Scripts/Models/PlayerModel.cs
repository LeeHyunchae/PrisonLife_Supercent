using PrisonLife.Reactive;

namespace PrisonLife.Models
{
    public class PlayerModel
    {
        public ReactiveProperty<float> MoveSpeed { get; } = new(5f);
        public ReactiveProperty<int> WeaponUpgradeStage { get; } = new(0);

        public ReactiveProperty<float> MiningSwingDurationSeconds { get; } = new(0.7f);
        public ReactiveProperty<int> MiningHitsPerSwing { get; } = new(1);
        public ReactiveProperty<float> MiningRangeWidth { get; } = new(1.0f);
        public ReactiveProperty<float> MiningRangeDepth { get; } = new(1.0f);

        public InventoryModel Inventory { get; }

        public PlayerModel(InventoryModel _inventory)
        {
            Inventory = _inventory;
        }
    }
}
