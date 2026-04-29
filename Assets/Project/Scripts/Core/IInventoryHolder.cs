using PrisonLife.Models;

namespace PrisonLife.Core
{
    public interface IInventoryHolder
    {
        InventoryModel Inventory { get; }
    }
}
