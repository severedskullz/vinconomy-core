using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Viconomy.Config;
using Viconomy.Entities;
using Viconomy.GUI;
using Viconomy.Network.Api;
using Viconomy.Network.Common;
using Viconomy.TradeNetwork.Api;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Viconomy.BlockEntities
{
    public class BEVinconTradeCenter : BlockEntity
    {
        GuiVinconNetworkCatalog dialog;
        VinconomyCoreSystem _coreSystem;

        public long LastSpawnedTraderId { get; private set; }
        public string NodeId { get; private set; }
        public long ShopId { get; private set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            _coreSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
        }
        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                dialog = new GuiVinconNetworkCatalog("Network Shops", this.Pos, (ICoreClientAPI) this.Api);
                dialog.TryOpen();
            }
            
            return true;
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == VinConstants.SEARCH_SHOPS)
            {
                List<SearchResult> results = SerializerUtil.Deserialize<List<SearchResult>>(data);

                if (dialog != null)
                {
                    dialog.PopulateResultsFromServer(results);
                }
            }
            else if (packetid == VinConstants.GET_PRODUCTS)
            {
                TradeNetworkShop results = SerializerUtil.Deserialize<TradeNetworkShop>(data);

                if (dialog != null)
                {
                    dialog.LoadShopResults(results);
                }
            }

        }

        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            ViconConfig config = _coreSystem.Config;
            if (!config.tradingNetworkEnabled)
            {
                return;
            }
            string apiKey = config.GetAPIKey(this.Api.World.SavegameIdentifier);
            if (apiKey == null)
            {
                return;
            }



            if (packetid == VinConstants.SEARCH_SHOPS)
            {
                string shopName = null;
                string currencyName = null;
                string productName = null;

                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    shopName = reader.ReadString();
                    productName = reader.ReadString();
                    currencyName = reader.ReadString();

                }

                System.Text.Json.Nodes.JsonObject json = new System.Text.Json.Nodes.JsonObject
                {
                    { "shopName", shopName },
                    { "currencyName", currencyName },
                    { "productName", productName }
                };

                VinUtils.PostAsync($"{config.tradingNetworkUrl}/api/market/search", json.ToJsonString(), OnRecieveAPISearchResults, apiKey, fromPlayer);
            } else if (packetid == VinConstants.GET_PRODUCTS)
            {
                string nodeId = "";
                long shopId = 0;

                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    nodeId = reader.ReadString();
                    shopId = reader.ReadInt64();
                }

                VinUtils.GetAsync($"{config.tradingNetworkUrl}/api/shop/inventory/{nodeId}/{shopId}", OnRecieveAPIProducts, apiKey, fromPlayer);
            }
            else if (packetid == VinConstants.SET_TRADER)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    NodeId = reader.ReadString();
                    ShopId = reader.ReadInt64();

                }
                //TODO: See about implementing some sort of Cost to summon them?

                SummonTrader();
            }
            else if (packetid == VinConstants.SUMMON_TRADER)
            {
                SummonTrader();
            }

        }

        private void OnRecieveAPIProducts(HttpCompletionArgs args)
        {
            if (args.State == CompletionState.Good)
            {

                TradeNetworkShop result = VinUtils.DeserializeFromJson<TradeNetworkShop>(args.Response);
                byte[] data = SerializerUtil.Serialize(result);
                // Has a String "data" overload, but it doesnt seem to work with JSON. Cuts off on the first quote character for some reason
                (Api as ICoreServerAPI).Network.SendBlockEntityPacket(args.RequestingPlayer as IServerPlayer, this.Pos, VinConstants.GET_PRODUCTS, data);
                
            }
            else
            {
                SpringErrorMessage error = VinUtils.DeserializeFromJson<SpringErrorMessage>(args.Response);
                _coreSystem.Mod.Logger.Error("There was an error retrieving search results: {0} {1} - {2}", new object[] { error.status, error.error, error.message });
                VinconomyCoreSystem.PrintClientMessage(args.RequestingPlayer, "There was an error retrieving search results");
            }
        }

        private void OnRecieveAPISearchResults(HttpCompletionArgs args)
        {
            if (args.State == CompletionState.Good)
            {
                List<SearchResult> results = VinUtils.DeserializeFromJson<List<SearchResult>>(args.Response);
                byte[] data = SerializerUtil.Serialize(results);
                // Has a String "data" overload, but it doesnt seem to work with JSON. Cuts off on the first quote character for some reason
                (Api as ICoreServerAPI).Network.SendBlockEntityPacket(args.RequestingPlayer as IServerPlayer, this.Pos, VinConstants.SEARCH_SHOPS, data); 
            }
            else
            {
                SpringErrorMessage error = VinUtils.DeserializeFromJson<SpringErrorMessage>(args.Response);
                _coreSystem.Mod.Logger.Error("There was an error retrieving search results: {0} {1} - {2}" , new object[]{ error.status, error.error, error.message});
                VinconomyCoreSystem.PrintClientMessage(args.RequestingPlayer, "There was an error retrieving search results");
            }
        }

        public void SummonTrader()
        {
            if (LastSpawnedTraderId > 0)
            {
                EntityVinconTrader ent = (Api as ICoreServerAPI).World.GetEntityById(LastSpawnedTraderId) as EntityVinconTrader;
                if (ent != null)
                {
                    ent.SetNetworkShop(Pos, NodeId, ShopId);

                    ent.ServerPos.SetFrom(Pos.ToVec3d());
                    return;
                }
            }

            
            EntityProperties type = Api.World.GetEntityType(new AssetLocation("vinconomy", "vincon-trader"));
            EntityVinconTrader entity = Api.World.ClassRegistry.CreateEntity(type) as EntityVinconTrader;
            entity.SetNetworkShop(Pos, NodeId, ShopId);
            entity.ServerPos.SetFrom(Pos.ToVec3d());
            Api.World.SpawnEntity(entity);
            LastSpawnedTraderId = entity.EntityId;


        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetLong("LastSpawnedTraderId", LastSpawnedTraderId);
            tree.SetLong("ShopId", ShopId);
            tree.SetString("NodeId", NodeId);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            LastSpawnedTraderId = tree.GetLong("LastSpawnedTraderId");
            ShopId = tree.GetLong("LastSpawnedTraderId");
            NodeId = tree.GetString("LastSpawnedTraderId");
        }

    }
}
