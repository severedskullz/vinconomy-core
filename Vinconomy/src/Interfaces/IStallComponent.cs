using Commercially.Common.Interfaces;
using Commercially.Common.Util;
using Commercially.Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public interface IStallComponent : IBlockEntityComponent
    {
        public IOwnableChild Ownable { get; }
        public IStallInventoryProvider InventoryProvider { get; }

        public int StallCount { get; }
        public ItemStack GetCurrencyForStallSlot(int stallSlot);
        public ItemStack GetProductForStallSlot(int stallSlot);
        public int GetRemainingProductForStallSlot(int stallSlot);
        public BaseStallSlot GetStallSlot(int stallSlot);
        public T GetStallSlot<T>(int stallSlot) where T : BaseStallSlot;
        public bool TryPurchaseItem(IPlayer player, int stallSlot, int numPurchases);

        public int GetStallIndexFromSelection(int selectionIndex);
    }
}