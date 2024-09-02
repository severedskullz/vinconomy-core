using ProtoBuf;
using System.Collections.Generic;

namespace Viconomy.Registry
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopProductList
    {
        public long ExpiresAt { get; internal set; }
        public List<ShopProduct> Products { get; internal set; } = new List<ShopProduct>();
    }
}
