using ProtoBuf;

namespace Vinconomy.Network
{
    [ProtoContract]
    public class LedgerReadResponsePacket
    {

        [ProtoMember(1)]
        public string Name {  get; set; }

        [ProtoMember(2)]
        public int Id { get; set; }

        [ProtoMember(3)]
        public string Error { get; set; }


        public LedgerReadResponsePacket()
        {
        }
    }
}