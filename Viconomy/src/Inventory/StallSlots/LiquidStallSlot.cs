using System;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.StallSlots
{
    public class LiquidStallSlot : ItemStallSlot
    {

        float literCapacity;
        public LiquidStallSlot(InventoryBase inventory, int stallSlot, float literCapacity) : base(inventory, stallSlot, 1)
        {
            slots = new ViconItemSlot[1];
            slots[0] = new ViconItemSlot(inventory, stallSlot, 0);
            this.literCapacity = literCapacity;
        }

        public void RemoveLiters(int litres)
        {
            ItemStack stack = slots[0].Itemstack;

            WaterTightContainableProps contentProps = GetContainableProps(slots[0].Itemstack);
            if (contentProps == null)
            {
                return;
            }
            stack.StackSize -= (int)(litres * contentProps.ItemsPerLitre);
            slots[0].MarkDirty();
        }

        public void AddLiters(float litres)
        {
            ItemStack stack = slots[0].Itemstack;

            WaterTightContainableProps contentProps = GetContainableProps(slots[0].Itemstack);
            if (contentProps == null)
            {
                return;
            }
            stack.StackSize += (int)(litres * contentProps.ItemsPerLitre);
            slots[0].MarkDirty();
        }


        public static WaterTightContainableProps GetContainableProps(ItemStack stack)
        {
            try
            {
                JsonObject jsonObject = stack?.ItemAttributes?["waterTightContainerProps"];
                if (jsonObject != null && jsonObject.Exists)
                {
                    return jsonObject.AsObject<WaterTightContainableProps>(null, stack.Collectible.Code.Domain);
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}