using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Registry
{
    [ProtoContract]
    public class ShopProductList
    {
        [ProtoMember(1)] public long ExpiresAt { get; internal set; }
        [ProtoMember(2)] public List<ShopProduct> Products { get; internal set; } = new List<ShopProduct>();
    }
}
