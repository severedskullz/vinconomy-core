using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.BlockTypes;
using Viconomy.Registry;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Server;

namespace Viconomy
{
    public class ViconomyModSystem : ModSystem
    {
        private ICoreServerAPI _coreServerAPI;
        private ICoreClientAPI _coreClientAPI;

        public ShopRegistry registers;
        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
 
            api.RegisterBlockClass("ViconContainer", typeof(BlockVContainer));
            api.RegisterBlockClass("ViconShelf", typeof(BlockVShelf));


            api.RegisterBlockEntityClass("BEViconStall", typeof(BEViconStall));
            api.RegisterBlockEntityClass("BEViconShelf", typeof(BEViconShelf));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _coreServerAPI = api;
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("Viconomy:hello"));

            registers = api.WorldManager.SaveGame.GetData("viconomy:registers", new ShopRegistry());
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            _coreClientAPI = api;
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("Viconomy:hello"));
        }

        public BEVRegister GetShopRegister(string owner, string registerID)
        {
            ViconRegister register = registers.GetRegister(owner, registerID);
            BEVRegister viconRegister = _coreServerAPI.World.BlockAccessor.GetBlockEntity(register.Position) as BEVRegister;
            if (viconRegister != null)
            {
                return viconRegister;
            } else
            {
                registers.ClearRegister(owner, registerID);
            }
            return null;
        }

    }
}
