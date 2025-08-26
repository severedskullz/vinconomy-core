using Vintagestory.API.Common;

namespace Viconomy.Inventory.Slots
{
    public class ViconCurrencySlot : ItemSlot
    {
        public ViconCurrencySlot(InventoryBase inventory) : base(inventory)
        {
            //this.HexBackgroundColor = "#B62521";
            BackgroundIcon = "vicon-payment";
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
            if (sourceSlot.Itemstack != null)
            {
                SetCurrency(sourceSlot.Itemstack.Clone());
            }
            else
            {
                SetCurrency(null);
            }
        }

        private void SetCurrency(ItemStack stack)
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
