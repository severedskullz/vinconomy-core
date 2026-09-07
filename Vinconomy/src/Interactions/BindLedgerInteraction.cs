using Commercially.Common.Interfaces;
using Commercially.Common.ModSystems;
using Commercially.Common.Registry;
using Commercially.Vinconomy.Interfaces;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Commercially.Vinconomy.Interactions
{
    public class BindLedgerInteraction : IInteraction
    {
        public const string Key = "Vinconomy.BindLedger";

        public bool CanHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;

            ItemStack? itemStack = caller.Player.InventoryManager.ActiveHotbarSlot.Itemstack;
            return itemStack?.Collectible.Code == "vinconomy:ledger";
        }

        public bool ShouldHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;

            IPlayer byPlayer = caller.Player;
            bool shiftMod = byPlayer.Entity.Controls.Sneak;

            if (!shiftMod) return false;

            return true;
        }

        public int GetInteractionCount(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            return 1;
        }

        public WorldInteraction[] GetInteractions(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            ItemStack ledger = new ItemStack(world.GetItem(new AssetLocation("vinconomy:ledger")), 1);

            WorldInteraction[] interactions =
            [
                new WorldInteraction()
                {
                    ActionLangCode = "vinconomy:bind-ledger",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak",
                    Itemstacks = [ledger]
                }
            ];

            return interactions;

        }

        public bool Interact(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            if (caller.Type != EnumCallerType.Player) return false;
   
            IPlayer byPlayer = caller.Player;
            bool shiftMod = byPlayer.Entity.Controls.Sneak;
            ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (shiftMod && (handSlot.Itemstack?.Item?.Code.ToString() == "vinconomy:ledger" || handSlot.Itemstack?.Item?.Code.ToString() == "vinconomy:catalog"))
            {
                if (world.Api.Side == EnumAppSide.Server)
                {
                    IShopComponent component = blockEntity.GetBehavior<IShopComponent>();
                    IOwnableReference ownable = component.Ownable;
                    IServerPlayer player = ((IServerPlayer)byPlayer);
                    if (ownable.IsOwner(player))
                    {
                        if (handSlot.Itemstack.Attributes.GetInt("ShopId", -1) <= 0)
                        {
                            CommerciallyModSystem modSystem = world.Api.ModLoader.GetModSystem<CommerciallyModSystem>();
                            OwnableRegistration shop = modSystem.OwnableRegistry.GetOwnable(ownable.ID);
                            player.SendMessage(0, Lang.Get("vinconomy:ledger-set", [shop.Name]), EnumChatType.OwnMessage);
                            handSlot.Itemstack.Attributes.SetLong("ShopId", ownable.ID);
                            handSlot.Itemstack.Attributes.SetString("Owner", ownable.OwnerUID);
                            handSlot.Itemstack.Attributes.SetString("ShopName", shop.Name);
                            handSlot.MarkDirty();
                        }
                        else
                        {
                            player.SendMessage(0, Lang.Get("vinconomy:ledger-already-set", []), EnumChatType.OwnMessage);
                        }
                    }
                    else
                    {
                        player.SendMessage(0, Lang.Get("vinconomy:doesnt-own", []), EnumChatType.OwnMessage);
                    }
                }

                return true;
            }
            return false;
        }
    }
}
