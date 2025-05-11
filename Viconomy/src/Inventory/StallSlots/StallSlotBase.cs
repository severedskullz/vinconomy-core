using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Viconomy.Inventory.StallSlots
{
    public abstract class StallSlotBase
    {

        public ViconCurrencySlot currency;
        public int itemsPerPurchase = 1;

        public StallSlotBase(InventoryBase inventory)
        {
            currency = new ViconCurrencySlot(inventory);

        }

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

        public int GetNumPurchasesRemaining()
        {
            return GetProductQuantity() / itemsPerPurchase;
        }

        public virtual string GetProductName(ICoreAPI api)
        {
            return FindFirstNonEmptyStockSlot().Itemstack.GetName();
        }

        public virtual string GetCurrencyName(ICoreAPI api)
        {
            return currency?.Itemstack.GetName();
        }
    }
}