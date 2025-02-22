using System;
using System.Collections.Generic;

namespace Viconomy.TradeNetwork
{
    public class TradeNetworkShopCache : Dictionary<int, TradeNetworkShop>
    {
        public TradeNetworkShop GetShop(int shopId)
        {
            if (ContainsKey(shopId))
            {
                return this[shopId];
            }
            else
                return null;
        }

        public void AddShop(TradeNetworkShop shop)
        {
            this.Add(shop.id, shop);
        }
    }
}