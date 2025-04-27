using System;
using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockTypes
{
    public class BlockVTeller : TextureSwappableBlock
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEVinconTeller be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconTeller;
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

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {
                string Owner = byItemStack.Attributes.GetString("Owner");
                
                BEVinconTeller vEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconTeller;
                if (vEntity != null)
                {
                    if (Owner != null)
                    {
                        vEntity.UpdateTeller(Owner, byItemStack.Attributes.GetString("OwnerName"));                        
                    }
                    else
                    {
                        vEntity.UpdateTeller(byPlayer.PlayerUID, byPlayer.PlayerName);
                    }
                       
                }
            }

            return result;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                return null;
            }
            ;
            ItemStack stack = new ItemStack(world.GetBlock(new AssetLocation(Code.Domain, this.CodeWithoutParts(1) +"-north")));
            BEVinconTeller vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEVinconTeller;
            if (vEntity != null)
            {
                stack.Attributes.SetString("Owner", vEntity.Owner);
                stack.Attributes.SetInt("ID", vEntity.RegisterID);
                stack.Attributes.SetString("OwnerName", vEntity.OwnerName);
                AddTextureAttributes(stack, vEntity);
            }
                
            return new ItemStack[] { stack };
        }

    }
}
