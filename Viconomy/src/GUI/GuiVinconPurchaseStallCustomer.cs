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

        public override void InitializeInventory()
        {
            ViconItemPurchaseInventory vinInv = Inventory as ViconItemPurchaseInventory;
            PurchaseStallSlot stallSlot = vinInv.GetStall<PurchaseStallSlot>(curTab);
            ItemSlot purchaseItem = stallSlot.Currency;

            if (purchaseItem != null && purchaseItem.Itemstack != null)
            {
                this.purchaseSlot.Itemstack = purchaseItem.Itemstack.Clone();
                this.purchaseSlot.Itemstack.StackSize = stallSlot.ItemsPerPurchase * quantity;
            }
            else
            {
                this.purchaseSlot.Itemstack = null;
            }

            ItemSlot currencyItem = stallSlot.DesiredProduct;

            if (currencyItem != null && currencyItem.Itemstack != null)
            {
                this.currancySlot.Itemstack = currencyItem.Itemstack.Clone();
                this.currancySlot.Itemstack.StackSize = currencyItem.Itemstack.StackSize * quantity;
            }
            else
            {
                this.currancySlot.Itemstack = null;
            }
        }

    }
}
