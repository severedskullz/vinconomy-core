
using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockTypes
{
    public class BlockVBase : Block
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (byPlayer == null)
                return;

            IOwnable vEntity = world.BlockAccessor.GetBlockEntity(pos) as IOwnable;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID || byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
                if (modSystem != null && !modSystem.BlockBroken(this.Code, world, pos, byPlayer, dropQuantityMultiplier))
                {
                    return;
                }
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            }

            else if (api.Side == EnumAppSide.Server)
                ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("vinconomy:doesnt-own", []), EnumChatType.CommandError, null);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            if (modSystem != null)
            {
                modSystem.BlockPlaced(this.Code, world, blockPos, byItemStack);
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
    }
}
