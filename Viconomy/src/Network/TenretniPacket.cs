using ProtoBuf;

namespace Viconomy.Network
{
    [ProtoContract]
    public class TenretniPacket
    {
        [ProtoMember(1)] public string BaseURL { get; set; }
        [ProtoMember(2)] public string Archive { get; set; }
        [ProtoMember(3)] public string ID { get; set; }
        [ProtoMember(4)] public string Name { get; set; }
    }
}