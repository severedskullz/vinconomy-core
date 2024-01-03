using ProtoBuf;
using Viconomy.Registry;

namespace Viconomy.Network
{
    [ProtoContract]
    public class RegistryUpdatePacket
    {
        [ProtoMember(1)]
        public RegistryUpdate[] registry;

        public RegistryUpdatePacket()
        {
        }


        public RegistryUpdatePacket(RegistryUpdate[] registry)
        {
            this.registry = registry;
        }

    }

    [ProtoContract]
    public class RegistryUpdate
    {
        [ProtoMember(1)] public int ID { get; internal set; } = -1;
        [ProtoMember(2)] public string Name { get; set; }
        [ProtoMember(3)] public string Owner { get; internal set; }
        [ProtoMember(4)] public string OwnerName { get; internal set; }
        [ProtoMember(5)] internal int X { get; set; }
        [ProtoMember(6)] internal int Y { get; set; }
        [ProtoMember(7)] internal int Z { get; set; }
        [ProtoMember(8)] public string WaypointIcon { get; set; }
        [ProtoMember(9)] public int WaypointColor { get; set; }
        [ProtoMember(10)] public bool IsWaypointBroadcasted { get; set; }

        public RegistryUpdate() { }
        public RegistryUpdate(ShopRegistration reg)
        {
            this.Owner = reg.Owner;
            this.ID = reg.ID;
            this.Name = reg.Name;
            this.OwnerName = reg.OwnerName;
            this.IsWaypointBroadcasted = reg.IsWaypointBroadcasted;
            if (reg.IsWaypointBroadcasted)
            {
                this.X = reg.X;
                this.Y = reg.Y;
                this.Z = reg.Z;
                this.WaypointIcon = reg.WaypointIcon;
                this.WaypointColor = reg.WaypointColor;
            }

        }

    }
}
