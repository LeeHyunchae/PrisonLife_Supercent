using PrisonLife.Reactive;

namespace PrisonLife.Core
{
    public interface IInventory
    {
        int GetCount(ResourceType _type);
        int GetCapacity(ResourceType _type);
        bool CanAdd(ResourceType _type, int _amount = 1);
        bool TryAdd(ResourceType _type, int _amount = 1);
        bool TryRemove(ResourceType _type, int _amount = 1);
        ReactiveProperty<int> ObserveCount(ResourceType _type);
    }
}
