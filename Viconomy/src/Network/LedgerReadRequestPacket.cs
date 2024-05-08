using ProtoBuf;

namespace Viconomy.Network
{
    [ProtoContract]
    public class LedgerReadRequestPacket
    {
        [ProtoMember(1)]
        public int shopId { get; set; }

        public LedgerReadRequestPacket() { }
    }
}