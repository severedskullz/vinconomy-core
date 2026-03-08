using ProtoBuf;
using System.Collections.Generic;
using Vinconomy.Network.JavaApi;

namespace Viconomy.Network.JavaApi.TradeNetwork
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class TradeNetworkShop
    {
        public int Id { get; set; }
        public string NodeId { get; set; }
        public string Name { get; set; }
        public string ServerName { get; set; }
        public string Owner {  get; set; }
        public List<ShopProduct> Products { get; set; } = new List<ShopProduct>();
        public long LastUpdatedTimestamp { get; set; }

        public Dictionary<string, ShopProduct> ProductMap;

        public ShopProduct GetProductById(string key)
        {
            if (ProductMap == null)
            {
                PopulateProductMap();
            }

            ProductMap.TryGetValue(key, out ShopProduct product);
            return product;
        }

        public void PopulateProductMap()
        {
            ProductMap = new Dictionary<string, ShopProduct>();
            foreach (ShopProduct product in Products)
            {
                ProductMap.Add(product.Id.ToKey(), product);
            }
        }

        public  ShopProduct GetProductById(int x, int y, int z, int stallSlot)
        {
            string key = $"{x}-{y}-{z}-{stallSlot}";
            return GetProductById(key);
        }
    }
}