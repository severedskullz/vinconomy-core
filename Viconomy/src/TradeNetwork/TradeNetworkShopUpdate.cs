using System;
using System.Collections.Generic;
using Viconomy.Network.Api;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.TradeNetwork
{
    //TODO: Refactor this based on Block Pos instead. Getting duplicate ID constraint violations when they change shops from one to another and back again.
    // ID is 1, adds an update for 1, then change shop to 2, ID is 2 and adds an update for 2.... Giving us 2 updates for the same block position with 2 different IDs.
    public class TradeNetworkShopUpdate : Dictionary<int, ShopProducts>
    {

        public void AddShopUpdate(int shopId, BlockPos pos, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (!ContainsKey(shopId))
            {
                Add(shopId, new ShopProducts(shopId));
            }

            this[shopId].AddStallUpdate(pos, stallSlot, product, numItemsPerPurchase, currency);
        }

        public int GetUpdateCount()
        {
            return 0;
        }
    }
}