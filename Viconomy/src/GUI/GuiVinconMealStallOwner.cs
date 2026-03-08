using System;
using Vinconomy.Inventory.Impl;
using Vinconomy.Inventory.Slots;
using Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vinconomy.GUI
{
    public class GuiVinconMealStallOwner : GuiVinconStallOwner
    {
        public GuiVinconMealStallOwner(string DialogTitle, InventoryBase Inventory, bool isOwner, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection) : base(DialogTitle, Inventory, isOwner, BlockEntityPosition, capi, stallSelection)
        {
        }

        public override void InitializeInventory()
        {
            inv = new DummyInventory(capi);
            purchaseSlot = new VinconLockedSlot(inv, 0);
            inv.TakeLocked = true;
            inv.PutLocked = true;
            inv[0] = purchaseSlot;

            UpdatePurchaseSlot();

        }

        public override void UpdatePurchaseSlot()
        {
            VinconMealInventory vinInv = Inventory as VinconMealInventory;
            MealStallSlot stall = vinInv.GetStall<MealStallSlot>(curTab);
            ItemSlot purchaseItem = stall.FindFirstNonEmptyStockSlot();

            if (purchaseItem != null)
            {
                this.purchaseSlot.Itemstack = stall.GetSlot(0).Itemstack?.Clone();
                this.purchaseSlot.Itemstack.StackSize = 1;
            }
            else
            {
                this.purchaseSlot.Itemstack = null;
            }
        }
    }
}
