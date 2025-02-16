using System;
using System.Collections.Generic;
using Viconomy.Config;
using Viconomy.Network;
using Viconomy.Network.Api;
using Viconomy.Util;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Viconomy
{

    public class VinconomyTradingIntegrationSystem : ModSystem
    {
        private ICoreServerAPI _coreServerAPI;
        private VinconomyCoreSystem _coreSystem;

        private TradeNetworkUpdate TradeNetworkUpdate;

        public override double ExecuteOrder() => 1.1;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }


        public override void StartServerSide(ICoreServerAPI api)
        {
            _coreServerAPI = api;
            _coreSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();

            _coreSystem.OnUpdateShopProduct += UpdateShopProductForNetwork;
            _coreServerAPI.Event.Timer(SendQueuedNetworkShopUpdates, 10 * 60);

            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands.GetOrCreate("viconomy")
                .WithAlias("vicon")
                .RequiresPrivilege(Privilege.chat)
                .BeginSubCommand("network")
                    .RequiresPrivilege(Privilege.controlserver)
                    .WithDescription("Configures the Trade Network")
                        .BeginSubCommand("configure")
                            .WithDescription("Sets the Trade Network URL")
                            .WithArgs(parsers.Word("Root URL (Ex: http://localhost:8080)"))
                            .HandleWith(ConfigureNetwork)
                        .EndSubCommand()
                        .BeginSubCommand("join")
                            .WithDescription("Joins a Trade Network")
                            .WithArgs(parsers.Word("Join Key"))
                            .HandleWith(JoinNetwork)
                        .EndSubCommand()
                        .BeginSubCommand("register")
                            .WithDescription("Registers the current save-game with the Trade Network")
                            .HandleWith(RegisterNetwork)
                        .EndSubCommand()
                        .BeginSubCommand("leave")
                            .WithDescription("Leaves the Trade Network")
                            .HandleWith(LeaveNetwork)
                        .EndSubCommand()
                .EndSubCommand();
        }

        private void SendQueuedNetworkShopUpdates()
        {
            if (TradeNetworkUpdate == null)
                return;

            ViconConfig config = _coreSystem.Config;
            string apiKey = config.GetAPIKey(null);
            if (string.IsNullOrEmpty(apiKey)) return;
            if (!config.tradingNetworkEnabled) return;
            if (string.IsNullOrEmpty(config.tradingNetworkUrl)) return;

            //TODO: Change for one POST instead of multiple?
            foreach (int shopId in TradeNetworkUpdate.Keys)
            {
                TradeNetworkShopUpdate shop = TradeNetworkUpdate[shopId];
                string payload = shop.ToJsonString();
                VinUtils.PostAsync($"{config.tradingNetworkUrl}/products/{shopId}", payload, OnNetworkShopUpdatesResponse, apiKey);
            }

           
        }

        private void OnNetworkShopUpdatesResponse(CompletedArgs args)
        {
            if (args.StatusCode == 200)
            {
                TradeNetworkUpdate = null;
            }
        }

        private TextCommandResult ConfigureNetwork(TextCommandCallingArgs args)
        {
            _coreSystem.Config.tradingNetworkUrl = (string)args[0];
            _coreServerAPI.StoreModConfig(_coreSystem.Config, VinconomyCoreSystem.CONFIG_NAME);
            return TextCommandResult.Success("Set URL to: " + _coreSystem.Config.tradingNetworkUrl);
        }

        private TextCommandResult RegisterNetwork(TextCommandCallingArgs args)
        {
            if (string.IsNullOrEmpty(_coreSystem.Config.tradingNetworkUrl))
            {
                return TextCommandResult.Error("Trade Network URL not configured");
            }

            TradeNetworkNodeRegistration data = new TradeNetworkNodeRegistration();
            data.name = _coreServerAPI.WorldManager.SaveGame.WorldName;
            data.guid = _coreServerAPI.World.SavegameIdentifier;
            string jsonData = VinUtils.SerializeToJson(data);


            VinUtils.PutAsync($"{_coreSystem.Config.tradingNetworkUrl}/api/network/node", jsonData, OnRegisterNetworkResponse, _coreSystem.Config.GetAPIKey(data.guid));
            return TextCommandResult.Success("Requested - Check Console for updates");
        }

        private TextCommandResult JoinNetwork(TextCommandCallingArgs args)
        {
            if (string.IsNullOrEmpty(_coreSystem.Config.tradingNetworkUrl))
            {
                return TextCommandResult.Error("Trade Network URL not configured");
            }

            if (string.IsNullOrEmpty(_coreSystem.Config.GetAPIKey(_coreServerAPI.World.SavegameIdentifier)))
            {
                return TextCommandResult.Error("Trade Network Node not registered. Run \"/vicon network register\" first!");
            }

            TradeNetworkNodeRegistration data = new TradeNetworkNodeRegistration();
            data.name = _coreServerAPI.WorldManager.CurrentWorldName;
            data.guid = this._coreServerAPI.World.SavegameIdentifier;
            string jsonData = VinUtils.SerializeToJson(data);


            VinUtils.PutAsync($"{_coreSystem.Config.tradingNetworkUrl}/api/network/join", jsonData, OnRegisterNetworkResponse, _coreSystem.Config.GetAPIKey(data.guid));
            return TextCommandResult.Success("Requested - Check Console for updates");
        }

        private TextCommandResult LeaveNetwork(TextCommandCallingArgs args)
        {
            return TextCommandResult.Success("Success");
        }

        private void UpdateShopProductForNetwork(int shopId, BlockPos pos, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (_coreSystem.Config.tradingNetworkEnabled && _coreSystem.Config.networkAPIKeys != null)
                //VinUtils.PostAsync()
                if (TradeNetworkUpdate == null)
                {
                    TradeNetworkUpdate = new TradeNetworkUpdate();
                }
            TradeNetworkUpdate.AddShopUpdate(shopId, pos, stallSlot, product, numItemsPerPurchase, currency);
        }

        private void OnRegisterNetworkResponse(CompletedArgs args)
        {
            if (args.StatusCode == 200)
            {
                TradeNetworkNode node = VinUtils.DeserializeFromJson<TradeNetworkNode>(args.Response);
                if (_coreSystem.Config.networkAPIKeys == null)
                {
                    _coreSystem.Config.networkAPIKeys = new Dictionary<string, string>();
                }
                _coreSystem.Config.networkAPIKeys.Add(_coreServerAPI.World.SavegameIdentifier, node.apiKey);
                _coreServerAPI.StoreModConfig(_coreSystem.Config, VinconomyCoreSystem.CONFIG_NAME);

                this.Mod.Logger.Notification("Completed registration of Trade Network Node. Api Key written to config!");
            }
            else
            {
                this.Mod.Logger.Error($"Failed registration of Trade Network Node. Error: {args.ErrorMessage}");
            }

        }


    }
}
