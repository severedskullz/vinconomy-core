using Vintagestory.API.Common;

namespace Viconomy.Network.Api
{
    public class TradeNetworkProductUpdate
    {
        public ItemStack Product {  get; set; }
        public int NumItemsPerPurchase { get; set; }
        public ItemStack Currency { get; set; }

        public TradeNetworkProductUpdate(ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            Product = product;
            NumItemsPerPurchase = numItemsPerPurchase;
            Currency = currency;
        }
    }
}