namespace Viconomy.Network.Api
{
    public class TradeNetworkProduct
    {
        public string NodeId { get; set; }
        public string ShopId { get; set; }
        public int StallSlot { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public byte[] ProductAttributes { get; set; }
        public int ProductQuantity { get; set; }
        public int TotalStock { get; set; }
        public byte[] CurrencyAttributes { get; set; }
        public int CurrencyAmount { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
    }
}
