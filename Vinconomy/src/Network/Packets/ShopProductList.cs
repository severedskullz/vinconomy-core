using ProtoBuf;
using System.Collections.Generic;

namespace Commercially.Vinconomy.Network.Packets
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopProductList
    {
        public long ExpiresAt { get; set; }
        public List<ShopProduct> Products { get; set; } = new List<ShopProduct>();
    }
}
