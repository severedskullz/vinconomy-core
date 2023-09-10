using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Vintagestory.API.Common;

namespace Viconomy.BlockTypes
{
    public class BlockVRegister : Block
    {

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            Console.WriteLine(api.Side + ": On interaction start was called!");
            return true;
        }
    }
}
