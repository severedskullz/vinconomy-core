using ProtoBuf;
using System.Collections.Generic;
using Viconomy.Registry;
using Vintagestory.API.Server;

namespace Viconomy.Network
{
    [ProtoContract]
    public class RegistryUpdatePacket
    {
        [ProtoMember(1)]
        public List<ShopUpdatePacket> registry;

        public RegistryUpdatePacket()
        {
        }

        public RegistryUpdatePacket(List<ShopUpdatePacket> registry)
        {
            this.registry = registry;
        }

    }

    [ProtoContract]
    public class ShopUpdatePacket
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
        [ProtoMember(11)] public bool IsRemoval { get; set; }

        public ShopUpdatePacket() { }
        public ShopUpdatePacket(ShopRegistration reg, bool isOwner)
        {
            this.Owner = reg.Owner;
            this.ID = reg.ID;
            this.Name = reg.Name;
            this.OwnerName = reg.OwnerName;
            this.IsWaypointBroadcasted = reg.IsWaypointBroadcasted;

            if (reg.IsWaypointBroadcasted || isOwner)
            {
                this.X = reg.X;
                this.Y = reg.Y;
                this.Z = reg.Z;
            }

            if (IsWaypointBroadcasted )
            {
                this.WaypointIcon = reg.WaypointIcon;
                this.WaypointColor = reg.WaypointColor;
            }

        }

        public ShopUpdatePacket(int ID) {
            this.ID = ID;
            this.IsRemoval = true;
        }

    }
}
