using ProtoBuf;

namespace Vinconomy.Network.Packets
{
    [ProtoContract]
    public class LedgerReadResponsePacket
    {

        [ProtoMember(1)]
        public string Name {  get; set; }

        [ProtoMember(2)]
        public long Id { get; set; }

        [ProtoMember(3)]
        public string Error { get; set; }


        public LedgerReadResponsePacket()
        {
        }
    }
}