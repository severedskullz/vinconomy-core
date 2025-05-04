using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Viconomy.Inventory.StallSlots
{
    public class MealStallSlot : StallSlotBase<ItemSlot>
    {
        public ItemSlot[] slots;

        public MealStallSlot(InventoryBase inventory, int stallSlot, int numSlots) : base(inventory)
        {
            slots = new ItemSlot[4];
            for (int i = 0; i < numSlots; i++)
            {
                slots[i] = new ViconItemSlot(inventory, stallSlot, i);

            }
        }

        public override ItemSlot FindFirstNonEmptyStockSlot()
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.Itemstack != null)
                    return slot;
            }
            return null;
        }

        public override ItemSlot GetSlot(int itemSlot)
        {
            return slots[itemSlot];
        }

        public override ItemSlot[] GetSlots()
        {
            return slots;
        }

        public override void SetSlot(int itemSlot, ItemSlot value)
        {
            slots[itemSlot] = value;
        }
    }
}