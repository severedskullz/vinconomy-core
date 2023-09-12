using System;
using System.Collections.Generic;
using System.Linq;
using Viconomy.BlockEntities;
using Vintagestory.API.MathTools;

namespace Viconomy.Registry
{
    public class ShopRegistry
    {
        private uint _id;
        private Dictionary<string, Dictionary<string, ViconRegister>> registers = new Dictionary<string, Dictionary<string, ViconRegister>>();

        public void ClearRegister(string owner, string registerID)
        {
            if (registers.ContainsKey(owner))
            {
                registers[owner].Remove(registerID);
            }
        }

        public ViconRegister GetRegister(string owner, string registerID)
        {
            if (registers.ContainsKey(owner))
            {
                return registers[owner][registerID];
            }
            return null; 
        }
        public ViconRegister[] GetRegistersForOwner(string owner)
        {
            if (registers.ContainsKey(owner))
            {
                return registers[owner].Values.ToArray<ViconRegister>();
            }
            return null;
        }

        public ViconRegister UpdateRegister(string owner, string id, string name, BlockPos pos)
        {
            ViconRegister register = null;
            if (registers.ContainsKey(owner) && registers[owner].ContainsKey(id))
            {
                Console.WriteLine("Updating existing Register with ID " + id);
                register = registers[owner][id];
                register.Name = name;
                register.Position = pos;
            } 
            return register;
            
        }

        public ViconRegister AddRegister(string owner, string ownerName, string name, BlockPos pos)
        {
            string id = owner + "-" + GetNextID();
            Console.WriteLine("Adding new Register with ID " + id );
            ViconRegister register = new ViconRegister() { ID = id, Owner = owner, OwnerName = ownerName, Name = name, Position = pos };
            this.AddRegister(register);
            

            return register;

        }

        public void AddRegister(ViconRegister register)
        {
            if (!registers.ContainsKey(register.Owner))
            {
                registers[register.Owner] = new Dictionary<string, ViconRegister>();
            }
            registers[register.Owner][register.ID] = register;
            Console.WriteLine("Added Register with ID " + register.ID + " and owner " + register.OwnerName);
        }

        public string GetNextID()
        {
            return _id++.ToString();
        }

        public int GetCount()
        {
            int i = 0;
            foreach (Dictionary<string, ViconRegister> item in registers.Values)
            {
                i += item.Count; 
            }

            return i;
        }
    }
}