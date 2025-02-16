using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Network.Api
{
    public class TradeNetworkStallUpdate : Dictionary<int, TradeNetworkProductUpdate>
    {
        BlockPos pos;
        bool RemoveAll;

        public TradeNetworkStallUpdate(BlockPos pos)
        {
            this.pos = pos;
        }

        public void AddStallUpdate(int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (!ContainsKey(stallSlot))
            {
                this.Add(stallSlot, new TradeNetworkProductUpdate(stallSlot, product, numItemsPerPurchase, currency));
            } else
            {
                this[stallSlot].Product = product;
                this[stallSlot].NumItemsPerPurchase = numItemsPerPurchase;
                this[stallSlot].Currency = currency;
            }
        }

        public JsonObject ToJsonString()
        {
            JsonObject json = new JsonObject()
            {
                { "x", pos.X },
                { "y", pos.Y },
                { "z", pos.Z },
                { "removeAll", RemoveAll }
            };

            JsonArray products = new JsonArray();
            foreach (TradeNetworkProductUpdate productUpdate in this.Values)
            {
                products.Add(productUpdate.ToJsonString());
            }
            json.Add("products", products);


            return json;
        }
    }
}