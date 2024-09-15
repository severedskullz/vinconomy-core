using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Registry
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopCatalog
    {
       public int ID { get; set; } = -1;
        public string Name { get; set; }
        public string OwnerName { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; internal set; }
        public string ImageURL { get; set; }
        public int X { get; internal set; }
        public int Y { get; internal set; }
        public int Z { get; internal set; }
        public int WorldX { get; internal set; }
        public int WorldZ { get; internal set; }
        public bool IsWaypointBroadcasted { get; set; }
        public ShopProductList Products { get; internal set; }
    }
}
