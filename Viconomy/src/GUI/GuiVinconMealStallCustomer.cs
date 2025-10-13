using HarmonyLib;
using Microsoft.VisualBasic;
using System;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.GUI
{
    public class GuiVinconMealStallCustomer : GuiViconStallCustomer
    {
        BEVinconContainer stall;

        public GuiVinconMealStallCustomer(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi, stallSelection)
        {

        }

        public override void InitializeInventory()
        {
            ViconMealInventory vinInv = Inventory as ViconMealInventory;
            MealStallSlot stall = vinInv.GetStall<MealStallSlot>(curTab);
            ItemSlot purchaseItem = stall.FindFirstNonEmptyStockSlot();

            if (purchaseItem != null)
            {
                this.purchaseSlot.Itemstack = stall.GenerateMealStack(capi);
                this.purchaseSlot.Itemstack.StackSize = stall.ItemsPerPurchase * quantity;
            }
            else
            {
                this.purchaseSlot.Itemstack = null;
            }

            ItemSlot currencyItem = stall.Currency;

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
