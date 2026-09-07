using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.Slots
{
    public class GachaProductSlot : VinconCloningSlot, IStockUpdater
    {
        int StallIndex;
        int SlotIndex;

        public GachaProductSlot(InventoryBase inventory, int stallIndex, int slotIndex) : base(inventory)
        {
            this.StallIndex = stallIndex;
            this.SlotIndex = slotIndex;
            //this.HexBackgroundColor = "#B625FF";
            this.BackgroundIcon = "commercially-general2";
        }

        public bool ItemMatchesProduct(ItemStack product, ItemSlot sourceSlot)
        {
            return true;
        }

        public bool ShouldUpdateProductSlot(ItemSlot productSlot, ItemSlot sourceSlot)
        {
            return true;
        }

        public void UpdateProductSlot(ItemSlot productSlot, ItemSlot sourceSlot)
        {
            GachaShopInventory inventory = (GachaShopInventory)this.inventory;
            GachaStallSlot stallSlot = inventory.GetStall<GachaStallSlot>(this.GetStallIndex());
            stallSlot.RegenProduct();
        }

        public int GetStallIndex()
        {
            return StallIndex;
        }

        public int GetSlotIndex()
        {
            return SlotIndex;
        }
    }
}
