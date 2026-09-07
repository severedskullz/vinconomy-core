using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Vintagestory.API.Common;

namespace Vinconomy.Inventory.Slots
{
    public class VinconCloningSlot : FilteredItemSlot
    {


        public VinconCloningSlot(InventoryBase inventory) : base(inventory)
        {
            //this.HexBackgroundColor = "#B62521";
            //BackgroundIcon = "vicon-payment";
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
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
            if (sourceSlot.Itemstack != null && (Filter?.Invoke(sourceSlot) ?? true))
            {
                SetStack(sourceSlot.Itemstack.Clone());
            }
            else
            {
                SetStack(null);
            }
        }

        private void SetStack(ItemStack stack)
        {
            itemstack = stack;
            this.OnItemSlotModified(stack);
        }

        public override ItemStack TakeOut(int quantity)
        {
            return null;
        }

    }

}
