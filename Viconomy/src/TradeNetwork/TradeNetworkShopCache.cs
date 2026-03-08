using System.Collections.Generic;
using Viconomy.Network.JavaApi.TradeNetwork;

namespace Vinconomy.TradeNetwork
{
    public class TradeNetworkShopCache : Dictionary<long, TradeNetworkShop>
    {
        public TradeNetworkShop GetShop(long shopId)
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
            this.Add(shop.Id, shop);
        }
    }
}