using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockTypes
{
    public class BlockVRegister : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            Console.WriteLine(api.Side + ": On interaction start was called!");
            BEVRegister be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVRegister;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return true;
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            Console.WriteLine(api.Side + ": On Block Placed called!");
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {
                string Owner = byItemStack.Attributes.GetString("Owner");
                BEVRegister vEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVRegister;
                if (vEntity != null)
                {
                    if (Owner != null)
                    {
                        vEntity.UpdateRegister(Owner, byItemStack.Attributes.GetString("OwnerName"), byItemStack.Attributes.GetString("ID"), null);                        
                    }
                    else
                    {
                        vEntity.UpdateRegister(byPlayer.PlayerUID, byPlayer.PlayerName, null, null);
                    }
                       
                }
            }

            return result;
        }



        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BEVRegister vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEVRegister;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID)
                    base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            else if (api.Side == EnumAppSide.Server)
                ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("You do not own this register", new object[0]), EnumChatType.CommandError, null);
        }

        
    }
}
