using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.BlockTypes
{
    public class BlockVGeneric : BlockVBase
    {
        public BlockVGeneric()
        {
            this.PlacedPriorityInteract = true;
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //Console.WriteLine(api.Side + ": On interaction start was called!");
            IInteractableStall be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as IInteractableStall;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return true;
        }
    }
}
