using Commercially.Common.Interfaces;
using Commercially.Vinconomy.Interfaces;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Interactions
{
    public class PurchaseItemInteraction : IInteraction
    {
        public const string Key = "Vinconomy.PurchaseItem";

        public bool CanHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;
            
            IPlayer byPlayer = caller.Player;
            ItemStack itemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            return stallComponent?.GetStallSlot(index)?.Currency.Itemstack != null;
        }

        public bool ShouldHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;

            IPlayer byPlayer = caller.Player;
            bool shiftMod = byPlayer.Entity.Controls.Sneak;

            if (!shiftMod) return false;


            ItemStack itemStack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            bool isCoupon = itemStack?.Collectible.Code == "vinconomy:coupon";
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            return stallComponent?.GetStallSlot(index)?.Currency.Itemstack?.Satisfies(itemStack) ?? false;

        }

        public int GetInteractionCount(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            ItemStack currency = stallComponent?.GetStallSlot(index)?.Currency.Itemstack;
            return currency == null ? 0 : 2;
        }

        public WorldInteraction[] GetInteractions(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            IStallComponent stallComponent = blockEntity.GetBehavior<IStallComponent>();
            int index = stallComponent.GetStallIndexFromSelection(blockSel.SelectionBoxIndex);
            ItemStack currency = stallComponent?.GetStallSlot(index)?.Currency.Itemstack;
            if (currency == null) return Array.Empty<WorldInteraction>();
                
            ItemStack singleStack = currency?.Clone();
            ItemStack fiveStack = currency?.Clone();
            fiveStack.StackSize *= 5;

            ItemStack singleCoupon = new ItemStack(world.GetItem(new AssetLocation("vinconomy:coupon")), 1);
            ItemStack fiveCoupon = singleCoupon.Clone();
            fiveCoupon.StackSize = 5;


            WorldInteraction[] interactions =
            [
                new WorldInteraction()
                {
                    ActionLangCode = "vinconomy:stall-purchase",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak",
                    Itemstacks = [singleStack, singleCoupon]
                },
                new WorldInteraction
                {
                    ActionLangCode = "vinconomy:stall-purchase-bulk",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCodes = ["sneak", "sprint"],
                    Itemstacks = [fiveStack, fiveCoupon]
                },
            ];

            return interactions;

        }

        public bool Interact(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;

            IPlayer byPlayer = caller.Player;
            if (byPlayer.Entity.Api.Side != EnumAppSide.Server) return true; // Should be handled, just not on client!

            bool shiftMod = byPlayer.Entity.Controls.Sneak;

            if (!shiftMod) return false;

            bool ctrlMod = byPlayer.Entity.Controls.Sprint;
            IStallComponent stall = blockEntity.GetBehavior<IStallComponent>();
            stall.TryPurchaseItem(byPlayer, stall.GetStallIndexFromSelection(blockSel.SelectionBoxIndex), ctrlMod ? 1 : 5);


            return true;
        }


    }
}
