using ProtoBuf;

namespace Vinconomy.Network
{
    [ProtoContract]
    public class LedgerEntryRequestPacket
    {

        public LedgerEntryRequestPacket() { }
        public LedgerEntryRequestPacket(int shopId, int month, int year)
        {
            ShopId = shopId;
            Month = month;
            Year = year;
        }

        [ProtoMember(1)]
        public int ShopId { get; set; }
        [ProtoMember(2)]
        public int Month { get; set; }
        [ProtoMember(3)]    
        public int Year { get; set; }

    }
}