using Vintagestory.API.Common;

namespace Viconomy.Inventory.Slots
{
    public class ViconSculptureBlockSlot : ItemSlot
    {
        public bool isDisabled { get; set; } = false;
        public ViconSculptureBlockSlot(InventoryBase inventory,int itemSlot) : base(inventory)
        {
            //this.HexBackgroundColor = "#65d934";
            //this.BackgroundIcon = "vicon-boots";
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
