using Commercially.Common.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public interface IStallInventoryProvider : IInventoryProvider
    {
        public int StallCount { get; }
        public ItemStack GetCurrencyForStallSlot(int stallSlot);
        public ItemStack GetProductForStallSlot(int stallSlot);
        public BaseStallSlot GetStallSlot(int stallSlot);
        public T GetStallSlot<T>(int stallSlot) where T : BaseStallSlot;
    }
}