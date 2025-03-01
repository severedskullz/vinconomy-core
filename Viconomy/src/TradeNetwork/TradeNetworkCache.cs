using System;
using System.Collections.Generic;
using Viconomy.TradeNetwork.Api;

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
