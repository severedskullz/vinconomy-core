using Viconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.BlockTypes
{
    public class BlockVFoodContainer : BlockVContainer
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            //Console.WriteLine(api.Side + ": On interaction start was called!");
            BEVinconFoodContainer be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconFoodContainer;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return true;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (selection.SelectionBoxIndex == 0)
            {
                return null;
            }
            else
            {
                selection.SelectionBoxIndex--;
                return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
            }
        }
    }
}
