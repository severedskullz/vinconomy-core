using ProtoBuf;
using Viconomy.Registry;

namespace Viconomy.Network
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopUpdatePacket
    {
        public int ID { get; set; } = -1;
        public string Name { get; set; }
        public string Owner { get; set; }
        public string OwnerName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string WaypointIcon { get; set; }
        public int WaypointColor { get; set; }
        public bool IsWaypointBroadcasted { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string WebHook { get; set; }
        public bool IsRemoval { get; set; }

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

            if (IsWaypointBroadcasted)
            {
                this.WaypointIcon = reg.WaypointIcon;
                this.WaypointColor = reg.WaypointColor;
            }
            this.ShortDescription = reg.ShortDescription;

            if (isOwner)
            {
                Description = reg.Description;
                WebHook = reg.WebHook;
            }
        }

        public ShopUpdatePacket(int ID)
        {
            this.ID = ID;
            this.IsRemoval = true;
        }

    }
}
