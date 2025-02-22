namespace Viconomy.Network.Api
{
    public class ShopProduct
    {
        public ShopProductId id;
        public long shopId;
        public string productName;
        public string productCode;
        public int productQuantity;
        public string productAttributes;

        public string currencyName;
        public string currencyCode;
        public int currencyQuantity;
        public string currencyAttributes;

        public int totalStock;
    }
}
