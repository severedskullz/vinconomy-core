using Commercially.Common.Blocks.BlockEntities;
using Commercially.Common.Interfaces;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Commercially.Common.Interactions
{
    public class OpenRegisterInteraction : IInteraction
    {
        public const string Key = "Vinconomy.OpenRegister";

        public bool CanHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            return true;
        }

        public int GetInteractionCount(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            return 1;
        }

        public WorldInteraction[] GetInteractions(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
           return
           [
               new WorldInteraction()
                {
                    ActionLangCode = "vinconomy:stall-open",
                    MouseButton = EnumMouseButton.Right,
                }
            ];
        }

        public bool Interact(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            IGUIManager manager = blockEntity.GetBehavior<IGUIManager>();
            bool isOwner = blockEntity.GetBehavior<IOwnableReference>()?.OwnerUID == caller.Player?.PlayerUID;
            if (isOwner)
            {
                manager.OpenGUI(blockEntity as BECommercialBase, caller, blockSel, key);
            } else
            {
                if (world.Api.Side == EnumAppSide.Server)
                    ((IServerPlayer)caller.Player)?.SendMessage(0, Lang.Get("commercially:doesnt-own", []), EnumChatType.OwnMessage);

            }
            return true;
        }

        public bool ShouldHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            return true;
        }
    }
}
