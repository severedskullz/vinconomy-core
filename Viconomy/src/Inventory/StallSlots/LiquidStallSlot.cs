using System;
using Viconomy.Inventory.Slots;
using Viconomy.Trading.TradeHandlers;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.StallSlots
{
    public class LiquidStallSlot : StallSlotBase
    {
        ViconLockedSlot Liquid;
        public float LiterCapacity { get; protected set; }

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < ProductStacksPerStall)
                {
                    return Liquid;
                }
                else
                    return Currency;

            }
            set
            {
                if (slotId < ProductStacksPerStall)
                {
                    Liquid = (ViconLockedSlot)value;
                }
                else
                    Currency = (ViconCurrencySlot)value;
            }
        }

        public LiquidStallSlot(InventoryBase inventory, int stallSlot, float literCapacity) : base(inventory, 1)
        {
            Liquid = new ViconLockedSlot(inventory, stallSlot);
            this.LiterCapacity = literCapacity;
        }

        public ItemStack RemoveLiters(float liters)
        {

            int totalItems = LiquidTradeHandler.ConvertLitersToStack(Liquid.Itemstack, liters);
            ItemStack stack = Liquid.TakeOut(totalItems);
            Liquid.MarkDirty();
            return stack;
        }

        public float GetLiters()
        {
            return LiquidTradeHandler.ConvertStackToLiters(Liquid.Itemstack);
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

            ItemStack stack = Liquid.Itemstack;
            if (stack != null)
            {
                toAdd = Math.Min(toAdd, maxCapacity - stack.StackSize);
                stack.StackSize += toAdd;
            } else
            {
                ItemStack newStack = containerStack.Clone();
                newStack.StackSize = toAdd;
                Liquid.Itemstack = newStack;
            }

            Liquid.MarkDirty();
            return toAdd / contentProps.ItemsPerLitre;
        }

        public override void SetSlot(int itemSlot, ItemSlot value)
        {
            Liquid = (ViconLockedSlot)value;
        }

        public override ItemSlot GetSlot(int itemSlot)
        {
            return Liquid;
        }

        public override ItemSlot[] GetSlots()
        {
            return [Liquid];
        }

        public override ItemSlot FindFirstNonEmptyStockSlot()
        {
            if (Liquid == null)
                return null;

            return Liquid;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("purchaseQuantity", ItemsPerPurchase);
            if (Currency.Itemstack != null)
            {
                tree.SetItemstack("currency", Currency.Itemstack.Clone());
            }

            tree.SetItemstack("slot0", Liquid.Itemstack?.Clone());
            
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            ItemsPerPurchase = tree.GetInt("purchaseQuantity", 1);

            Currency.Itemstack = tree.GetItemstack("currency");

            ItemStack itemStack = tree.GetItemstack("slot0");
            Liquid.Itemstack = itemStack;

           
        }
    }
}