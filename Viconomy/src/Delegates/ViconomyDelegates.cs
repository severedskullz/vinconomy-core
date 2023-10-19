using Viconomy.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace Viconomy.Delegates
{
    /*
     *  Called whenever an item is purchased from a stall, regardless if it is assoicated with a Register (Nullable)
     */
    public delegate void OnPurchasedItemDelegate(IPlayer player, BEViconStall stall, BEVRegister register, ItemStack product, ItemStack payment);

    /*
     *  Called whenever a player attempts to purchase from a stall, regardless if it is assoicated with a Register (Nullable). Return true to allow the purchase;
     */
    public delegate bool CanPurchaseItemDelegate(IPlayer player, BEViconStall stall, BEVRegister register, ItemSlot product, ItemSlot payment);

    /*
     * Called when a product is purchased and payment is sent to a register.
     */
    public delegate void OnRecordPurchaseDelegate(IPlayer player, BEViconStall stall, ItemStack product, ItemStack payment);

    public delegate bool TryPlaceBlockDelegate(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel);
    public delegate bool OnBlockBrokenDelegate(AssetLocation code, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier);
    public delegate void OnBlockPlacedDelegate(AssetLocation code, IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack);
    public delegate EnumWorldAccessResponse OnTestAccessDelegate(IPlayer player, BlockSelection blockSelection, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response);


}