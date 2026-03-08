using System.Collections.Generic;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vinconomy.Network.JavaApi
{
    public class ShopUpdate : Dictionary<BlockPos, ShopStall>
    {
        int ShopId;
        public string Name;
        public string Owner;
        public string Description;

        public bool RemoveAll;

        public ShopUpdate(int shopId)
        {
            ShopId = shopId;
        }

        public void AddStallUpdate(BlockPos pos, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (!ContainsKey(pos))
            {
                this.Add(pos, new ShopStall(pos));
            }

            this[pos].AddStallUpdate(stallSlot, product, numItemsPerPurchase, currency);
        }

        public static ShopUpdate FromJson(JsonObject obj)
        {
            obj.TryGetPropertyValue("id", out JsonNode id);
            if (id == null)
            {
                throw new System.Exception("Shop must have an ID");
            }
            ShopUpdate products = new ShopUpdate(id.GetValue<int>());
            return products;
        }


        public JsonObject ToJson()
        {
            JsonObject json = new JsonObject
            {
                { "id", ShopId },
                { "name", Name },
                { "owner", Owner },
                { "description", Description },
                { "removeAll", RemoveAll }
            };

            JsonArray stalls = new JsonArray();
            foreach (ShopStall stallUpdate in this.Values)
            {
                stalls.Add(stallUpdate.ToJsonString());
            }
            json.Add("stalls", stalls);

            return json;
        }

        public string ToJsonString()
        {
            return ToJson().ToString();
        }
    }
}