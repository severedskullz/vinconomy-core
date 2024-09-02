using ProtoBuf;
using System.Collections.Generic;
using Viconomy.Registry;

namespace Viconomy.Network
{
    [ProtoContract]
    public class ShopCatalogResponsePacket
    {
        [ProtoMember(1)] public ShopCatalog ShopCatalog { get; set; }
        [ProtoMember(2)] public List<ShopCatalog> ShopList { get; set; }
    }
}
