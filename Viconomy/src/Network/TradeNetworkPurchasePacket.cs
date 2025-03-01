using ProtoBuf;

namespace Viconomy.Network
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class TradeNetworkPurchasePacket
    {
        public int Amount { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int StallSlot { get; set; }
    }
}
