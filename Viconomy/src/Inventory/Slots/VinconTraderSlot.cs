using Vintagestory.API.Common;

namespace Vinconomy.Inventory.Slots
{
    public class VinconTraderSlot : ItemSlot
    {
        public int stallSlot { get; private set; }

        public VinconTraderSlot(InventoryBase inventory, int stallSlot) : base(inventory)
        {
            this.stallSlot = stallSlot;
            //this.HexBackgroundColor = "#12526B";
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

        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (Empty)
            {
                return;
            }


        }

    }

}
