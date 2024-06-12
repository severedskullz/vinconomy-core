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
    public class BlockVGachaLoader : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEViconGachaLoader be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconGachaLoader;
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
                
                BEViconTeller vEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconTeller;
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
            ItemStack stack = new ItemStack(world.GetBlock(new AssetLocation(Code.Domain, this.CodeWithoutParts(1) +"-east")));
            BEViconTeller vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEViconTeller;
            if (vEntity != null)
            {
                stack.Attributes.SetString("Owner", vEntity.Owner);
                stack.Attributes.SetInt("ID", vEntity.RegisterID);
                stack.Attributes.SetString("OwnerName", vEntity.OwnerName);
            }
                
            return new ItemStack[] { stack };
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (byPlayer == null)
                return;

            BEViconTeller vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEViconTeller;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID || byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                ViconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
                if (modSystem != null && !modSystem.BlockBroken(this.Code, world, pos, byPlayer, dropQuantityMultiplier))
                {
                    return;
                }
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            }

            else if (api.Side == EnumAppSide.Server)
                ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("vinconomy:doesnt-own", new object[0]), EnumChatType.CommandError, null);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            ViconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
            if (modSystem != null)
            {
                modSystem.BlockPlaced(this.Code, world, blockPos, byItemStack);
            }
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            ViconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
            if (modSystem != null && !modSystem.TryPlaceBlock(world, byPlayer, itemstack, blockSel))
            {
                failureCode = "__ignore__";
                return false;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

    }
}
