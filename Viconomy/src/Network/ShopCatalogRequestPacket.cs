using ProtoBuf;

namespace Viconomy.Network
{
    [ProtoContract]
    public class ShopCatalogRequestPacket
    {
        [ProtoMember(1)]
        public int ShopId { get; set; }
    }
}
