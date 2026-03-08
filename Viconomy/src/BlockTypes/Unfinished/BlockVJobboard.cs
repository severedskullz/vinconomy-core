using System.Collections.Generic;
using Vinconomy.BlockEntities;
using Vinconomy.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vinconomy.BlockTypes
{
    public class BlockVJobboard : Block
    {
        public BlockVJobboard()
        {
            this.PlacedPriorityInteract = true;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEVinconJobboard be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconJobboard;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return true;
        }
    }
}
