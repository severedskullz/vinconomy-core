using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Commercially.Vinconomy.Network.Packets
{

        [ProtoContract]
        public class CatalogResponsePacket
        {
        [ProtoMember(1)] public ShopCatalog ShopCatalog { get; set; }
        [ProtoMember(2)] public List<ShopCatalog> ShopList { get; set; }
    }
    
}
