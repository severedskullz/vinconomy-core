
using Commercially.Common.Interfaces;
using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Interactions
{
    public class RemoveStockInteraction : IInteraction
    {
        public const string Key = "Vinconomy.RemoveStock";

        public bool CanHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type == EnumCallerType.Player)
            {
                IPlayer byPlayer = caller.Player;
                ItemStack itemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
                IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
                int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
                return stallComponent?.GetStallSlot(index)?.Product.Itemstack != null;
            }
            return false;
        }

        public bool ShouldHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;

            IPlayer byPlayer = caller.Player;
            bool shiftMod = byPlayer.Entity.Controls.Sneak;

            if (!shiftMod) return false;

            ItemStack itemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            return stallComponent?.GetStallSlot(index).MatchesProduct(itemStack) ?? false;

        }

        public int GetInteractionCount(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            ItemStack currency = stallComponent?.GetStallSlot(index)?.Product.Itemstack;
            return currency == null ? 0 : 2;
        }

        public WorldInteraction[] GetInteractions(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            ItemStack product = stallComponent?.GetStallSlot(index)?.Product.Itemstack;
            if (product == null) return Array.Empty<WorldInteraction>();

            ItemStack singleStack = product?.Clone();
            ItemStack fullStack = product?.Clone();
            fullStack.StackSize = 64;

            WorldInteraction[] interactions =
            [
                new WorldInteraction()
                {
                    ActionLangCode = "vinconomy:stall-remove",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak",
                    Itemstacks = [singleStack]
                },
                new WorldInteraction
                {
                    ActionLangCode = "vinconomy:stall-remove-bulk",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCodes = ["sneak", "sprint"],
                    Itemstacks = [fullStack]
                },
            ];

            return interactions;

        }

        public bool Interact(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;

            IPlayer byPlayer = caller.Player;
            bool shiftMod = byPlayer.Entity.Controls.Sneak;
            bool ctrlMod = byPlayer.Entity.Controls.Sprint;

            if (!shiftMod) return false;

            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            BaseStallSlot stallSlot = stallComponent?.GetStallSlot(index);

            AggregatedStacks returnedStacks = stallSlot.ExtractProduct(1, 1, false);
            while (returnedStacks.CanRemoveStack()) 
            {
                ItemStack stack = returnedStacks.RemoveStack();
                byPlayer.InventoryManager.TryGiveItemstack(stack, true);
                if (stack.StackSize > 0)
                {
                    //Console.WriteLine("Should have spawned item for " + String.Format("{0}-{1}-{2}", x, y, z));
                    world.SpawnItemEntity(stack, blockEntity.Pos.AddCopy(0.0f, 0.5f, 0.0f), null);
                }
            }
            return returnedStacks.TotalCount > 0;
        }
    }
}
