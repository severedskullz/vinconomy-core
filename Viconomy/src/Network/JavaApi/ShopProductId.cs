using ProtoBuf;

namespace Vinconomy.Network.JavaApi
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopProductId
    {
        public long NodeId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int StallSlot { get; set; }
        public long ShopId { get; set; }

        public string ToKey()
        {
            return $"{X}-{Y}-{Z}-{StallSlot}";
        }
    }
}