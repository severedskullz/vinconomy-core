using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using Viconomy.Database;
using Viconomy.Network;
using Vintagestory.API.MathTools;
using Vintagestory.Server;

namespace Viconomy.Registry
{
    public class ShopRegistry
    {
        //TODO: Better checks / seperation for DB commits
        private ViconDatabase db;
        private Dictionary<string, Dictionary<int, ShopRegistration>> shopsByOwner = new Dictionary<string, Dictionary<int, ShopRegistration>>();
        private Dictionary<int, ShopRegistration> shops = new Dictionary<int, ShopRegistration>();

        public ShopRegistry(ViconDatabase db) { 
            this.db = db;
        }

        public string[] GetAllShopOwners()
        {
            return shopsByOwner.Keys.ToArray();
        }

        public List<ShopRegistration> GetAllShops()
        {
            return shops.Values.ToList();
        }

        public void ClearShop(int registerID)
        {
            if (shops.ContainsKey(registerID))
            {
                ShopRegistration shop = GetShop(registerID);
                shopsByOwner[shop.Owner].Remove(registerID);
                shops.Remove(registerID);
                if (db != null) db.DeleteShop(registerID);
            }
        }
        public void ClearShopPos(int id)
        {
            ShopRegistration shop = GetShop(id);
            if (shop != null)
            {
                shop.Position = null;
                shop.IsWaypointBroadcasted = false;
                shop.WaypointColor = 0;
                shop.WaypointIcon = null;
                if (db != null) db.UpdateShop(shop);
            }
        }

        public ShopRegistration GetShop(int registerID)
        {
            if (registerID > 0 && shops.ContainsKey(registerID))
            {
                return shops[registerID];
            }
            return null;
        }

        public ShopRegistration[] GetShopsForOwner(string owner)
        {
            if (shopsByOwner.ContainsKey(owner) && shopsByOwner[owner] != null)
            {
                return shopsByOwner[owner].Values.ToArray<ShopRegistration>();
            }
            return new ShopRegistration[0];
        }



        public ShopRegistration AddShop(string owner, string ownerName, string name, BlockPos pos, int ID = -1)
        {
            Console.WriteLine("Adding new Shop for " + owner );
            ShopRegistration register = new ShopRegistration() {Owner = owner, OwnerName = ownerName, Name = name, Position = pos, ID = ID };

            db.AddShop(register);
            if (register.ID > 0)
            {
                this.AddShop(register);
            }
            else
            {
                throw new ApplicationException("Failed to persist shop to DB");
            }
            

            return register;

        }

        public void AddShop(ShopRegistration register)
        {
            if (!shopsByOwner.ContainsKey(register.Owner) || shopsByOwner[register.Owner] == null)
            {
                shopsByOwner[register.Owner] = new Dictionary<int, ShopRegistration>();
            }

            shopsByOwner[register.Owner][register.ID] = register;
            shops[register.ID] = register;
            Console.WriteLine("Added Register with ID " + register.ID + " and owner " + register.Owner);
        }

        public int GetCount()
        {
            int i = 0;
            foreach (Dictionary<int, ShopRegistration> item in shopsByOwner.Values)
            {
                if (item == null) 
                    continue;

                i += item.Count; 
            }

            return i;
        }

        public ShopRegistration UpdateShop(int id, string name, BlockPos pos)
        {
            ShopRegistration register = GetShop(id);
            if (register != null)
            {
                Console.WriteLine("Updating existing Register with ID " + id);
                if (name != null) { 
                    register.Name = name;
                }
                register.Position = pos;

                if (db != null) db.UpdateShop(register);
            }
            else
            {
                throw new ArgumentException("Tried to update a non-existant shop with ID " + id);
            }
            return register;

        }

        public void UpdateShopWaypoint(int ID, bool enabled, string icon = null, int color = 0)
        {
            ShopRegistration shop = GetShop(ID);
            if (shop != null)
            {
                shop.IsWaypointBroadcasted = enabled;
                shop.WaypointIcon = icon;
                shop.WaypointColor = color;
                if (db != null) db.UpdateShop(shop);

            }

        }

        public void UpdateShopFromServer(ShopUpdatePacket packet)
        {
            ShopRegistration reg = GetShop(packet.ID);
            if (reg == null)
            {
                reg = new ShopRegistration();
                reg.ID = packet.ID;
                shops[packet.ID] = reg;
                if (!shopsByOwner.ContainsKey(packet.Owner))
                {
                    shopsByOwner[packet.Owner] = new Dictionary<int, ShopRegistration>();
                }
                shopsByOwner[packet.Owner][packet.ID] = reg;
            }

            reg.Name = packet.Name;
            reg.Owner = packet.Owner;
            reg.X = packet.X;
            reg.Y = packet.Y;
            reg.Z = packet.Z;
            reg.IsWaypointBroadcasted = packet.IsWaypointBroadcasted;
            if (reg.IsWaypointBroadcasted)
            {
                reg.WaypointIcon = packet.WaypointIcon;
                reg.WaypointColor = packet.WaypointColor;
            }
           
            
        }

        public string GetShopName(int ID)
        {
            ShopRegistration shop = GetShop(ID);
            if (shop == null)
            {
                return "Unknown Shop";
            } else
            {
                return shop.Name;
            }
        }
    }
}