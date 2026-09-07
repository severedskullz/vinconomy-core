using Commercially.Common.Blocks.BlockEntities;
using Commercially.Common.Interfaces;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Common.Interactions
{
    public class OpenStallInteraction : IInteraction
    {
        public const string Key = "Vinconomy.OpenStall";

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

            string ownerTab = properties?["ownerTab"] != null ? properties["ownerTab"].AsString() : "Vinconomy.ShopOwner";
            string customerTab = properties?["customerTab"] != null ? properties["customerTab"].AsString() : "Vinconomy.ShopCustomer";
            string tab = isOwner ? ownerTab : customerTab;

            return manager.OpenGUI(blockEntity as BECommercialBase, caller, blockSel, key, tab);
        }

        public bool ShouldHandle(IWorldAccessor world, Caller caller, BlockEntity blockEntity, BlockSelection blockSel, string key = "default", JsonObject? properties = null, ITreeAttribute activationArgs = null)
        {
            return true;
        }
    }
}
