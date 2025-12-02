using System;
using Viconomy.Inventory.Slots;
using Viconomy.Trading.TradeHandlers;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.StallSlots
{
    public class LiquidStallSlot : ItemStallSlot
    {

        public float LiterCapacity { get; protected set; }
        public LiquidStallSlot(InventoryBase inventory, int stallSlot, float literCapacity) : base(inventory, stallSlot, 1)
        {
            slots = new ViconItemSlot[1];
            slots[0] = new ViconItemSlot(inventory, stallSlot, 0);
            this.LiterCapacity = literCapacity;
        }

        public ItemStack RemoveLiters(float liters)
        {

            int totalItems = LiquidTradeHandler.ConvertLitersToStack(slots[0].Itemstack, liters);
            ItemStack stack = slots[0].TakeOut(totalItems);
            slots[0].MarkDirty();
            return stack;
        }

        public float GetLiters()
        {
            return LiquidTradeHandler.ConvertStackToLiters(slots[0].Itemstack);
        }

        public float AddLiters(ItemStack containerStack, float litres)
        {

            WaterTightContainableProps contentProps = BlockLiquidContainerBase.GetContainableProps(containerStack);
            if (contentProps == null)
            {
                return 0;
            }
            int toAdd = (int)(litres * contentProps.ItemsPerLitre);
            int maxCapacity = (int)(LiterCapacity * contentProps.ItemsPerLitre);

            ItemStack stack = slots[0].Itemstack;
            if (stack != null)
            {
                toAdd = Math.Min(toAdd, maxCapacity - stack.StackSize);
                stack.StackSize += toAdd;
            } else
            {
                ItemStack newStack = containerStack.Clone();
                newStack.StackSize = toAdd;
                slots[0].Itemstack = newStack;
            }

            slots[0].MarkDirty();
            return toAdd / contentProps.ItemsPerLitre;
        }



    }
}