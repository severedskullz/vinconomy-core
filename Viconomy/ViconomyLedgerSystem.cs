using Viconomy.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using System.Collections.Generic;

namespace Viconomy
{


    public class ViconomyLedgerSystem : ModSystem
    {
        //Client Variables
        private IClientNetworkChannel _clientChannel;

        //Server Variables
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
            _serverChannel = api.Network.GetChannel("Vinconomy");
            _serverChannel.RegisterMessageType(typeof(LedgerEntryRequestPacket))
                .RegisterMessageType(typeof(LedgerEntryResponsePacket));
            _serverChannel.SetMessageHandler(new NetworkClientMessageHandler<LedgerEntryRequestPacket>(this.OnRecieveLedgerRequestPacket));

        }


        public override void StartClientSide(ICoreClientAPI api)
        {
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
            Dictionary<string, List<LedgerEntry>> sales = core.DB.LoadSales(packet.ShopId, packet.Month, packet.Year);
            _serverChannel.SendPacket(new LedgerEntryResponsePacket { entries = sales }, new IServerPlayer[] { player });
        }

        private void OnRecieveLedgerResponsePacket(LedgerEntryResponsePacket packet)
        {
            LoadLedgerData(packet.entries);
        }

        public void LoadLedgerData(Dictionary<string, List<LedgerEntry>> data)
        {
            OnLedgerData?.Invoke(data);
        }
        public event OnLedgerDataDelegate OnLedgerData;
        public delegate void OnLedgerDataDelegate(Dictionary<string, List<LedgerEntry>> data);

    }
}
