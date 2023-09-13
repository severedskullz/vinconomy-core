using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.BlockTypes;
using Viconomy.Network;
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

        private IClientNetworkChannel _clientChannel;
        private IServerNetworkChannel _serverChannel;

        public ShopRegistry registers = new ShopRegistry();

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
 
            api.RegisterBlockClass("ViconContainer", typeof(BlockVContainer));
            api.RegisterBlockClass("ViconRegister", typeof(BlockVRegister));
            api.RegisterBlockClass("ViconShelf", typeof(BlockVShelf));


            api.RegisterBlockEntityClass("BEViconStall", typeof(BEViconStall));
            api.RegisterBlockEntityClass("BEVRegister", typeof(BEVRegister));
            api.RegisterBlockEntityClass("BEViconShelf", typeof(BEViconShelf));

            api.Network.RegisterChannel("Viconomy")
                .RegisterMessageType(typeof(RegistryUpdatePacket));

        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _coreServerAPI = api;
            _serverChannel = api.Network.GetChannel("Viconomy");
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("Viconomy:hello"));

            

            api.Event.SaveGameLoaded += OnSaveGameLoading;
            api.Event.GameWorldSave += OnSaveGameSaving;
            api.Event.PlayerJoin += OnPlayerJoined;

            
        }



        public override void StartClientSide(ICoreClientAPI api)
        {
            _coreClientAPI = api;
            _clientChannel = api.Network.GetChannel("Viconomy");
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("Viconomy:hello"));
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<RegistryUpdatePacket>(this.OnRecieveRegistryUpdate));
            this.registers = new ShopRegistry();
        }

        private void OnRecieveRegistryUpdate(RegistryUpdatePacket packet)
        {
            _coreClientAPI.Logger.Notification("Recieved Registry Update Packet");
            this.registers = new ShopRegistry();
            if (packet.registry != null) {
                foreach (RegistryUpdate item in packet.registry)
                {
                    //TODO: Figure out how to get local player.
                    this.registers.AddRegister(new ViconRegister() { Name = item.Name, ID = item.ID , Owner = item.Owner});
                }
            }
            
            
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

        public ShopRegistry GetRegistry()
        {
            return registers;
        }

        private void OnSaveGameLoading()
        {
            this._coreServerAPI.Logger.Debug("=============== Loading Viconomy ===============");
            registers = _coreServerAPI.WorldManager.SaveGame.GetData("viconomy:registers", new ShopRegistry());
            this._coreServerAPI.Logger.Debug("= Loaded " + registers.GetCount() + " registers");

            foreach (string ownerId in registers.registers.Keys)
            {
                var shops = registers.registers[ownerId];
                this._coreServerAPI.Logger.Debug("== Loading " + shops.Values.Count + " registers for Owner: " + ownerId);
                foreach (string shopId in shops.Keys)
                {
                    var shop = registers.registers[ownerId][shopId];
                    this._coreServerAPI.Logger.Debug("=== Loading shop " + shop.Name);
                }
            }
            this._coreServerAPI.Logger.Debug("=============== Loaded Viconomy ================");
        }

        private void OnSaveGameSaving()
        {
            _coreServerAPI.WorldManager.SaveGame.StoreData<ShopRegistry>("viconomy:registers", registers);
        }

        private void OnPlayerJoined(IServerPlayer player)
        {
            ViconRegister[] shops = this.registers.GetRegistersForOwner(player.PlayerUID);
            RegistryUpdate[] updates = new RegistryUpdate[0];
            if (shops != null)
            {
                updates = new RegistryUpdate[shops.Length];

                for (int i = 0; i < shops.Length; i++)
                {
                    updates[i] = new RegistryUpdate(shops[i].Owner, shops[i].ID, shops[i].Name);
                }
            }
            

            _serverChannel.SendPacket<RegistryUpdatePacket>(new RegistryUpdatePacket(updates), new IServerPlayer[]{ player });
        }

    }
}
