using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace Commercially.Vinconomy.Network.Packets
{

        [ProtoContract]
        public class CatalogRequestPacket
        {
            [ProtoMember(1)]
            public long ShopId { get; set; }

            [ProtoMember(2)]
            public bool IncludeShopList { get; set; }
        }
    
}
