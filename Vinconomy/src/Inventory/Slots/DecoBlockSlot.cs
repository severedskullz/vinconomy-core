using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Vinconomy.Inventory.Slots
{
    public class DecoBlockSlot : ItemSlot
    {
        public bool isDisabled { get; set; } = false;
        public DecoBlockSlot(InventoryBase inventory, int itemSlot) : base(inventory)
        {
            //this.HexBackgroundColor = "#65d934";
            this.BackgroundIcon = "commercially-chisel";
            MaxSlotStackSize = 1;

        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            CollectibleObject collectible = sourceSlot.Itemstack?.Collectible;
            if (collectible != null && !isDisabled)
            {
                if (collectible is BlockMicroBlock)
                {
                    return true;
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
