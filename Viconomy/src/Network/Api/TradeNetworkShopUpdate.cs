using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Network.Api
{
    public class TradeNetworkShopUpdate : Dictionary<BlockPos, TradeNetworkStallUpdate>
    {
        int ShopId;
        bool RemoveAll;

        public TradeNetworkShopUpdate(int shopId)
        {
            ShopId = shopId;
        }

        public void AddStallUpdate(BlockPos pos, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (!ContainsKey(pos))
            {
                this.Add(pos, new TradeNetworkStallUpdate(pos));
            }

            this[pos].AddStallUpdate(stallSlot, product, numItemsPerPurchase, currency);
        }

        public string ToJsonString()
        {
            JsonObject json = new JsonObject
            {
                { "id", ShopId },
                { "removeAll", RemoveAll }
            };

            JsonArray stalls = new JsonArray();
            foreach (TradeNetworkStallUpdate stallUpdate in this.Values) {
                stalls.Add(stallUpdate.ToJsonString());
            }
            json.Add("stalls", stalls);

            return json.ToString();
        }
    }
}