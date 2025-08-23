using Viconomy.BlockEntities.Unfinished;
using Vintagestory.API.Common;

namespace Viconomy.BlockTypes
{
    public class BlockVCouponCutter : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEVinconCouponCutter be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconCouponCutter;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return true;
        }

    }
}
