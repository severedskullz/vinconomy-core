using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Viconomy.Network.Api
{
    public class TradeNetworkStallUpdate : Dictionary<int, TradeNetworkProductUpdate>
    {
        public void AddStallUpdate(int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (!ContainsKey(stallSlot))
            {
                this.Add(stallSlot, new TradeNetworkProductUpdate(product, numItemsPerPurchase, currency));
            } else
            {
                this[stallSlot].Product = product;
                this[stallSlot].NumItemsPerPurchase = numItemsPerPurchase;
                this[stallSlot].Currency = currency;
            }
        }
    }
}