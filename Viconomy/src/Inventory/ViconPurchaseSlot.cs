using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Viconomy.Inventory
{
    public class ViconPurchaseSlot : ItemSlot
    {
        private int stallSlot;

        public ViconPurchaseSlot(InventoryBase inventory, int stallSlot) : base(inventory)
        {
            this.stallSlot = stallSlot;
            this.HexBackgroundColor = "#12526B";
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            Console.WriteLine("Skipping Purchase Slot.");
            return false;
        }

        public override bool CanTake()
        {
            return false;
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return false;
        }

        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (OnActivateLeftClick != null)
            {
                this.OnActivateLeftClick();
            }
        }

        protected override void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            
        }

        public event Action OnActivateLeftClick;

        public void SetProduct(ItemStack stack)
        {
            this.itemstack = stack;
        }

    }
    
}
