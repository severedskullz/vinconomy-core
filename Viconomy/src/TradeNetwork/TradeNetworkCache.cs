using System;
using System.Collections.Generic;

namespace Viconomy.TradeNetwork
{
    public class TradeNetworkCache : Dictionary<string, TradeNetworkShopCache>
    {
        public TradeNetworkShop GetShop(string nodeId, int shopId)
        {
            if (ContainsKey(nodeId))
            {
                return this[nodeId].GetShop(shopId);
            }
            else
                return null;
        }

        public void AddShop(TradeNetworkShop shop)
        {
            if (!ContainsKey(shop.nodeId))
            {
                this.Add(shop.nodeId, new TradeNetworkShopCache());
            }

            this[shop.nodeId].AddShop(shop);
        }

        public void RemoveOldCacheEntries()
        {

        }
    }
}
