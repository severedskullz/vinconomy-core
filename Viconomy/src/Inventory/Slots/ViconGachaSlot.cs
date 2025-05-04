using Vintagestory.API.Common;

namespace Viconomy.Inventory.Slots
{
    public class ViconGachaSlot : ItemSlot
    {
        public bool isDisabled { get; set; } = false;
        public int Slot { get; internal set; }

        public ViconGachaSlot(InventoryBase inventory, int itemSlot) : base(inventory)
        {
            //this.HexBackgroundColor = "#65d934";
            BackgroundIcon = "vicon-general";
            Slot = itemSlot;
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            CollectibleObject collectible = sourceSlot.Itemstack?.Collectible;
            if (collectible != null && !isDisabled)
            {
                if (collectible is Block)
                {
                    return base.CanHold(sourceSlot);
                }
            }

            //Console.WriteLine("Stall Slot " + stallSlot + ":First Non-Empty Slot was not satisfied, so we return false");
            return false;
        }

        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            if (CanHold(sourceSlot))
            {
                return base.CanTakeFrom(sourceSlot, priority);
            }

            return false;
        }
    }

}
