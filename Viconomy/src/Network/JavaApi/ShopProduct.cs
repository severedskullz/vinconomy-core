using ProtoBuf;

namespace Vinconomy.Network.JavaApi
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopProduct
    {
        public ShopProductId Id { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public int ProductQuantity { get; set; }
        public byte[] ProductAttributes { get; set; }

        public string CurrencyName { get; set; }
        public string CurrencyCode { get; set; }
        public int CurrencyQuantity { get; set; }
        public byte[] CurrencyAttributes { get; set; }

        public int TotalStock { get; set; }
    }
}
