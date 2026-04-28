using PrisonLife.Reactive;

namespace PrisonLife.Models
{
    public class WalletModel
    {
        public ReactiveProperty<int> Balance { get; } = new(0);

        public void Earn(int _amount)
        {
            if (_amount <= 0) return;
            Balance.Value += _amount;
        }

        public bool TrySpend(int _amount)
        {
            if (_amount <= 0) return false;
            if (Balance.Value < _amount) return false;
            Balance.Value -= _amount;
            return true;
        }
    }
}
