using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;

namespace Viconomy.Registry
{
    public class ShopRegistry
    {
        private uint _id;
        private Dictionary<string, Dictionary<string, ViconRegister>> registers;

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

        public void AddRegister(string owner, string registerID, ViconRegister register)
        {
            if (!registers.ContainsKey(owner))
            {
                registers[owner] = new Dictionary<string, ViconRegister>();
            }
            registers[owner][registerID] = register;
            _id++;
        }

        public string GetNextID()
        {
            return _id++.ToString();
        }
    }
}