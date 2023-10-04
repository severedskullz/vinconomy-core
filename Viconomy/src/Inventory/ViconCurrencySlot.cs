using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Viconomy.Inventory
{
    public class ViconCurrencySlot : ItemSlot
    {
        int stallSlot = 0;
        public ViconCurrencySlot(InventoryBase inventory, int stallSlot) : base(inventory)
        {
            this.stallSlot = stallSlot;
            //this.HexBackgroundColor = "#B62521";
            this.BackgroundIcon = "vicon-payment";
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            Console.WriteLine("Skipping Currency Slot.");
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
            if (sourceSlot.Itemstack != null)
            {
                SetCurrency(sourceSlot.Itemstack.Clone());
            } else
            {
                SetCurrency(null);
            }
        }

        private void SetCurrency(ItemStack stack)
        {
            this.itemstack = stack;
        }

    }
    
}
