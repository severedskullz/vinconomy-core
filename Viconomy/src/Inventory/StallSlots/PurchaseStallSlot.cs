using System;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory.StallSlots
{
    public class PurchaseStallSlot : StallSlotBase
    {

        public ViconCurrencySlot DesiredProduct;
        public ViconItemSlot[] CurrencySlots;
        public ViconItemSlot[] PurchasedProductSlots;
        public int PurchasedItemStacksPerStall { get; private set; }
        public int NumTradesLeft;
        public bool LimitedPurchases;
        public bool FuzzyMatching;

        public override int StallSlots => ProductStacksPerStall + PurchasedItemStacksPerStall ;
        public override int TotalSlots => StallSlots + 2; // Currency and DesiredProduct

        public override ItemSlot this[int slotId] {
            get
            {
                if (slotId < ProductStacksPerStall)
                {
                    return CurrencySlots[slotId];
                }
                else if (slotId <= StallSlots)
                {
                    return PurchasedProductSlots[slotId - ProductStacksPerStall];
                }
                else if (slotId == TotalSlots - 2)
                {
                    return DesiredProduct;
                }
                else
                    return Currency;
                
            }
            set
            {
                if (slotId < ProductStacksPerStall)
                {
                    CurrencySlots[slotId] = (ViconItemSlot)value;
                }
                else if (slotId <= StallSlots)
                {
                    PurchasedProductSlots[slotId - ProductStacksPerStall] = (ViconItemSlot)value;
                }
                else if (slotId == TotalSlots - 2)
                {
                    DesiredProduct = (ViconCurrencySlot)value;
                }
                else
                    Currency = (ViconCurrencySlot)value;
            }
        }

        public PurchaseStallSlot(InventoryBase inventory, int stallSlot,  int numProductSlots, int numPurchasedStacks) : base(inventory, numProductSlots)
        {

            DesiredProduct = new ViconCurrencySlot(inventory);
            DesiredProduct.BackgroundIcon = "vicon-general";

            Currency.BackgroundIcon = "vicon-payment";

            PurchasedItemStacksPerStall = numPurchasedStacks;
            ProductStacksPerStall = numProductSlots;

            CurrencySlots = new ViconItemSlot[numProductSlots];
            for (int i = 0; i < numProductSlots; i++)
            {
                CurrencySlots[i] = new ViconItemSlot(inventory, stallSlot, i);
                CurrencySlots[i].BackgroundIcon = "vicon-payment";
                //ProductSlots[i].HexBackgroundColor = "#FF0000";

            }

            PurchasedProductSlots = new ViconItemSlot[numPurchasedStacks];
            for (int i = 0; i < numPurchasedStacks; i++)
            {
                PurchasedProductSlots[i] = new ViconItemSlot(inventory, stallSlot, i);
                PurchasedProductSlots[i].BackgroundIcon = "vicon-general";
                //PurchasedProductSlots[i].HexBackgroundColor = "#FFFF00";

            }
        } 

        public override ItemSlot FindFirstNonEmptyStockSlot()
        {
            foreach (ViconItemSlot slot in CurrencySlots)
            {
                if (slot.Itemstack != null)
                    return slot;
            }
            return null;
        }

        public override string GetProductName(ICoreAPI api)
        {
            return Currency?.Itemstack?.GetName();
        }

        public override string GetCurrencyName(ICoreAPI api)
        {
            return DesiredProduct?.Itemstack?.GetName();
        }


        public override ItemSlot GetSlot(int itemSlot)
        {
            //modSystem = Inventory.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            //modSystem.Mod.Logger.Warning($"Getting itemslot {itemSlot}");
            if (itemSlot < ProductStacksPerStall)

                return CurrencySlots[itemSlot];
            else
                return PurchasedProductSlots[itemSlot - ProductStacksPerStall];
        }

        public override ItemSlot[] GetSlots()
        {
            return CurrencySlots;
        }

        public ItemSlot[] GetPurchasedProductSlots()
        {
            return PurchasedProductSlots;
        }

        public override void SetSlot(int itemSlot, ItemSlot value)
        {
            if (itemSlot < ProductStacksPerStall)
                CurrencySlots[itemSlot] = (ViconItemSlot)value;
            else
                PurchasedProductSlots[itemSlot - ProductStacksPerStall] = (ViconItemSlot)value;

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("numTradesLeft", NumTradesLeft);
            tree.SetBool("limitedPurchases", LimitedPurchases);
            tree.SetBool("FuzzyMatching", FuzzyMatching);
            if (Currency.Itemstack != null)
            {
                tree.SetItemstack("currency", Currency.Itemstack.Clone());
            }
            if (DesiredProduct.Itemstack != null)
            {
                tree.SetItemstack("desiredProduct", DesiredProduct.Itemstack.Clone());
            }
            for (int i = 0; i < ProductStacksPerStall; i++)
            {
                if (CurrencySlots[i].Itemstack != null)
                {
                    tree.SetItemstack("slot" + i, CurrencySlots[i].Itemstack.Clone());
                }
            }

            for (int j = 0; j < PurchasedItemStacksPerStall; j++)
            {
                if (PurchasedProductSlots[j].Itemstack != null)
                {
                    tree.SetItemstack("productSlot" + j, PurchasedProductSlots[j].Itemstack.Clone());
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            NumTradesLeft = tree.GetInt("numTradesLeft", 0);
            LimitedPurchases = tree.GetBool("limitedPurchases", true);
            Currency.Itemstack = tree.GetItemstack("currency");
            DesiredProduct.Itemstack = tree.GetItemstack("desiredProduct");
            FuzzyMatching = tree.GetBool("FuzzyMatching");
            for (int i = 0; i < ProductStacksPerStall; i++)
            {
                CurrencySlots[i].Itemstack = tree.GetItemstack("slot" + i);

            }
            for (int j = 0; j < PurchasedItemStacksPerStall; j++)
            {
                PurchasedProductSlots[j].Itemstack = tree.GetItemstack("productSlot" + j);

            }
        }

        public override void ResolveBlockOrItem(IWorldAccessor world)
        {
            Currency.Itemstack?.ResolveBlockOrItem(world);
            DesiredProduct.Itemstack?.ResolveBlockOrItem(world);
            for (int j = 0; j < StallSlots; j++)
            {
                GetSlot(j).Itemstack?.ResolveBlockOrItem(world);
            }
        }

    }
}
