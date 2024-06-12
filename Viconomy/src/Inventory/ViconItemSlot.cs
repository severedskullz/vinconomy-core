using Vintagestory.API.Common;

namespace Viconomy.Inventory
{
    public class ViconItemSlot : ItemSlot
    {
        public int stallSlot { get; private set; } = 0;
        public int itemSlot { get; private set; } = 0;
        Func<ItemSlot, bool> slotFilter = null;
        public ViconItemSlot(InventoryBase inventory, int stallSlot, int itemSlot) : base(inventory)
        {
            this.stallSlot = stallSlot;
            this.itemSlot = itemSlot;
            //this.HexBackgroundColor = "#65d934";
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            if (this.slotFilter != null && this.slotFilter(sourceSlot) == false)
            {
                return false;
            }

            if (this.inventory is ViconomyInventory) { 
                ItemSlot slot = ((ViconomyInventory)this.inventory).FindFirstNonEmptyStockSlot(stallSlot);
                if (slot == null)
                {
                    //Console.WriteLine("Stall Slot " +stallSlot +":First Non-Empty Slot was null, so we called Base");
                    return base.CanHold(sourceSlot);
                } else if (slot.Itemstack == null) {
                    //Console.WriteLine("Stall Slot " + stallSlot + ":First Non-Empty Slot Item Stack was null, so we called Base");
                    return base.CanHold(sourceSlot);
                } else if (slot.Itemstack.Satisfies(sourceSlot.Itemstack)) {
                    //Console.WriteLine("Stall Slot " + stallSlot + ":First Non-Empty Slot satisfied, so we called Base");
                    return base.CanHold(sourceSlot);
                } else
                {
                    return false;
                }
                    
            } 
            
            //Console.WriteLine("Stall Slot " + stallSlot + ":First Non-Empty Slot was not satisfied, so we return false");
            return base.CanHold(sourceSlot);
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            //Console.WriteLine("Can Take From " + stallSlot + " called...");
            if (CanHold(sourceSlot))
            {
                return base.CanTakeFrom(sourceSlot, priority);
            }
            return false;
        }

        public void setFilter(Func<ItemSlot, bool> filter)
        {
            this.slotFilter = filter;
        }

    }
    
}
