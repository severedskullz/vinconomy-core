using System;
using Viconomy.BlockEntities;
using Viconomy.BlockTypes;
using Viconomy.Network;
using Viconomy.Registry;
using Viconomy.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Viconomy.Delegates;
using System.Collections.Generic;
using Viconomy.Renderer;
using Viconomy.ItemTypes;
using Cairo;
using Viconomy.Database;
using Viconomy.Trading;

namespace Viconomy
{


    public class ViconomyLedgerSystem : ModSystem
    {
        //Client Variables
        private ICoreClientAPI _coreClientAPI;
        private IClientNetworkChannel _clientChannel;

        //Server Variables
        private ICoreServerAPI _coreServerAPI;
        private IServerNetworkChannel _serverChannel;

        //Shared Variables
        private ViconomyCoreSystem core;



        public override double ExecuteOrder() => 1.1;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            core = api.ModLoader.GetModSystem<ViconomyCoreSystem>();
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            _coreServerAPI = api;
            _serverChannel = api.Network.GetChannel("Vinconomy");
            _serverChannel.RegisterMessageType(typeof(LedgerEntryRequestPacket))
                .RegisterMessageType(typeof(LedgerEntryResponsePacket));
            _serverChannel.SetMessageHandler(new NetworkClientMessageHandler<LedgerEntryRequestPacket>(this.OnRecieveLedgerRequestPacket));

        }


        public override void StartClientSide(ICoreClientAPI api)
        {
            _coreClientAPI = api;
            _clientChannel = api.Network.GetChannel("Vinconomy");
            _clientChannel.RegisterMessageType(typeof(LedgerEntryRequestPacket))
                .RegisterMessageType(typeof(LedgerEntryResponsePacket));
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<LedgerEntryResponsePacket>(this.OnRecieveLedgerResponsePacket));
        }

        public void RequestLedgerData(int shopId, int month, int year)
        {
            _clientChannel.SendPacket(new LedgerEntryRequestPacket(shopId, month, year));
        }

        private void OnRecieveLedgerRequestPacket(IServerPlayer player, LedgerEntryRequestPacket packet)
        {
            Dictionary<string, LedgerEntry> sales = core.DB.LoadSales(packet.ShopId, packet.Month, packet.Year);
            _serverChannel.SendPacket(new LedgerEntryResponsePacket { entries = sales }, new IServerPlayer[] { player });
        }

        private void OnRecieveLedgerResponsePacket(LedgerEntryResponsePacket packet)
        {
            LoadLedgerData(packet.entries);
        }

        public void LoadLedgerData(Dictionary<string, LedgerEntry> data)
        {
            OnLedgerData?.Invoke(data);
        }
        public event OnLedgerDataDelegate OnLedgerData;
        public delegate void OnLedgerDataDelegate(Dictionary<string, LedgerEntry> data);

    }
}
