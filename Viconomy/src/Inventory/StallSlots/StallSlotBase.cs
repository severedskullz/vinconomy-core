using System;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Viconomy.Inventory.StallSlots
{
    public abstract class StallSlotBase<E> where E : ItemSlot
    {

        public ViconCurrencySlot currency;
        public int itemsPerPurchase = 1;

        public StallSlotBase(InventoryBase inventory)
        {
            currency = new ViconCurrencySlot(inventory);

        }

        public abstract void SetSlot(int itemSlot, E value);
        public abstract E GetSlot(int itemSlot);

        public abstract E[] GetSlots();
        public abstract E FindFirstNonEmptyStockSlot();
    }
}