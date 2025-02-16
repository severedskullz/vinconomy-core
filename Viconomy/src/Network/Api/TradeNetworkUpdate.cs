using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Network.Api
{
    public class TradeNetworkUpdate : Dictionary<int, TradeNetworkShopUpdate>
    {

        public void AddShopUpdate(int shopId, BlockPos pos, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (!ContainsKey(shopId))
            {
                this.Add(shopId, new TradeNetworkShopUpdate(shopId));
            }

            this[shopId].AddStallUpdate(pos, stallSlot, product, numItemsPerPurchase, currency);
        }
    }
}