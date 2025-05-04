using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Viconomy.Inventory.StallSlots
{
    public class ItemStallSlot : StallSlotBase<ViconItemSlot>
    {
        public ViconItemSlot[] slots;

        public ItemStallSlot(InventoryBase inventory, int stallSlot, int numSlots) : base(inventory)
        {
            slots = new ViconItemSlot[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                slots[i] = new ViconItemSlot(inventory, stallSlot, i);

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

        public override void SetSlot(int itemSlot, ViconItemSlot value)
        {
            slots[itemSlot] = value;
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
    }
}