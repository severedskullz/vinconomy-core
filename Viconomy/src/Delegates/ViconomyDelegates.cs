using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Viconomy.Delegates
{
    public delegate bool OnPurchasedItemDelegate(IPlayer player, BEViconStall stall, BEVRegister register, ItemStack product, ItemStack payment);
    public delegate bool CanPurchaseItemDelegate(IPlayer player, BEViconStall stall, BEVRegister register, ItemSlot product, ItemSlot payment);
    public delegate bool TryPlaceBlockDelegate(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel);
    public delegate bool OnBlockBrokenDelegate(AssetLocation code, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier);
    public delegate void OnBlockPlacedDelegate(AssetLocation code, IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack);

}