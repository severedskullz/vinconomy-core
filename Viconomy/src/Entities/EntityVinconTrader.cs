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
using Viconomy.GUI;
using Viconomy.TradeNetwork;

namespace Viconomy.Entities
{
    public class EntityVinconTrader : EntityAgent
    {
        private EntityTrader trader; // Literally here so I can quickly go decompile the class for references. Remove when done


        public EntityTalkUtil talkUtil;
        private EntityPlayer talkingWithPlayer;
        private GuiVinconTrader tradeDialog;
        private InventoryBase Inventory;
        public EntityTalkUtil TalkUtil => talkUtil;
        private EntityBehaviorConversable ConversableBh => GetBehavior<EntityBehaviorConversable>();

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

        

        public EntityVinconTrader()
        {
            AnimManager = new PersonalizedAnimationManager();
            Inventory = new InventoryGeneric(10,"trade-1234", null);
        }

        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            Inventory.LateInitialize("trade-123", api);
            if (api.Side == EnumAppSide.Client)
            {
                talkUtil = new EntityTalkUtil(api as ICoreClientAPI, this, isMultiSoundVoice: false);
            }

            (AnimManager as PersonalizedAnimationManager).Personality = Personality;
            Personality = Personality;

            EntityBehaviorConversable behavior = GetBehavior<EntityBehaviorConversable>();
            if (behavior != null)
            {
                behavior.OnControllerCreated = (Action<DialogueController>)Delegate.Combine(behavior.OnControllerCreated, (Action<DialogueController>)delegate (DialogueController controller)
                {
                    controller.DialogTriggers += Dialog_DialogTriggers;
                });
            }
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
                /*
               
                */
                return;
            }

            ICoreServerAPI coreServerAPI = (ICoreServerAPI)Api;
            VinconomyTradingIntegrationSystem tradingNetwork = coreServerAPI.ModLoader.GetModSystem<VinconomyTradingIntegrationSystem>();
            tradingNetwork.GetTradeNetworkShop("f0d34c1a-472a-47c9-9cad-1c7317a434d3", 1, (shop) => {
                if (shop != null)
                {
                    ICoreServerAPI coreServerAPI = Api as ICoreServerAPI;
                    byte[] data = SerializerUtil.Serialize(shop);
                    coreServerAPI.Network.SendEntityPacket(talkingWithPlayer.Player as IServerPlayer, EntityId, 2000, data);
                }

            });
        }

        public void OpenTradeNetworkDialogue(TradeNetworkShop shop)
        {
            ICoreClientAPI coreClientAPI = (ICoreClientAPI)Api;
            if (shop.products.Count <= 0)
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
                    if (coreClientAPI.Gui.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiViconTrader && dlg.IsOpened()) == null)
                    {
                        coreClientAPI.Network.SendEntityPacket(EntityId, 1001);
                        player.InventoryManager.OpenInventory(Inventory);
                        tradeDialog = new GuiVinconTrader(shop.name, shop, World.Api as ICoreClientAPI);
                        tradeDialog.TryOpen();
                    }
                    else
                    {
                        coreClientAPI.TriggerIngameError(this, "onlyonedialog", Lang.Get("Can only trade with one trader at a time"));
                    }

                    return;
                }
            }
            
            coreClientAPI.Network.SendPacketClient(coreClientAPI.World.Player.InventoryManager.CloseInventory(Inventory));
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
            }
        }

        public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);           
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
                ServerPos.X = Attributes.GetDouble("spawnX");
                ServerPos.Y = Attributes.GetDouble("spawnY");
                ServerPos.Z = Attributes.GetDouble("spawnZ");
            }
        }

        public override void PlayEntitySound(string type, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 24f)
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
                base.PlayEntitySound(type, dualCallByPlayer, randomizePitch, range);
            }
        }

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

    }
}