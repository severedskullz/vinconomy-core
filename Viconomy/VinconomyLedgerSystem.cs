using Viconomy.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using System.Collections.Generic;
using Viconomy.GUI;
using Vintagestory.API.Config;
using Viconomy.Registry;
using Viconomy.Util;

namespace Viconomy
{


    public class VinconomyLedgerSystem : ModSystem
    {
        private ICoreClientAPI capi;

        //Client Variables
        private IClientNetworkChannel _clientChannel;

        //Server Variables
        private IServerNetworkChannel _serverChannel;

        //Shared Variables
        private VinconomyCoreSystem _coreSystem;
        private GuiViconLedger ledgerGUI;

        public override double ExecuteOrder() => 1.1;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            _coreSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            _serverChannel = api.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL);
            _serverChannel.RegisterMessageType(typeof(LedgerEntryRequestPacket))
                .RegisterMessageType(typeof(LedgerEntryResponsePacket))
                .RegisterMessageType(typeof(LedgerReadRequestPacket))
                .RegisterMessageType(typeof(LedgerReadResponsePacket));
            _serverChannel.SetMessageHandler(new NetworkClientMessageHandler<LedgerEntryRequestPacket>(this.OnRecieveLedgerRequestPacket));
            _serverChannel.SetMessageHandler(new NetworkClientMessageHandler<LedgerReadRequestPacket>(this.OnRecieveRequestToReadLedgerData));

        }


        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            _clientChannel = api.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL);
            _clientChannel.RegisterMessageType(typeof(LedgerEntryRequestPacket))
                .RegisterMessageType(typeof(LedgerEntryResponsePacket))
                .RegisterMessageType(typeof(LedgerReadRequestPacket))
                .RegisterMessageType(typeof(LedgerReadResponsePacket));
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<LedgerEntryResponsePacket>(this.OnRecieveLedgerResponsePacket));
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<LedgerReadResponsePacket>(this.OnRecieveRequestToReadResponsePacket));
        }

        public void RequestToReadLedgerData(int shopId)
        {
            _clientChannel.SendPacket(new LedgerReadRequestPacket() { shopId = shopId }); 
        }
        private void OnRecieveRequestToReadLedgerData(IServerPlayer player, LedgerReadRequestPacket packet)
        {
            if (packet.shopId == -1 && player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                _serverChannel.SendPacket(new LedgerReadResponsePacket() { Id = -1, Name = "Admin Shops" }, new IServerPlayer[] { player });
                return;
            }

            ShopRegistration shop = _coreSystem.GetRegistry().GetShop(packet.shopId);
            if (shop != null)
            {
                _serverChannel.SendPacket(new LedgerReadResponsePacket() { Id = shop.ID, Name = shop.Name }, new IServerPlayer[] { player });
            }
            else
            {
                _serverChannel.SendPacket(new LedgerReadResponsePacket() { Error = "Not Found" }, new IServerPlayer[] { player });
            }
        }

        private void OnRecieveRequestToReadResponsePacket(LedgerReadResponsePacket packet)
        {
            if (packet.Error != null) {
                capi.ShowChatMessage(Lang.Get("vinconomy:ledger-shop-not-found"));
            } else if ( ledgerGUI == null || (ledgerGUI != null && !ledgerGUI.IsOpened()))
            {
                ledgerGUI = new GuiViconLedger(packet.Name, packet.Id, capi);
                ledgerGUI.OnClosed += LedgerGUI_OnClosed;
                ledgerGUI.TryOpen();
            }
            
        }

        private void LedgerGUI_OnClosed()
        {
            ledgerGUI?.Dispose();
            ledgerGUI=null;
        }

        public void RequestLedgerData(int shopId, int month, int year)
        {
            _clientChannel.SendPacket(new LedgerEntryRequestPacket(shopId, month, year));
        }

        private void OnRecieveLedgerRequestPacket(IServerPlayer player, LedgerEntryRequestPacket packet)
        {
            Dictionary<string, List<LedgerEntry>> sales = _coreSystem.DB.LoadSales(packet.ShopId, packet.Month, packet.Year);
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
