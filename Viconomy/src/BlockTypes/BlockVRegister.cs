using System.Text;
using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockTypes
{
    public class BlockVRegister : TextureSwappableBlock
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

            string Owner = byItemStack.Attributes.GetString("Owner");
            if (Owner != null && byPlayer.PlayerUID != Owner)
            {
                if (api.Side == EnumAppSide.Server)
                    ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("vinconomy:doesnt-own", new object[0]), EnumChatType.CommandError, null);

                return false;
            }


            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {

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
            ItemStack stack = new ItemStack(world.GetBlock(new AssetLocation(Code.Domain, this.CodeWithoutParts(1) +"-north")));
            BEVinconRegister vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEVinconRegister;
            if (vEntity != null)
            {
                stack.Attributes.SetString("Owner", vEntity.Owner);
                stack.Attributes.SetInt("ID", vEntity.ID);
                stack.Attributes.SetString("OwnerName", vEntity.OwnerName);
                VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
                stack.Attributes.SetString("ShopName", modSystem.GetRegistry().GetShopName(vEntity.ID));
                AddTextureAttributes(stack, vEntity);
            }
                
            return new ItemStack[] { stack };
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
