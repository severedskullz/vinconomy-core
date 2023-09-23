using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory
{
    public class ViconItemSlot : ItemSlot
    {
        int stallSlot = 0;
        int itemSlot = 0;
        Vintagestory.API.Common.Func<ItemSlot, bool> slotFilter = null;
        public ViconItemSlot(InventoryBase inventory, int stallSlot, int itemSlot) : base(inventory)
        {
            this.stallSlot = stallSlot;
            this.itemSlot = itemSlot;
            this.HexBackgroundColor = "#65d934";
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            if (this.slotFilter != null && this.slotFilter(sourceSlot) == false)
            {
                return false;
            }

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
            }
            
            //Console.WriteLine("Stall Slot " + stallSlot + ":First Non-Empty Slot was not satisfied, so we return false");
            return false;
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

        public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
        {
            //Console.WriteLine("TryPutInto " + stallSlot + " called...");
            return base.TryPutInto(sinkSlot, ref op);
        }

        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
           base.ActivateSlotLeftClick(sourceSlot, ref op);
        }
        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            //Console.WriteLine("We called Activate Slot.");
            base.ActivateSlot(sourceSlot, ref op);
        }

        public void setFilter(Vintagestory.API.Common.Func<ItemSlot, bool> filter)
        {
            this.slotFilter = filter;
        }

    }
    
}
