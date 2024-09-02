using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Registry
{
    [ProtoContract]
    public class ShopCatalog
    {
        [ProtoMember(1)] public int ID { get; set; } = -1;
        [ProtoMember(2)] public string Name { get; set; }
        [ProtoMember(3)] public string OwnerName { get; set; }
        [ProtoMember(4)] public string Description { get; set; }
        [ProtoMember(5)] public string ImageURL { get; set; }
        [ProtoMember(6)] public int X { get; internal set; }
        [ProtoMember(7)] public int Y { get; internal set; }
        [ProtoMember(8)] public int Z { get; internal set; }
        [ProtoMember(9)] public bool IsWaypointBroadcasted { get; set; }
        [ProtoMember(10)] public ShopProductList Products { get; internal set; }
    }
}
