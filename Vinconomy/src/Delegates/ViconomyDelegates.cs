using Commercially.Common.Interfaces;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Trading;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vinconomy.Delegates
{

    /*
     *  Called whenever an item is purchased from a stall, regardless if it is assoicated with a Register (Nullable)
     */
    public delegate void OnPurchasedItemDelegate(TradeResult result, ItemStack product, ItemStack payment);

    /*
     *  Called whenever a player attempts to purchase from a stall, regardless if it is assoicated with a Register (Nullable). Return true to allow the purchase;
     */
    public delegate bool CanPurchaseItemDelegate(IPlayer player, IStallInventoryProvider stall, IOwnable parent, int productSlot, int numPurchases);

    /*
     * Called when a product is purchased and payment is sent to a register. Changes to Payment stack will be persisted. Can be used for things like subtracting Taxes or the like.
     */
    public delegate void OnRecordPurchaseDelegate(IPlayer player, IStallInventoryProvider stall, ItemStack productClone, ItemStack payment);

    public delegate bool TryPlaceBlockDelegate(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, bool multicastResult);
    public delegate bool OnBlockBrokenDelegate(AssetLocation code, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier);
    public delegate void OnBlockPlacedDelegate(AssetLocation code, IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack);
    public delegate EnumWorldAccessResponse OnTestAccessDelegate(IPlayer player, BlockSelection blockSelection, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response);

    /*
    public delegate void OnUpdateShopDelegate(ShopRegistration shop);
    public delegate void OnTradeSelectedDelegate(VinconNetworkItemSlot product);
    public delegate void OnUpdateShopProductDelegate(BEVinconBase stall, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency);
    public delegate void OnTradeNetworkShopRecieved(TradeNetworkShop shop);
    */

    public delegate void PreProcessTrade(TradeRequest request,ref EnumHandling handling);
    public delegate void PostProcessTrade(TradeResult request, ref EnumHandling handling);
    public delegate void PreFinalizeTrade(TradeResult request, ref EnumHandling handling);
    public delegate void PostFinalizeTrade(TradeResult request, ref EnumHandling handling);
    
}