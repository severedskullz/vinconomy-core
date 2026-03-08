using System.IO;
using System.Linq;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vinconomy.GUI;
using Vinconomy.Network;
using Vintagestory.API.MathTools;
using Vinconomy.BlockEntities;
using System.Collections.Generic;
using Viconomy.Network.JavaApi.TradeNetwork;

namespace Vinconomy.Entities
{
    public class EntityVinconTrader : EntityAgent
    {
        //private EntityTrader trader; // Literally here so I can quickly go decompile the class for references. Remove when done

        private bool IsLoading;
        private string NodeId;
        private long ShopId;
        private BlockPos Origin;
        public EntityTalkUtil talkUtil;
        private EntityPlayer talkingWithPlayer;
        private GuiVinconTrader tradeDialog;
        private EntityBehaviorConversable ConversableBh => GetBehavior<EntityBehaviorConversable>();
        private VinconomyTradingIntegrationSystem TradingIntegrationSystem;

        public string Personality
        {
            get
            {
                return WatchedAttributes.GetString("personality", "formal");
            }
            set
            {
                WatchedAttributes.SetString("personality", value);
                talkUtil?.SetModifiers(EntityTrader.Personalities[value].ChordDelayMul, EntityTrader.Personalities[value].PitchModifier, EntityTrader.Personalities[value].VolumneModifier);
            }
        }

        public void SetNetworkShop(BlockPos origin, string nodeId, long shopId)
        {
            Origin = origin;
            NodeId = nodeId;
            ShopId = shopId;
        }
        

        public EntityVinconTrader()
        {
            AnimManager = new PersonalizedAnimationManager();
        }

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            //Inventory.LateInitialize("trade-" + EntityId, api);
            if (api.Side == EnumAppSide.Client)
            {
                talkUtil = new EntityTalkUtil(api as ICoreClientAPI, this, isMultiSoundVoice: false);
            }

            (AnimManager as PersonalizedAnimationManager).Personality = Personality;
            Personality = Personality;

            EntityBehaviorConversable behavior = GetBehavior<EntityBehaviorConversable>();
            if (behavior != null)
            {
                behavior.OnControllerCreated = (Action<DialogueController>)Delegate.Combine(behavior.OnControllerCreated, delegate (DialogueController controller)
                {
                    controller.DialogTriggers += Dialog_DialogTriggers;
                });
            }

            TradingIntegrationSystem = api.ModLoader.GetModSystem<VinconomyTradingIntegrationSystem>();



            
        }

        private int Dialog_DialogTriggers(EntityAgent triggeringEntity, string value, JsonObject data)
        {
            if (value == "opentrade")
            {
                ConversableBh.Dialog?.TryClose();
                talkingWithPlayer = triggeringEntity as EntityPlayer;
                TryOpenTradeDialog(triggeringEntity);
                return 0;
            }

            return -1;
        }

        private void TryOpenTradeDialog(EntityAgent forEntity)
        {
            if (!Alive)
                return;

            if (World.Side == EnumAppSide.Client)
            {
                return;
            }

            if (IsLoading)
            {
                VinconomyCoreSystem.PrintClientMessage(talkingWithPlayer.Player, "The trader is setting up shop. Please wait a moment.");
                return;
            }
            IsLoading = true;

            ICoreServerAPI coreServerAPI = (ICoreServerAPI)Api;
            VinconomyTradingIntegrationSystem tradingNetwork = coreServerAPI.ModLoader.GetModSystem<VinconomyTradingIntegrationSystem>();
            tradingNetwork.GetTradeNetworkShop(NodeId, ShopId, (shop) => {
                if (shop != null)
                {
                    ICoreServerAPI coreServerAPI = Api as ICoreServerAPI;
                    byte[] data = SerializerUtil.Serialize(shop);
                    coreServerAPI.Network.SendEntityPacket(talkingWithPlayer.Player as IServerPlayer, EntityId, 2000, data);
                } else
                {
                    VinconomyCoreSystem.PrintClientMessage(talkingWithPlayer.Player, "There was an error retrieving the shop");
                }
                IsLoading = false;
            });
        }

        public void OpenTradeNetworkDialogue(TradeNetworkShop shop)
        {
            ICoreClientAPI coreClientAPI = (ICoreClientAPI)Api;
            if (shop.Products.Count <= 0)
            {
                coreClientAPI.TriggerIngameError(this, "noproducts", Lang.Get("This trader has no products for sale"));
                return;
            }

            IPlayer player = World.PlayerByUid(talkingWithPlayer.PlayerUID);
            
            if (talkingWithPlayer.Pos.SquareDistanceTo(Pos) <= 5f)
            {
                GuiDialog guiDialog = tradeDialog;
                if (guiDialog == null || !guiDialog.IsOpened())
                {
                    if (coreClientAPI.Gui.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiVinconTrader && dlg.IsOpened()) == null)
                    {
                        coreClientAPI.Network.SendEntityPacket(EntityId, 1001);
                        //player.InventoryManager.OpenInventory(Inventory); // Why do I need an Inventory, again?
                        tradeDialog = new GuiVinconTrader(shop.Name, shop, this,  World.Api as ICoreClientAPI);
                        tradeDialog.TryOpen();
                    }
                    else
                    {
                        coreClientAPI.TriggerIngameError(this, "onlyonedialog", Lang.Get("Can only trade with one trader at a time"));
                    }

                    return;
                }
            }
            
            //coreClientAPI.Network.SendPacketClient(coreClientAPI.World.Player.InventoryManager.CloseInventory(Inventory));
        }

        public override void OnEntitySpawn()
        {
            base.OnEntitySpawn();
            if (World.Api.Side == EnumAppSide.Server)
            {
                Personality = EntityTrader.Personalities.GetKeyAtIndex(World.Rand.Next(EntityTrader.Personalities.Count));
                (AnimManager as PersonalizedAnimationManager).Personality = Personality;

                setupTaskBlocker();
            }
        }

        public override void OnEntityLoaded()
        {
            base.OnEntityLoaded();
            if (Api.Side == EnumAppSide.Server)
            {
                setupTaskBlocker();

                if (Origin != null)
                {
                    BEVinconTradeCenter spawner = (Api as ICoreServerAPI).World.BlockAccessor.GetBlockEntity<BEVinconTradeCenter>(Origin);
                    if (spawner == null || spawner.LastSpawnedTraderId != this.EntityId)
                    {
                        TradingIntegrationSystem.Mod.Logger.Debug("Cleaning up Network Trader due to missing Trade Center or mismatched Entity ID");
                        this.Die(EnumDespawnReason.Removed);
                    }
                }
                else
                {
                    TradingIntegrationSystem.Mod.Logger.Debug("Cleaning up Network Trader due Origin not being set");
                    this.Die(EnumDespawnReason.Removed);
                }

            }
        }

        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
            if (packetid == 2001)
            {
                TradeNetworkPurchasePacket purchase = SerializerUtil.Deserialize<TradeNetworkPurchasePacket>(data);
                bool result = TradingIntegrationSystem.PurchaseFromNetworkShop(player, NodeId, ShopId, purchase);
                if (result)
                {
                    //Payment was successful - send the packet right back to the player.
                    ((ICoreServerAPI)Api).Network.SendEntityPacket(player, this.EntityId, 2002, data);
                } else
                {
                    TradingIntegrationSystem.Mod.Logger.Error("Error occurred attempting to purchase from network shop.");

                }


            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
            if (packetid == 199)
            {
                if (!Alive)
                {
                    return;
                }

                talkUtil.Talk(EnumTalkType.Hurt);
            }

            if (packetid == 198)
            {
                talkUtil.Talk(EnumTalkType.Death);
            }

            if (packetid == 2000)
            {
                TradeNetworkShop shop = SerializerUtil.Deserialize<TradeNetworkShop>(data);
                OpenTradeNetworkDialogue(shop);
            }

            if (packetid == 2002)
            {
                // Update TotalStock for new inventory
                TradeNetworkPurchasePacket purchase = SerializerUtil.Deserialize<TradeNetworkPurchasePacket>(data);
                tradeDialog.UpdateSlotCount(purchase.X, purchase.Y, purchase.Z, purchase.StallSlot, purchase.Amount);
            }
        }

        public override void OnGameTick(float dt)
        {
            base.OnGameTick(dt);
            if (Alive && AnimManager.ActiveAnimationsByAnimCode.Count == 0)
            {
                AnimManager.StartAnimation(new AnimationMetaData
                {
                    Code = "idle",
                    Animation = "idle",
                    EaseOutSpeed = 10000f,
                    EaseInSpeed = 10000f
                });
            }

            if (World.Side == EnumAppSide.Client)
            {
                talkUtil.OnGameTick(dt);
            }
        }

        public override void FromBytes(BinaryReader reader, bool forClient)
        {
            base.FromBytes(reader, forClient);
            (AnimManager as PersonalizedAnimationManager).Personality = Personality;
        }

        public override void Revive()
        {
            base.Revive();
            if (Attributes.HasAttribute("spawnX"))
            {
                Pos.X = Attributes.GetDouble("spawnX");
                Pos.Y = Attributes.GetDouble("spawnY");
                Pos.Z = Attributes.GetDouble("spawnZ");
            }
        }

        public override void PlayEntitySound(string type, IPlayer dualCallByPlayer = null)// , bool randomizePitch = true, float range = 24f)
        {
            if (type == "hurt" && World.Side == EnumAppSide.Server)
            {
                (World.Api as ICoreServerAPI).Network.BroadcastEntityPacket(EntityId, 199);
            }
            else if (type == "death" && World.Side == EnumAppSide.Server)
            {
                (World.Api as ICoreServerAPI).Network.BroadcastEntityPacket(EntityId, 198);
            }
            else
            {
                base.PlayEntitySound(type, dualCallByPlayer); //, randomizePitch, range);
            }
        }

        protected void setupTaskBlocker()
        {
        }

        /*
        protected void setupTaskBlocker()
        {
            EntityBehaviorTaskAI behavior = GetBehavior<EntityBehaviorTaskAI>();
            if (behavior != null)
            {
                behavior.TaskManager.OnShouldExecuteTask += (IAiTask task) => talkingWithPlayer == null;
            }

            EntityBehaviorActivityDriven behavior2 = GetBehavior<EntityBehaviorActivityDriven>();
            if (behavior2 != null)
            {
                behavior2.OnShouldRunActivitySystem += () => talkingWithPlayer == null;
            }
        }
        */



        public override void ToBytes(BinaryWriter writer, bool forClient)
        {
            base.ToBytes(writer, forClient);
            writer.Write(NodeId == null ? "" : NodeId);
            writer.Write(ShopId);
            if (Origin != null)
            {
                writer.Write(Origin.X);
                writer.Write(Origin.Y);
                writer.Write(Origin.Z);
            } else
            {
                // Should never happen, but *JUST* in case. :)
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
            }

        }

        public override void FromBytes(BinaryReader reader, bool isSync, Dictionary<string, string> serversideRemaps)
        {
            base.FromBytes(reader, isSync, serversideRemaps);

            NodeId = reader.ReadString();
            ShopId = reader.ReadInt64();
            BlockPos pos = new BlockPos(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            Origin = pos;
        }

    }
}