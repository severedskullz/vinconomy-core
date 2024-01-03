using ProtoBuf;
using Viconomy.Network;
using Vintagestory.API.MathTools;

namespace Viconomy.Registry
{
    [ProtoContract]
    public class ShopRegistration
    {



        [ProtoMember(1)] public int ID { get; internal set; } = -1;
        [ProtoMember(2)] public string Name { get;  set; }
        [ProtoMember(3)] public string Owner { get; internal set; }
        [ProtoMember(4)] public string OwnerName { get; internal set; }
        [ProtoMember(5)] internal int X { get; set; }
        [ProtoMember(6)] internal int Y { get; set; }
        [ProtoMember(7)] internal int Z { get; set; }
        [ProtoMember(8)] public string WaypointIcon { get; set; }
        [ProtoMember(9)] public int WaypointColor { get; set; }
        [ProtoMember(10)] public bool IsWaypointBroadcasted { get; set; }

        public ShopRegistration() { }
        public ShopRegistration(RegistryUpdate item)
        {
           this.ID = item.ID;
            this.Name = item.Name;
            this.Owner = item.Owner;
            this.OwnerName = item.OwnerName;
            this.X = item.X;
            this.Y = item.Y;
            this.Z = item.Z;
            this.WaypointIcon = item.WaypointIcon;
            this.WaypointColor = item.WaypointColor;
            this.IsWaypointBroadcasted = item.IsWaypointBroadcasted;
        }


        public BlockPos Position { 
            get { 
                if (Y == -1)
                {
                    return null;
                }
                return new BlockPos(X, Y, Z, 0);
            }
            
            set {
                if (value != null)
                {
                    this.X = value.X;
                    this.Y = value.Y;
                    this.Z = value.Z;
                } else
                {
                    this.X = 0;
                    this.Y = -1;
                    this.Z = 0;
                }

            }
        }


    }
}