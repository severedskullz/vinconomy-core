using Viconomy.Network.JavaApi.TradeNetwork;
using Vinconomy.BlockEntities;
using Vinconomy.Inventory;
using Vinconomy.Registry;
using Vinconomy.Trading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vinconomy.Delegates
{


    /*
     *  Called whenever an item is purchased from a stall, regardless if it is assoicated with a Register (Nullable)
     */
    public delegate void OnPurchasedItemDelegate(GenericTradeResult result, ItemStack product, ItemStack payment);

    /*
     *  Called whenever a player attempts to purchase from a stall, regardless if it is assoicated with a Register (Nullable). Return true to allow the purchase;
     */
    public delegate bool CanPurchaseItemDelegate(IPlayer player, BEVinconBase stall, BEVinconRegister register, int productSlot, int numPurchases);

    /*
     * Called when a product is purchased and payment is sent to a register. Changes to Payment stack will be persisted. Can be used for things like subtracting Taxes or the like.
     */
    public delegate void OnRecordPurchaseDelegate(IPlayer player, BEVinconBase stall, ItemStack productClone, ItemStack payment);

    public delegate bool TryPlaceBlockDelegate(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, bool multicastResult);
    public delegate bool OnBlockBrokenDelegate(AssetLocation code, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier);
    public delegate void OnBlockPlacedDelegate(AssetLocation code, IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack);
    public delegate EnumWorldAccessResponse OnTestAccessDelegate(IPlayer player, BlockSelection blockSelection, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response);

    public delegate void OnUpdateShopDelegate(ShopRegistration shop);

    public delegate void OnTradeSelectedDelegate(VinconNetworkItemSlot product);

    public delegate void OnUpdateShopProductDelegate(BEVinconBase stall, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency);
    public delegate void OnTradeNetworkShopRecieved(TradeNetworkShop shop);
}