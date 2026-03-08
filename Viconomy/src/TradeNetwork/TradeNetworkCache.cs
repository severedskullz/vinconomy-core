using System;
using System.Collections.Generic;
using Viconomy.Network.JavaApi.TradeNetwork;

namespace Vinconomy.TradeNetwork
{
    public class TradeNetworkCache : Dictionary<string, TradeNetworkShopCache>
    {
        public TradeNetworkShop GetShop(string nodeGuid, long shopId)
        {
            if (ContainsKey(nodeGuid))
            {
                return this[nodeGuid].GetShop(shopId);
            }
            else
                return null;
        }

        public void AddShop(TradeNetworkShop shop)
        {
            if (!ContainsKey(shop.NodeId))
            {
                this.Add(shop.NodeId, new TradeNetworkShopCache());
            }

            this[shop.NodeId].AddShop(shop);
        }

        public void RemoveOldCacheEntries()
        {

        }
    }
}
