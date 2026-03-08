using Vinconomy.BlockEntities;
using Vintagestory.API.Common;

namespace Vinconomy.BlockTypes
{
    public class BlockVTradeCenter : BlockVBase
    {
        public BlockVTradeCenter()
        {
            this.PlacedPriorityInteract = true;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be != null)
            {
                return ((BEVinconTradeCenter)be).OnPlayerRightClick(byPlayer, blockSel);
            }
            return true;
        }

    }
}
