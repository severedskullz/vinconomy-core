using System;
using System.Collections.Generic;
using System.Linq;
using Viconomy.BlockEntities;
using Viconomy.Database;
using Vintagestory.API.MathTools;

namespace Viconomy.Registry
{
    public class ShopRegistry
    {
        //TODO: Better checks / seperation for DB commits
        private ViconDatabase db;
        internal Dictionary<string, Dictionary<int, ShopRegistration>> registers = new Dictionary<string, Dictionary<int, ShopRegistration>>();

        public ShopRegistry(ViconDatabase db) { 
            this.db = db;
        }


        public void ClearShop(string owner, int registerID)
        {
            if (registers.ContainsKey(owner))
            {
                registers[owner].Remove(registerID);
                if (db != null) db.DeleteShop(registerID);
            }
        }
        public void ClearShopPos(string owner, int id)
        {
            if (registers.ContainsKey(owner) && registers[owner].ContainsKey(id))
            {
                registers[owner][id].Position = null;
                if (db != null) db.UpdateShop(registers[owner][id]);
            }
        }


        public ShopRegistration GetShop(string owner, int registerID)
        {
            if ( registerID != -1 && owner != null && registers.ContainsKey(owner) && registers[owner].ContainsKey(registerID))
            {
                return registers[owner][registerID];
            }
            return null; 
        }
        public ShopRegistration[] GetShopsForOwner(string owner)
        {
            if (registers.ContainsKey(owner) && registers[owner] != null)
            {
                return registers[owner].Values.ToArray<ShopRegistration>();
            }
            return new ShopRegistration[0];
        }

        public ShopRegistration UpdateShop(string owner, int id, string name, BlockPos pos)
        {
            ShopRegistration register = null;
            if (registers.ContainsKey(owner) && registers[owner].ContainsKey(id))
            {
                Console.WriteLine("Updating existing Register with ID " + id);
                register = registers[owner][id];
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
            if (!registers.ContainsKey(register.Owner) || registers[register.Owner] == null)
            {
                registers[register.Owner] = new Dictionary<int, ShopRegistration>();
            }

            registers[register.Owner][register.ID] = register;
            Console.WriteLine("Added Register with ID " + register.ID + " and owner " + register.Owner);
        }

        public int GetCount()
        {
            int i = 0;
            foreach (Dictionary<int, ShopRegistration> item in registers.Values)
            {
                if (item == null) 
                    continue;

                i += item.Count; 
            }

            return i;
        }


    }
}