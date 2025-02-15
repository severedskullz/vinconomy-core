using ProtoBuf;

namespace Viconomy.Registry
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopProduct
    {
        public string ProductName { get; internal set; }
        public string ProductCode { get; internal set; }
        public byte[] ProductAttributes { get; internal set; }
        public int ProductQuantity { get; internal set; }
        public int TotalStock { get; internal set; }
        public byte[] CurrencyAttributes { get; internal set; }
        public int CurrencyAmount { get; internal set; }
        public string CurrencyCode { get; internal set; }
        public string CurrencyName { get; internal set; }
       
    }
}
