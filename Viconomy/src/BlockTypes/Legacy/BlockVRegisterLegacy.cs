using System.Text;
using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockTypes.Legacy
{
    public class BlockVRegisterLegacy : Block
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BEVinconRegister be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconRegister;
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
                BEVinconRegister vEntity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconRegister;
                if (vEntity != null)
                {
                    if (Owner != null)
                    {
                        vEntity.UpdateShop(Owner, byItemStack.Attributes.GetString("OwnerName"), byItemStack.Attributes.GetInt("ID"), byItemStack.Attributes.GetString("ShopName"));
                    }
                    else
                    {
                        vEntity.UpdateShop(byPlayer.PlayerUID, byPlayer.PlayerName, -1, null);
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
            ItemStack stack = new ItemStack(world.GetBlock(new AssetLocation(Code.Domain, CodeWithoutParts(1) + "-east")));
            BEVinconRegister vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEVinconRegister;
            if (vEntity != null)
            {
                stack.Attributes.SetString("Owner", vEntity.Owner);
                stack.Attributes.SetInt("ID", vEntity.ID);
                stack.Attributes.SetString("OwnerName", vEntity.OwnerName);
                VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
                stack.Attributes.SetString("ShopName", modSystem.GetRegistry().GetShopName(vEntity.ID));
            }

            return new ItemStack[] { stack };
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (byPlayer == null)
                return;

            BEVinconRegister vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEVinconRegister;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID || byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
                if (modSystem != null && !modSystem.BlockBroken(Code, world, pos, byPlayer, dropQuantityMultiplier))
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
            VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            if (modSystem != null)
            {
                modSystem.BlockPlaced(Code, world, blockPos, byItemStack);
            }
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            if (modSystem != null && !modSystem.TryPlaceBlock(world, byPlayer, itemstack, blockSel))
            {
                failureCode = "__ignore__";
                return false;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ITreeAttribute attrs = inSlot.Itemstack.Attributes;
            dsc.AppendLine("Owner: " + attrs.GetString("OwnerName", "None"));
            dsc.AppendLine("Shop: " + attrs.GetString("ShopName", "None"));
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);


        }

    }
}
