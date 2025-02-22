using System.Collections.Generic;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Network.Api
{
    public class ShopStall : Dictionary<int, Product>
    {
        BlockPos pos;
        bool RemoveAll;

        public ShopStall(BlockPos pos)
        {
            this.pos = pos;
        }

        public void AddStallUpdate(int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (!ContainsKey(stallSlot))
            {
                Add(stallSlot, new Product(stallSlot, product, numItemsPerPurchase, currency));
            }
            else
            {
                this[stallSlot].Item = product;
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
            foreach (Product productUpdate in Values)
            {
                products.Add(productUpdate.ToJsonString());
            }
            json.Add("products", products);


            return json;
        }
    }
}