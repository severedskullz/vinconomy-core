using Vinconomy.BlockEntities;
using Vinconomy.Inventory.Impl;
using Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vinconomy.GUI
{
    public class GuiVinconMealStallCustomer : GuiVinconStallCustomer
    {
        BEVinconContainer stall;

        public GuiVinconMealStallCustomer(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi, stallSelection)
        {

        }

        public override void InitializeInventoryDisplaySlots()
        {
            VinconMealInventory vinInv = Inventory as VinconMealInventory;
            MealStallSlot stall = vinInv.GetStall<MealStallSlot>(curTab);
            stallSlot = stall;
            ItemSlot purchaseItem = stall.FindFirstNonEmptyStockSlot();

            if (purchaseItem != null)
            {
                this.purchaseSlot.Itemstack = stall.GetSlot(0).Itemstack.Clone();
                this.purchaseSlot.Itemstack.StackSize = stall.ItemsPerPurchase * quantity;
            }
            else
            {
                this.purchaseSlot.Itemstack = null;
            }

            ItemSlot currencyItem = stall.Currency;

            if (currencyItem != null && currencyItem.Itemstack != null)
            {
                this.currancySlot.Itemstack = currencyItem.Itemstack?.Clone();
                this.currancySlot.Itemstack.StackSize = currencyItem.Itemstack.StackSize * quantity;
            }
            else
            {
                this.currancySlot.Itemstack = null;
            }
        }
    }
}
