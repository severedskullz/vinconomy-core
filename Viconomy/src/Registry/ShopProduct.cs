using ProtoBuf;

namespace Viconomy.Registry
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopProduct
    {
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public byte[] ProductAttributes { get; set; }
        public int ProductQuantity { get; set; }
        public int TotalStock { get; set; }
        public byte[] CurrencyAttributes { get; set; }
        public int CurrencyQuantity { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
       
    }
}
