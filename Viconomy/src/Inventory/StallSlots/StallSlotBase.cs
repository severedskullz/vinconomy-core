using System;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory.StallSlots
{
    public abstract class StallSlotBase
    {
        public int ProductStacksPerStall;
        public ViconCurrencySlot Currency;
        public int ItemsPerPurchase = 1;
        //protected InventoryBase Inventory;

        public virtual int StallSlots => ProductStacksPerStall;
        public virtual int TotalSlots => StallSlots + 1;


        public StallSlotBase(InventoryBase inventory, int numStacksPerStall)
        {
            //Inventory = inventory;
            Currency = new ViconCurrencySlot(inventory);
            ProductStacksPerStall = numStacksPerStall;
        }

        public abstract ItemSlot this[int slotId] { get; set; }

        public abstract void SetSlot(int itemSlot, ItemSlot value);
        public abstract ItemSlot GetSlot(int itemSlot);

        public abstract ItemSlot[] GetSlots();
        public abstract ItemSlot FindFirstNonEmptyStockSlot();

        public virtual int GetProductQuantity()
        {
            ItemSlot[] items = GetSlots();
            int amount = 0;
            foreach (ItemSlot item in items)
            {
                if (item?.Itemstack != null)
                {
                    amount += item.Itemstack.StackSize;
                }
            }

            return amount;
        }

        public virtual int GetNumPurchasesRemaining()
        {
            return GetProductQuantity() / ItemsPerPurchase;
        }

        public virtual string GetProductName(ICoreAPI api)
        {
            return FindFirstNonEmptyStockSlot().Itemstack.GetName();
        }

        public virtual string GetCurrencyName(ICoreAPI api)
        {
            return Currency?.Itemstack.GetName();
        }

        public abstract void ToTreeAttributes(ITreeAttribute tree);
        public abstract void FromTreeAttributes(ITreeAttribute tree);

        public virtual void ResolveBlockOrItem(IWorldAccessor world)
        {
            Currency.Itemstack?.ResolveBlockOrItem(world);
            for (int j = 0; j < StallSlots; j++)
            {
                GetSlot(j).Itemstack?.ResolveBlockOrItem(world);
            }
        }
    }
}