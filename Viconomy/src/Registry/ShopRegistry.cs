using System;
using System.Collections.Generic;
using System.Linq;
using Viconomy.Database;
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

        public ShopRegistration[] GetAllShops()
        {
            return shops.Values.ToArray();
        }


        public void ClearShop(string owner, int registerID)
        {
            if (shopsByOwner.ContainsKey(owner))
            {
                shopsByOwner[owner].Remove(registerID);
                shops.Remove(registerID);
                if (db != null) db.DeleteShop(registerID);
            }
        }
        public void ClearShopPos(string owner, int id)
        {
            if (shopsByOwner.ContainsKey(owner) && shopsByOwner[owner].ContainsKey(id))
            {
                shopsByOwner[owner][id].Position = null;
                if (db != null) db.UpdateShop(shopsByOwner[owner][id]);
            }
        }

        public ShopRegistration GetShop(string owner, int registerID)
        {
            if ( registerID != -1 && owner != null && shopsByOwner.ContainsKey(owner) && shopsByOwner[owner].ContainsKey(registerID))
            {
                return shopsByOwner[owner][registerID];
            }
            return null; 
        }

        public ShopRegistration GetShop(int registerID)
        {
            if (shops.ContainsKey(registerID))
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

        public ShopRegistration UpdateShop(string owner, int id, string name, BlockPos pos)
        {
            ShopRegistration register = null;
            if (shopsByOwner.ContainsKey(owner) && shopsByOwner[owner].ContainsKey(id))
            {
                Console.WriteLine("Updating existing Register with ID " + id);
                register = shopsByOwner[owner][id];
                if (name != null)
                    register.Name = name;
                register.Position = pos;

                if (db != null) db.UpdateShop(register);
            } else
            {
                throw new ArgumentException("Tried to update a non-existant shop");
            }
            return register;
            
        }

        public ShopRegistration AddShop(string owner, string ownerName, string name, BlockPos pos)
        {
            Console.WriteLine("Adding new Shop for " + owner );
            ShopRegistration register = new ShopRegistration() {Owner = owner, OwnerName = ownerName, Name = name, Position = pos };

            db.AddShop(register);
            if (register.ID > -1)
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


    }
}