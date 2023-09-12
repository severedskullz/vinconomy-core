using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.BlockTypes
{
    public class BlockVRegister : Block
    {

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            Console.WriteLine(api.Side + ": On interaction start was called!");
            return true;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {
                string Owner = byItemStack.Attributes.GetString("Owner");
                BEVRegister viconBlock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVRegister;
                if (viconBlock != null)
                {
                    if (Owner != null)
                    {
                        viconBlock.Owner = Owner;
                        viconBlock.OwnerName = byItemStack.Attributes.GetString("OwnerName");
                        viconBlock.ID = byItemStack.Attributes.GetString("ID");
                        
                    }
                        
                    else
                    {
                        viconBlock.Owner = byPlayer.PlayerUID;
                        viconBlock.OwnerName = byPlayer.PlayerName;
                        viconBlock.Name = byPlayer.PlayerName + "'s Shop";
                    }
                       
                }
            }

            return result;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
}
