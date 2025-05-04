using Vintagestory.API.Common;

namespace Viconomy.Inventory.Slots
{
    public class ViconPurchaseSlot : ItemSlot
    {
        public int stallSlot { get; private set; }

        public ViconPurchaseSlot(InventoryBase inventory, int stallSlot) : base(inventory)
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

    }
    
}
