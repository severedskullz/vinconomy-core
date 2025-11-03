using Vintagestory.API.Common;

namespace Viconomy.Inventory.Slots
{
    public class VinconLockedItemSlot : ViconItemSlot
    {
        public VinconLockedItemSlot(InventoryBase inventory, int stallSlot, int itemSlot) : base(inventory, stallSlot, itemSlot)
        {
        }

        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            // Dont do shit!
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            return false;
        }
    }
}
