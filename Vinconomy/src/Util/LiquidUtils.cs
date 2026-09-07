using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.Util
{
    public static class LiquidUtils
    {
        public static bool IsLiquidContainer(ItemStack stack)
        {
            return stack?.Block is BlockLiquidContainerBase container;
        }

        public static bool IsEmptyLiquidContainer(ItemStack stack)
        {
            return IsLiquidContainer(stack) && ((BlockLiquidContainerBase)stack.Block).GetCurrentLitres(stack) == 0;
        }

        public static int GetStackSizeFromLiters(ItemStack stack, float liters)
        {
            WaterTightContainableProps contentProps = BlockLiquidContainerBase.GetContainableProps(stack);
            if (contentProps == null)
            {
                return 0;
            }
            return (int)(liters * contentProps.ItemsPerLitre);
        }


        public static float GetLitersFromStackSize(ItemStack stack)
        {
            return GetLitersFromStackSize(stack, stack.StackSize);
        }

        public static float GetLitersFromStackSize(ItemStack stack, int numItems)
        {
            WaterTightContainableProps contentProps = BlockLiquidContainerBase.GetContainableProps(stack);
            if (contentProps == null)
            {
                return 0;
            }
            return numItems / contentProps.ItemsPerLitre;
        }

        public static int TransferLiquidToItemStack(ItemStack containerStack, ItemStack liquidStacks)
        {
            Block block = containerStack?.Block;
            if (block == null)
            {
                return 0;
            }

            if (block is BlockLiquidContainerBase container)
            {

                if (container.GetCurrentLitres(containerStack) >= container.CapacityLitres)
                    return 0;

                int moved = container.TryPutLiquid(containerStack, liquidStacks, ConvertStackToLiters(liquidStacks));
                return moved;

            }
            return 0;
        }

        /// <summary>
        /// Transfers contents from the stall into a container aquired from containerSlot
        /// 
        /// Returns the ItemStack with the container filled with the contents. Since empty (And some full) containers may stack,
        /// the ItemStack will be taken out of the containerSlot. If containerSlot only had 1 item, the slot would be empty by the end
        /// of a successful operation
        /// </summary>
        /// <returns></returns>
        public static ItemStack TransferLiquidContentsToContainer(ItemSlot containerSlot, ItemStack contents, int amount, out int amountMoved)
        {
            amountMoved = 0;
            if (containerSlot?.Itemstack == null)
                return null;

            BlockLiquidContainerBase container = containerSlot.Itemstack?.Block as BlockLiquidContainerBase;
            if (container == null)
                return null;

            ItemStack curContainer = containerSlot.TakeOut(1);

            amountMoved = container.TryPutLiquid(curContainer, contents, amount);

            return curContainer;
        }

        /// <summary>
        /// Removes contents from the container
        /// 
        /// Returns the ItemStack with the container with the contents removed. Since some full containers may stack,
        /// the ItemStack will be taken out of the containerSlot. If containerSlot only had 1 item, the slot would be empty by the end
        /// of a successful operation
        /// </summary>
        /// <returns></returns>
        public static ItemStack TransferLiquidContentsToStack(ItemSlot containerSlot, int amount, out ItemStack removedContents)
        {
            removedContents = null;
            if (containerSlot?.Itemstack == null)
                return null;

            BlockLiquidContainerBase container = containerSlot.Itemstack?.Block as BlockLiquidContainerBase;
            if (container == null)
                return null;

            ItemStack curContainer = containerSlot.TakeOut(1);
            ItemStack content = container.GetContent(curContainer);
            float liters = GetLitersFromStackSize(content, amount);
            removedContents = container.TryTakeLiquid(curContainer, liters);

            return curContainer;
        }


        public static int TransferToLiquidContainer(IPlayer player, ItemSlot containerSlot, ItemStack liquidStacks)
        {
            ICoreAPI api = player.Entity.Api;
            if (containerSlot == null)
                return 0;

            if (liquidStacks == null)
                return 0;

            if (containerSlot.StackSize == 1)
            {
                return TransferLiquidToItemStack(containerSlot.Itemstack, liquidStacks);
            }
            else
            {
                ItemStack containerStack = containerSlot.TakeOut(1);
                int moved = TransferLiquidToItemStack(containerStack, liquidStacks);
                if (!player.InventoryManager.TryGiveItemstack(containerStack))
                {
                    api.World.SpawnItemEntity(containerStack, player.Entity.Pos.XYZ.AddCopy(0.5, 0.5, 0.5), null);
                }
                return moved;

            }
        }

        public static float GetItemsPerLiter(ItemStack stack)
        {
            if (stack == null) return 0;

            WaterTightContainableProps contentProps = BlockLiquidContainerBase.GetContainableProps(stack);
            if (contentProps == null)
            {
                return 0;
            }
            return contentProps.ItemsPerLitre; // Tyron why the fuck is this a float? You cant have .1 of a StackSize
        }


        public static float ConvertStackToLiters(ItemStack stack)
        {
            WaterTightContainableProps contentProps = BlockLiquidContainerBase.GetContainableProps(stack);
            if (contentProps == null)
            {
                return 0;
            }
            return stack.StackSize / contentProps.ItemsPerLitre;
        }

        public static bool CanContainerHoldLiquid(IWorldAccessor world, ItemStack sourceContainer, ItemStack liquidContents)
        {
            if (sourceContainer == null)
                return false;

            BlockLiquidContainerBase container = sourceContainer.Block as BlockLiquidContainerBase;
            if (container == null)
                return false;

            if (container.GetContent(sourceContainer) == null)
                return true;

            if (container.GetCurrentLitres(sourceContainer) >= container.CapacityLitres)
                return false;

            return liquidContents.Equals(world, container.GetContent(sourceContainer), GlobalConstants.IgnoredStackAttributes);
        }
    }
}
