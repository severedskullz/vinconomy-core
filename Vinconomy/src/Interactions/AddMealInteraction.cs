
using Commercially.Common.Interfaces;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.Interactions
{
    public class AddMealInteraction : IInteraction
    {
        public const string Key = "Vinconomy.AddMeal";

        public bool CanHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute? activationArgs = null)
        {
            if (caller.Type == EnumCallerType.Player)
            {
                IPlayer byPlayer = caller.Player;
                ItemStack itemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
                IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();

                //TODO: Convert to some sort of "Can Manage" check, so that admins and authorized players can manage stalls they don't own.
                if (stallComponent.Ownable.OwnerUID == byPlayer.PlayerUID) return true;
            }
            return false;
        }

        public bool ShouldHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute? activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;

            IPlayer byPlayer = caller.Player;
            bool shiftMod = byPlayer.Entity.Controls.Sneak;

            if (!shiftMod) return false;

            ItemStack itemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            return stallComponent?.GetStallSlot<MealStallSlot>(index)?.CanAcceptFrom(byPlayer.InventoryManager.ActiveHotbarSlot) ?? false;

        }

        public int GetInteractionCount(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute? activationArgs = null)
        {
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();

            //TODO: Convert to some sort of "Can Manage" check, so that admins and authorized players can manage stalls they don't own.
            if (stallComponent.Ownable.OwnerUID != caller.Player.PlayerUID) return 0;

            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            ItemStack currency = stallComponent?.GetStallSlot(index)?.Product.Itemstack;
            return currency == null ? 0 : 2;
        }

        public WorldInteraction[] GetInteractions(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute? activationArgs = null)
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
                    ActionLangCode = "vinconomy:stall-add",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak",
                    Itemstacks = [singleStack]
                },
                new WorldInteraction
                {
                    ActionLangCode = "vinconomy:stall-add-bulk",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCodes = ["sneak", "sprint"],
                    Itemstacks = [fullStack]
                },
            ];

            return interactions;

        }

        public bool Interact(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;

            //public class BlockCookedContainer : BlockCookedContainerBase, IInFirepitRendererSupplier, IContainedMeshSource, IGroundStoredParticleEmitter, IAttachableToEntity
            //public class BlockCookedContainerBase : BlockContainer, IBlockMealContainer, IContainedInteractable, IContainedCustomName, IHandBookPageCodeProvider
            //public class BlockCrock : BlockCookedContainerBase, IBlockMealContainer, IContainedMeshSource
            //public class BlockMeal : BlockContainer, IBlockMealContainer, IContainedMeshSource, IContainedInteractable, IContainedCustomName, IGroundStoredParticleEmitter, IHandBookPageCodeProvider

            IPlayer byPlayer = caller.Player;
            bool shiftMod = byPlayer.Entity.Controls.Sneak;
            bool ctrlMod = byPlayer.Entity.Controls.Sprint;
            ItemStack itemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            IBlockMealContainer mealContainer = itemStack?.Block as IBlockMealContainer;
            if (mealContainer != null) return false;

            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            ItemSlot productSlot = stallComponent?.GetStallSlot(index)?.Product;

            BlockCookedContainerBase productPot = productSlot?.Itemstack?.Block as BlockCookedContainerBase;
            if (productPot != null) return false;

            return productPot.ServeIntoStack(productSlot, byPlayer.InventoryManager.ActiveHotbarSlot, world);


        }
    }
}
