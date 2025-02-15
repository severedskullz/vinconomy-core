using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Network.Api
{
    public class TradeNetworkShopUpdate : Dictionary<BlockPos, TradeNetworkStallUpdate>
    {
        public void AddStallUpdate(BlockPos pos, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (!ContainsKey(pos))
            {
                this.Add(pos, new TradeNetworkStallUpdate());
            }

            this[pos].AddStallUpdate(stallSlot, product, numItemsPerPurchase, currency);
        }
    }
}