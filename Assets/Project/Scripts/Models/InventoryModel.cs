using System;
using System.Collections.Generic;
using PrisonLife.Core;
using PrisonLife.Reactive;

namespace PrisonLife.Models
{
    public class InventoryModel : IInventory
    {
        private readonly Dictionary<ResourceType, ReactiveProperty<int>> countByType = new();
        private readonly Dictionary<ResourceType, ReactiveProperty<int>> capacityByType = new();

        public InventoryModel(IDictionary<ResourceType, int> _initialCapacityByType = null)
        {
            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                countByType[resourceType] = new ReactiveProperty<int>(0);
                int initialCapacity = 0;
                if (_initialCapacityByType != null)
                {
                    _initialCapacityByType.TryGetValue(resourceType, out initialCapacity);
                }
                capacityByType[resourceType] = new ReactiveProperty<int>(initialCapacity);
            }
        }

        public int GetCount(ResourceType _type) => countByType[_type].Value;

        public int GetCapacity(ResourceType _type) => capacityByType[_type].Value;

        public ReactiveProperty<int> ObserveCount(ResourceType _type) => countByType[_type];

        public ReactiveProperty<int> ObserveCapacity(ResourceType _type) => capacityByType[_type];

        public bool CanAdd(ResourceType _type, int _amount = 1)
        {
            if (_amount <= 0) return false;
            return countByType[_type].Value + _amount <= capacityByType[_type].Value;
        }

        public bool TryAdd(ResourceType _type, int _amount = 1)
        {
            if (!CanAdd(_type, _amount)) return false;
            countByType[_type].Value += _amount;
            return true;
        }

        public bool TryRemove(ResourceType _type, int _amount = 1)
        {
            if (_amount <= 0) return false;
            if (countByType[_type].Value < _amount) return false;
            countByType[_type].Value -= _amount;
            return true;
        }

        public void SetCapacity(ResourceType _type, int _newCapacity)
        {
            if (_newCapacity < 0) _newCapacity = 0;
            capacityByType[_type].Value = _newCapacity;
        }

        public bool IsAtCapacity(ResourceType _type)
        {
            return countByType[_type].Value >= capacityByType[_type].Value;
        }
    }
}
