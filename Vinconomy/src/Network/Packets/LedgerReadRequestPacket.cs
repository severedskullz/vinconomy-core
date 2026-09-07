using ProtoBuf;

namespace Vinconomy.Network.Packets
{
    [ProtoContract]
    public class LedgerReadRequestPacket
    {
        [ProtoMember(1)]
        public long shopId { get; set; }

        public LedgerReadRequestPacket() { }
    }
}