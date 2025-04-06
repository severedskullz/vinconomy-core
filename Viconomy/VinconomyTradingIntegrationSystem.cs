using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Config;
using Viconomy.Delegates;
using Viconomy.Network;
using Viconomy.Network.Api;
using Viconomy.Registry;
using Viconomy.TradeNetwork;
using Viconomy.TradeNetwork.Api;
using Viconomy.Trading;
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

        //TODO: Move this to DB so we dont lose info as soon as they shut down the server / crash
        private TradeNetworkShopUpdate TradeNetworkUpdates;
        private TradeNetworkPurchaseUpdate TradeNetworkPurchaseUpdates = new TradeNetworkPurchaseUpdate();

        private TradeNetworkCache TradeNetworkCache = new TradeNetworkCache();

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
            //_coreServerAPI.Event.Timer(SyncTradeNetwork, 1 * 60);

            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands.GetOrCreate("vinconomy")
                .WithAlias("vincon")
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
                            .WithArgs(parsers.Word("Join Key", new string[] { "GLOBAL" }), parsers.OptionalWord("Username"), parsers.OptionalWord("Password"))
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
                        .BeginSubCommand("sync")
                            .WithDescription("Syncs the Trade Network")
                            .HandleWith(SyncNetwork)
                        .EndSubCommand()
                .EndSubCommand();


            
        }

        private TextCommandResult SyncNetwork(TextCommandCallingArgs args)
        {
            SyncTradeNetwork();
            return TextCommandResult.Success("Starting Sync");
        }

        private void SyncTradeNetwork()
        {
            ViconConfig config = _coreSystem.Config;
            if (!config.tradingNetworkEnabled)
            {
                return;
            }

            string apiKey = config.GetAPIKey(_coreServerAPI.World.SavegameIdentifier);
            if (apiKey == null)
            {
                return;
            }

            this.Mod.Logger.Event("Attempting to sync Trade Network Updates");


            // Trade Network - Shop Updates
            if (TradeNetworkUpdates != null)
            {
                if (string.IsNullOrEmpty(apiKey)) return;
                if (!config.tradingNetworkEnabled) return;
                if (string.IsNullOrEmpty(config.tradingNetworkUrl)) return;

                //TODO: Change for one POST instead of multiple?
                foreach (int shopId in TradeNetworkUpdates.Keys)
                {
                    ShopProducts shop = TradeNetworkUpdates[shopId];
                    string payload = shop.ToJsonString();
                    VinUtils.PatchAsync($"{config.tradingNetworkUrl}/api/shop/products/{shopId}", payload, OnNetworkShopUpdatesResponse, apiKey);
                }
            }

            //Trade Network - Outgoing Purchase Updates
            if (TradeNetworkPurchaseUpdates.Count > 0)
            {
                string payload = TradeNetworkPurchaseUpdates.ToJsonString();
                VinUtils.PostAsync($"{config.tradingNetworkUrl}/api/market/purchase", payload, OnNetworkProductUpdatesResponse, apiKey);
            }

            //Trade Network - Incoming Purchase Updates
            VinUtils.GetAsync($"{config.tradingNetworkUrl}/api/market/purchases/pending", OnNetworkIncomingPurchaseUpdatesResponse, apiKey);


        }

        private bool isValidResponse(CompletedArgs args)
        {
            if (args.StatusCode == 200)
                return true;
            else if (args.StatusCode == 401)
            {
                this.Mod.Logger.Error("API Key was not valid for the given Node. Re-register the network node and try again. Removing API Key from configuration");
                _coreSystem.Config.ClearApiKeyForGUID(_coreServerAPI.World.SavegameIdentifier);
                _coreSystem.PersistConfig();
            } else {
                this.Mod.Logger.Event($"An error occurred communicating with Trade Network. Message was {args.Response}");
            }

            return false;
        }

        private void OnNetworkIncomingPurchaseUpdatesResponse(CompletedArgs args)
        {
            if (isValidResponse(args))
            {
                HashSet<string> playersToNotify = new HashSet<string>();

                this.Mod.Logger.Event("got response");
                List<ShopTradeUpdate> tradeUpdates = VinUtils.DeserializeFromJson<List<ShopTradeUpdate>>(args.Response);
                List<ShopTradeUpdate> updateResults = new List<ShopTradeUpdate>();

                foreach (ShopTradeUpdate update in tradeUpdates)
                {
                    this.Mod.Logger.Debug($"Processing update of shop {update.ShopId} and stall {update.X}-{update.Y}-{update.Z}-{update.StallSlot} with {update.Amount}x purchases from {update.RequestingNode}");
                    ShopTradeUpdate curUpdate = update;
                    ShopRegistration reg = _coreSystem.GetRegistry().GetShop(update.ShopId);

                    if (reg != null)
                    {
                        //Load the chunk the Register is in
                        VinUtils.LoadChunk(_coreServerAPI, reg.Position.X, reg.Position.Y, reg.Position.Z, () =>
                        {
                            BEVinconRegister register = _coreSystem.GetShopRegister(reg.Owner, reg.ID);

                            if (register != null)
                            {
                                //Load the chunk that the Shop Stall is in
                                VinUtils.LoadChunk(_coreServerAPI, curUpdate.X, curUpdate.Y, curUpdate.Z, () =>
                                {

                                    BlockPos pos = new BlockPos(curUpdate.X, curUpdate.Y, curUpdate.Z);
                                    BEVinconContainer stall = _coreServerAPI.World.BlockAccessor.GetBlockEntity<BEVinconContainer>(pos);
                                    if (stall != null)
                                    {
                                        //TODO: Check that the product/currency items match the trade
                                        
                                        ItemStack currencyStack = stall.GetCurrencyForStall(curUpdate.StallSlot).Itemstack;
                                        if (currencyStack == null) {
                                            this.Mod.Logger.Error("Tried to process network purchase on an item that didn't have a price set");
                                            update.Status = VinConstants.TRADE_STATUS_LACKS_ITEMS;
                                            updateResults.Add(update);
                                            return;
                                        }

                                        ItemSlot firtProductSlot = stall.FindFirstNonEmptyStockSlotForStall(curUpdate.StallSlot);
                                        if (firtProductSlot == null || firtProductSlot.Itemstack == null)
                                        {
                                            this.Mod.Logger.Error("Tried to process network purchase on an item that didn't have any product set");
                                            update.Status = VinConstants.TRADE_STATUS_LACKS_ITEMS;
                                            updateResults.Add(update);
                                            return;
                                        }

                                        ItemSlot[] productSlots = stall.GetSlotsForStall(curUpdate.StallSlot);
                                        int perPurchase = stall.GetNumItemsPerPurchaseForStall(curUpdate.StallSlot);
                                        int totalProductsNeeded = perPurchase * curUpdate.Amount;
                                        int numPurchases = 0;

                                        foreach (ItemSlot productSlot in productSlots)
                                        {
                                            int numPossiblePurchases = productSlot.StackSize / perPurchase;
                                            int availPurchases = Math.Clamp(numPossiblePurchases, 0, curUpdate.Amount);
                                            if (availPurchases > 0)
                                            {
                                                productSlot.TakeOut(availPurchases * perPurchase);
                                                numPurchases += availPurchases;
                                            }

                                            if (numPurchases >= curUpdate.Amount)
                                            {
                                                break;
                                            }
                                        }

                                        if (numPurchases != curUpdate.Amount)
                                        {
                                            this.Mod.Logger.Warning("Tried to process network purchase on an item that didn't have enough product. Only providing partial payment");
                                        }

                                        ItemStack currency = currencyStack.Clone();
                                        currency.StackSize = currencyStack.StackSize * numPurchases;
                                        register.AddItem(currency, currency.StackSize);

                                        playersToNotify.Add(reg.Owner);
                                        update.Status = VinConstants.TRADE_STATUS_PROCESSED;
                                        updateResults.Add(update);
                                    }
                                    else
                                    {
                                        this.Mod.Logger.Debug("Shit we didnt find an entity!");
                                    }
                                });
                            }
                           
                        });
                    }
                    
                }

                if (updateResults.Count > 0)
                {
                    string jsonString = VinUtils.SerializeToJson(updateResults);
                    ViconConfig config = _coreSystem.Config;
                    string apiKey = config.GetAPIKey(_coreServerAPI.World.SavegameIdentifier);
                    VinUtils.PostAsync($"{config.tradingNetworkUrl}/api/market/purchases/pending", jsonString, null, apiKey);
                }

                if (playersToNotify.Count > 0)
                {
                    foreach(string playerId in playersToNotify)
                    {
                        IPlayer player = _coreServerAPI.World.PlayerByUid(playerId);
                        if (player != null && player.ClientId != 0)
                        {
                            _coreServerAPI.SendMessage(player, 0, "You recieved sales for cross-server trades", EnumChatType.OwnMessage);
                        }

                    }
                }


            }
        }

        private void OnNetworkProductUpdatesResponse(CompletedArgs args)
        {
            if (isValidResponse(args))
            {
                TradeNetworkPurchaseUpdates.Clear();
                this.Mod.Logger.Event("Trade Network Product Updates acknowledged and processed");
            }
        }

        private void OnNetworkShopUpdatesResponse(CompletedArgs args)
        {
            if (isValidResponse(args))
            {
                TradeNetworkUpdates = null;
                this.Mod.Logger.Event("Trade Network Shop Updates acknowledged and processed");
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
            IServerPlayer player = args.Caller.Player as IServerPlayer;

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

            TradeNetworkJoinRequest data = new TradeNetworkJoinRequest();
            data.serverName = _coreServerAPI.WorldManager.CurrentWorldName;
            data.guid = _coreServerAPI.World.SavegameIdentifier;
            data.networkAccessKey = (string) args[0];
            string jsonData = VinUtils.SerializeToJson(data);


            VinUtils.PostAsync($"{_coreSystem.Config.tradingNetworkUrl}/api/network/join", jsonData, (completionArgs) => {
                OnRegisterNetworkResponse(completionArgs);
                VinconomyCoreSystem.PrintClientMessage(args.Caller.Player, completionArgs.Response);

            }, _coreSystem.Config.GetAPIKey(data.guid));
            return TextCommandResult.Success("Requested - Check Console for updates");
        }

        private TextCommandResult LeaveNetwork(TextCommandCallingArgs args)
        {
            return TextCommandResult.Success("Success");
        }

        private void UpdateShopProductForNetwork(int shopId, BlockPos pos, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (_coreSystem.Config.tradingNetworkEnabled && _coreSystem.Config.networkAPIKeys != null)
            {
                if (TradeNetworkUpdates == null)
                {
                    TradeNetworkUpdates = new TradeNetworkShopUpdate();
                }
                TradeNetworkUpdates.AddShopUpdate(shopId, pos, stallSlot, product, numItemsPerPurchase, currency);
                this.Mod.Logger.Log(EnumLogType.Event, $"Added a new shop product update. Queue now has {TradeNetworkUpdates.GetUpdateCount()}");
            }

        }

        private void OnRegisterNetworkResponse(CompletedArgs args)
        {
            if (args.StatusCode == 200)
            {
                TradeNetworkNode node = TradeNetworkNode.FromJson(args.Response);
                if (_coreSystem.Config.networkAPIKeys == null)
                {
                    _coreSystem.Config.networkAPIKeys = new Dictionary<string, string>();
                }
                _coreSystem.Config.networkAPIKeys.Add(_coreServerAPI.World.SavegameIdentifier, node.apiKey);
                _coreServerAPI.StoreModConfig(_coreSystem.Config, VinconomyCoreSystem.CONFIG_NAME);

                Mod.Logger.Notification("Completed registration of Trade Network Node. Api Key written to config!");
            }
            else
            {
                Mod.Logger.Error($"Failed registration of Trade Network Node. Error: {args.StatusCode} : {args.ErrorMessage}");
            }
        }

        public TradeNetworkShop GetTradeNetworkFromCache(string nodeId, int shopId)
        {
            return TradeNetworkCache.GetShop(nodeId, shopId);
        }

        public void GetTradeNetworkShop(string nodeId, int shopId, OnTradeNetworkShopRecieved callback)
        {
            TradeNetworkShop shop = TradeNetworkCache.GetShop(nodeId, shopId);
            if ( shop == null) {

                ViconConfig config = _coreSystem.Config;
                string apiKey = config.GetAPIKey(_coreServerAPI.World.SavegameIdentifier);
                if (string.IsNullOrEmpty(apiKey)) return;
                if (!config.tradingNetworkEnabled) return;
                if (string.IsNullOrEmpty(config.tradingNetworkUrl)) return;

                VinUtils.GetAsync($"{_coreSystem.Config.tradingNetworkUrl}/api/shop/inventory/{nodeId}/{shopId}", delegate (CompletedArgs args)  {
                    TradeNetworkShop shop = null;
                    if (isValidResponse(args))
                    {
                        shop = VinUtils.DeserializeFromJson<TradeNetworkShop>(args.Response);
                        shop.PopulateProductMap();
                        TradeNetworkCache.AddShop(shop);
                        Mod.Logger.Notification("Added new shop to cache");
                    }
                    else
                    {
                        Mod.Logger.Error($"Failed lookup of Trade Network shop. Error: {args.ErrorMessage}");
                    }
                    callback.Invoke(shop);  
                }, apiKey);
            } else
            {
                callback.Invoke(shop);
            } 
        }

        public bool PurchaseFromNetworkShop(IPlayer customer, string nodeId, int shopId, TradeNetworkPurchasePacket purchase)
        {
            TradeNetworkShop shop = TradeNetworkCache.GetShop(nodeId, shopId);
            if (shop == null) {
                return false;
            }
            Network.Api.ShopProduct prod = shop.GetProductById(purchase.X, purchase.Y, purchase.Z, purchase.StallSlot);
            if (prod == null)
            {
                return false;
            }

            ItemStack productStack = VinUtils.DeserializeProduct(_coreServerAPI, prod.ProductCode, prod.ProductQuantity, prod.ProductAttributes);
            productStack.StackSize = productStack.StackSize * purchase.Amount;

            ItemStack currencyStack = VinUtils.DeserializeProduct(_coreServerAPI, prod.CurrencyCode, prod.CurrencyQuantity, prod.CurrencyAttributes);
            int currencyNeeded = currencyStack.StackSize * purchase.Amount;
            List<ItemSlot> currencySlots = TradingUtil.GetAllValidCurrencyFor(customer, currencyStack);
            foreach (ItemSlot slot in currencySlots)
            {
                currencyNeeded -= slot.StackSize;
                if (currencyNeeded <= 0) {
                    break;
                }
            }

            // Are they unable to afford the trade? Return
            if (currencyNeeded > 0) {
                return false;
            }

            // Is there enough product left from total stock to buy all of it?
            if (prod.TotalStock < prod.ProductQuantity * purchase.Amount) 
            {
                return false;
            }

            //Decrement the remaining stock from the Shop
            prod.TotalStock -= prod.ProductQuantity * purchase.Amount;

            //Take the money from the player.
            currencyNeeded = currencyStack.StackSize * purchase.Amount;
            foreach (ItemSlot itemSlot in currencySlots)
            {

                ItemStack takenStack = itemSlot.TakeOut(currencyNeeded);
                currencyNeeded -= takenStack.StackSize;

                itemSlot.MarkDirty();
                if (currencyNeeded <= 0) break;
            }

            //Give the player the item
            customer.InventoryManager.TryGiveItemstack(productStack, true);
            if (productStack.StackSize > 0)
            {
                _coreServerAPI.World.SpawnItemEntity(productStack, customer.Entity.Pos.XYZ.Add(0.5, 0.5, 0.5), null);
            }

            //Add the purchase to the queued list
            ShopPurchaseUpdate update = new ShopPurchaseUpdate()
            {
                StallSlot = purchase.StallSlot,
                NodeId = nodeId,
                ShopId = shopId,
                X = purchase.X,
                Y = purchase.Y,
                Z = purchase.Z,
                PlayerGuid = customer.PlayerUID,
                Name = customer.PlayerName,
                Amount = purchase.Amount,
            };



            TradeNetworkPurchaseUpdates.AddPurchaseUpdate(nodeId, shopId, update);

            Block block = productStack.Block;
            AssetLocation assetLocation = null;
            if (block != null)
            {
                BlockSounds sounds = block.Sounds;
                assetLocation = ((sounds != null) ? sounds.Place : null);
            }
            AssetLocation sound = assetLocation;
            _coreServerAPI.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), customer.Entity, customer, true, 16f, 1f);
            return true;
        }

    }
}
