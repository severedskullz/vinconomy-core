using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.Slots
{
    public class VinconTransferSlot : ItemSlot
    {
        public override int MaxSlotStackSize => 1;
        public VinconTransferSlot(InventoryBase inventory) : base(inventory)
        {
        }
    }
}
