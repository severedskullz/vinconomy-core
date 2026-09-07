using ProtoBuf;

namespace Vinconomy.Network.Packets
{
    [ProtoContract]
    public class LedgerEntryRequestPacket
    {

        public LedgerEntryRequestPacket() { }
        public LedgerEntryRequestPacket(long shopId, int month, int year)
        {
            ShopId = shopId;
            Month = month;
            Year = year;
        }

        [ProtoMember(1)]
        public long ShopId { get; set; }
        [ProtoMember(2)]
        public int Month { get; set; }
        [ProtoMember(3)]    
        public int Year { get; set; }

    }
}