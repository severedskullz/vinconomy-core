using System;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.Slots;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiVinconMealStallOwner : GuiViconStallOwner
    {
        public GuiVinconMealStallOwner(string DialogTitle, InventoryBase Inventory, bool isOwner, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection) : base(DialogTitle, Inventory, isOwner, BlockEntityPosition, capi, stallSelection)
        {
        }

        public override void InitializeInventory()
        {
            inv = new DummyInventory(capi);
            purchaseSlot = new ViconPurchaseSlot(inv, 0);
            inv.TakeLocked = true;
            inv.PutLocked = true;
            inv[0] = purchaseSlot;

            UpdatePurchaseSlot();

        }

        public override void UpdatePurchaseSlot()
        {
            ViconMealInventory vinInv = Inventory as ViconMealInventory;
            MealStallSlot stall = vinInv.GetStall<MealStallSlot>(curTab);
            ItemSlot purchaseItem = stall.FindFirstNonEmptyStockSlot();

            if (purchaseItem != null)
            {
                this.purchaseSlot.Itemstack = stall.GenerateMealStack(capi);
                this.purchaseSlot.Itemstack.StackSize = 1;
            }
            else
            {
                this.purchaseSlot.Itemstack = null;
            }
        }
    }
}
