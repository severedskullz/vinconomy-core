using ProtoBuf;

namespace Vinconomy.Network
{
    [ProtoContract]
    public class ShopCatalogRequestPacket
    {
        [ProtoMember(1)]
        public int ShopId { get; set; }
        [ProtoMember(2)]
        public bool IncludeShopList { get; set; }
    }
}
