using Vinconomy.BlockEntities;
using Vintagestory.API.Common;

namespace Vinconomy.BlockTypes
{
    public class BlockVGachaLoader : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEVinconGachaLoader be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconGachaLoader;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return true;
        }

    }
}
