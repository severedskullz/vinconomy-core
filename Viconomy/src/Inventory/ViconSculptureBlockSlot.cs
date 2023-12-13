using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Viconomy.Inventory
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
                if (collectible.ItemClass == EnumItemClass.Block && (collectible is Block || collectible is BlockChisel))
                {
                    return base.CanHold(sourceSlot);
                }
            }
            
            //Console.WriteLine("Stall Slot " + stallSlot + ":First Non-Empty Slot was not satisfied, so we return false");
            return false;
        }

        /*
        protected override void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
                base.ActivateSlotLeftClick(sourceSlot, ref op);
        }

        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            //Console.WriteLine("We called Activate Slot.");
           
            if (sourceSlot.Empty && Itemstack == null)
            {
                setSlotDisabled(!isDisabled);
            }
            else
            {
                base.ActivateSlot(sourceSlot, ref op);
            }
        }

        public void setSlotDisabled(bool isDisabled)
        {
            this.isDisabled = isDisabled;
        }
        */

    }
    
}
