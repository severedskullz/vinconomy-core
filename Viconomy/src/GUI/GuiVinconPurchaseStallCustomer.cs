using Viconomy.BlockEntities;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Viconomy.Inventory.Impl;

namespace Viconomy.GUI
{
    public class GuiVinconPurchaseStallCustomer : GuiViconStallCustomer
    {
        public GuiVinconPurchaseStallCustomer(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi, stallSelection)
        {

        }

        public override void InitializeInventoryDisplaySlots()
        {
            ViconItemPurchaseInventory vinInv = Inventory as ViconItemPurchaseInventory;
            PurchaseStallSlot purchaseSlot = vinInv.GetStall<PurchaseStallSlot>(curTab);
            stallSlot = purchaseSlot;
            ItemSlot purchaseItem = stallSlot.Currency;

            if (purchaseItem != null && purchaseItem.Itemstack != null)
            {
                this.purchaseSlot.Itemstack = purchaseItem.Itemstack.Clone();
                this.purchaseSlot.Itemstack.StackSize = purchaseItem.StackSize * quantity;
            }
            else
            {
                this.purchaseSlot.Itemstack = null;
            }

            ItemSlot currencyItem = purchaseSlot.DesiredProduct;

            if (currencyItem != null && currencyItem.Itemstack != null)
            {
                this.currancySlot.Itemstack = currencyItem.Itemstack.Clone();
                this.currancySlot.Itemstack.StackSize = currencyItem.StackSize * quantity;
            }
            else
            {
                this.currancySlot.Itemstack = null;
            }
        }

    }
}
