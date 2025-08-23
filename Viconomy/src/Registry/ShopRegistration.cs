using ProtoBuf;
using System.Collections.Generic;
using Viconomy.Network;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Registry
{

    //Note to self: Why didnt I just extend ShopUpdatePacket? Well it doesnt fucking work, thats why.

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopRegistration
    {

        public int ID { get; internal set; } = -1;
        public string Name { get;  set; }
        public string Owner { get; internal set; }
        public Dictionary<string,ShopAccess> Permissions { get; internal set; } = new Dictionary<string, ShopAccess>();
        public bool StallPermissions { get; internal set; }
        public string OwnerName { get; internal set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public string WaypointIcon { get; set; }
        public int WaypointColor { get; set; }
        public bool IsWaypointBroadcasted { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public string WebHook {  get; set; }


        public ShopRegistration() { }
        public ShopRegistration(ShopUpdatePacket item)
        {
           this.ID = item.ID;
            this.Name = item.Name;
            this.Owner = item.Owner;
            this.OwnerName = item.OwnerName;
            this.X = item.X;
            this.Y = item.Y;
            this.Z = item.Z;
            this.Permissions = item.Permissions;
            this.StallPermissions = item.StallPermissions;
            this.WaypointIcon = item.WaypointIcon;
            this.WaypointColor = item.WaypointColor;
            this.IsWaypointBroadcasted = item.IsWaypointBroadcasted;
            this.ShortDescription = item.ShortDescription;
            this.Description = item.Description;
            this.WebHook = item.WebHook;

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

        public void RemoveAccess(string playerUid)
        {
            Permissions.Remove(playerUid);
        }

        public void AddAccess(string playerUid, string playerName)
        {
            if (!Permissions.ContainsKey(playerUid))
              Permissions.Add(playerUid, new ShopAccess(playerUid, playerName));
        }

        public bool CanAccess(IPlayer player)
        {
            return player.PlayerUID == Owner ||  Permissions.ContainsKey(player.PlayerUID);
        }
    }
}