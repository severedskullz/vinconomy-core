
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory.StallSlots
{
    public class ItemStallSlot : StallSlotBase
    {
        public ViconItemSlot[] slots;

        public ItemStallSlot(InventoryBase inventory, int stallSlot, int numSlots) : base(inventory, numSlots)
        {
            slots = new ViconItemSlot[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                slots[i] = new ViconItemSlot(inventory, stallSlot, i);

            }
        }

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < ProductStacksPerStall)
                {
                    return slots[slotId];
                }
                else
                    return Currency;

            }
            set
            {
                if (slotId < ProductStacksPerStall)
                {
                    slots[slotId] = (ViconItemSlot)value;
                }
                else
                    Currency = (ViconCurrencySlot)value;
            }
        }

        public override ViconItemSlot GetSlot(int itemSlot)
        {
            return slots[itemSlot];
        }

        public override ViconItemSlot[] GetSlots()
        {
            return slots;
        }

        public override void SetSlot(int itemSlot, ItemSlot value)
        {
            slots[itemSlot] = (ViconItemSlot) value;
        }

        public override ViconItemSlot FindFirstNonEmptyStockSlot()
        {
            foreach (ViconItemSlot slot in slots)
            {
                if (slot.Itemstack != null)
                    return slot;
            }
            return null;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("purchaseQuantity", ItemsPerPurchase);
            if (Currency.Itemstack != null)
            {
                tree.SetItemstack("currency", Currency.Itemstack.Clone());
            }
            for (int j = 0; j < ProductStacksPerStall; j++)
            {
                if (slots[j].Itemstack != null)
                {
                    tree.SetItemstack("slot" + j, slots[j].Itemstack.Clone());
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            ItemsPerPurchase = tree.GetInt("purchaseQuantity", 1);

            Currency.Itemstack = tree.GetItemstack("currency");
            for (int j = 0; j < ProductStacksPerStall; j++)
            {
                ItemStack itemStack = tree.GetItemstack("slot" + j);
                slots[j].Itemstack = itemStack;

            }
        }

        
    }
}