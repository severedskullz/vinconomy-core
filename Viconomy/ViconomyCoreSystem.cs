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
using Vintagestory.GameContent;
using Viconomy.Map;
using System.Numerics;

namespace Viconomy
{
    using RayTraceResults = Tuple<BlockSelection, EntitySelection>;

    public class ViconomyCoreSystem : ModSystem
    {
        //Client Variables
        private ICoreClientAPI _coreClientAPI;
        private IClientNetworkChannel _clientChannel;
        private Dictionary<EnumItemClass, List<IItemRenderer>> renderers;
        private bool isRegisteringRenderers;
        public ShopMapLayer ShopMapLayer { get; set; }

        //Server Variables
        private ICoreServerAPI _coreServerAPI;
        private IServerNetworkChannel _serverChannel;
        public ViconDatabase DB;

        //Shared Variables
        public ViconConfig Config { get; internal set; }
        private ShopRegistry ShopRegistry { get; set; }


        public override double ExecuteOrder() => 1;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
 
            api.RegisterBlockClass("ViconContainer", typeof(BlockVContainer));
            api.RegisterBlockClass("ViconRegister", typeof(BlockVRegister));
            api.RegisterBlockClass("ViconTeller", typeof(BlockVTeller));
            api.RegisterBlockClass("ViconClothingDisplay", typeof(BlockVClothingDisplay));
            api.RegisterBlockClass("ViconClothingDisplayTop", typeof(BlockVClothingDisplayTop));
            api.RegisterBlockClass("ViconJobboard", typeof(BlockVJobboard));

            api.RegisterBlockEntityClass("BEViconStall", typeof(BEViconStall));
            api.RegisterBlockEntityClass("BEViconHelmetStand", typeof(BEViconHelmetStand));
            api.RegisterBlockEntityClass("BEViconArmorStand", typeof(BEViconArmorStand));
            api.RegisterBlockEntityClass("BEVRegister", typeof(BEVRegister));
            api.RegisterBlockEntityClass("BEViconTeller", typeof(BEViconTeller));
            api.RegisterBlockEntityClass("BEViconShelf", typeof(BEViconShelf));
            api.RegisterBlockEntityClass("BEViconSculpturePad", typeof(BEViconSculpturePad));
            api.RegisterBlockEntityClass("BEViconWeaponrack", typeof(BEViconWeaponRack));
            api.RegisterBlockEntityClass("BEViconToolrack", typeof(BEViconToolRack));
            api.RegisterBlockEntityClass("BEViconArmorStand", typeof(BEViconArmorStand));
            api.RegisterBlockEntityClass("BEViconJobboard", typeof(BEViconJobboard));

            api.RegisterItemClass("ViconLedger", typeof(ItemLedger));
            api.RegisterItemClass("ViconSculptureBundle", typeof(ItemSculptureBundle));

            api.Network.RegisterChannel("Vinconomy")
                .RegisterMessageType(typeof(RegistryUpdatePacket))
                .RegisterMessageType(typeof(ShopUpdatePacket));

        }

        public override void StartPre(ICoreAPI api)
        {
            string filename = "vinconomy-core.json";
            try
            {
                ViconConfig config = api.LoadModConfig<ViconConfig>(filename);
                if (config == null)
                {
                    config = new ViconConfig();
                    api.StoreModConfig<ViconConfig>(config, filename);
                }
                else
                {
                    this.Config = config;
                }
            }
            catch
            {
                Config = new ViconConfig();
                //api.StoreModConfig<ViconConfig>(new ViconConfig(), filename);
            }

            base.StartPre(api);
        }



        public override void StartServerSide(ICoreServerAPI api)
        {
            _coreServerAPI = api;
            _serverChannel = api.Network.GetChannel("Vinconomy");
          
            api.Event.SaveGameLoaded += OnSaveGameLoading;
            api.Event.PlayerJoin += SendAllPublicRegisters;

            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create("viconomy")
                .WithAlias("vicon")
                .RequiresPrivilege(Privilege.gamemode)
                .BeginSubCommand("setowner")
                    .WithDescription("Sets the Owner of the Stall or Register at the given position to the provided player.")
                    .WithArgs(parsers.Word("Player Name"), parsers.Int("Block X"), parsers.Int("Block Y"), parsers.Int("Block Z"))
                    .HandleWith(SetOwner)
                .EndSubCommand();

            _coreServerAPI.Event.OnTestBlockAccess += TestAccess;
            OnTestAccess += AllowStallUse;

            DB = new ViconDatabase(api);
            ShopRegistry = new ShopRegistry(DB);

        }


        public override void StartClientSide(ICoreClientAPI api)
        {
            _coreClientAPI = api;
            _clientChannel = api.Network.GetChannel("Vinconomy");
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<RegistryUpdatePacket>(this.OnRecieveRegistry));
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<ShopUpdatePacket>(this.OnRecieveRegistryUpdate));

            ShopRegistry = new ShopRegistry(DB);

            renderers = new Dictionary<EnumItemClass, List<IItemRenderer>>();
            foreach (EnumItemClass i in Enum.GetValues(typeof(EnumItemClass)))
            {
                renderers[i] = new List<IItemRenderer>();
            }

            BeginRendererRegistration();
                RegisterRenderer(new BlockRenderer());
                RegisterRenderer(new ItemRenderer());
                RegisterRenderer(new ClutterBlockRenderer());
                RegisterRenderer(new MicroBlockRenderer());
            EndRendererRegistration();

            RegisterCustomIcon("arms");
            RegisterCustomIcon("belt");
            RegisterCustomIcon("body");
            RegisterCustomIcon("boots");
            RegisterCustomIcon("face");
            RegisterCustomIcon("general");
            RegisterCustomIcon("general2");
            RegisterCustomIcon("hands");
            RegisterCustomIcon("hands2");
            RegisterCustomIcon("hat");
            RegisterCustomIcon("helmet");
            RegisterCustomIcon("legs");
            RegisterCustomIcon("neck");
            RegisterCustomIcon("payment");
            RegisterCustomIcon("produce");
            RegisterCustomIcon("shield");
            RegisterCustomIcon("toolrack");
            RegisterCustomIcon("weapon");

            _coreClientAPI.Event.OnTestBlockAccess += TestAccess;
            OnTestAccess += AllowStallUse;

            api.ModLoader.GetModSystem<WorldMapManager>().RegisterMapLayer<ShopMapLayer>("vinconomyShop", 20);

        }

        private void BeginRendererRegistration()
        {
            isRegisteringRenderers = true;
        }

        private void RegisterRenderer(IItemRenderer blockRenderer)
        {
            renderers[blockRenderer.getRendererClass()].Add(blockRenderer);
            if (!isRegisteringRenderers)
            {
                renderers[blockRenderer.getRendererClass()].Sort((i1, i2) => { return i2.getPriority().CompareTo(i1.getPriority()); });
            }
        }
        private void EndRendererRegistration()
        {
            isRegisteringRenderers = false;
            foreach (EnumItemClass i in Enum.GetValues(typeof(EnumItemClass)))
            {
                renderers[i].Sort((i1, i2) => { return i2.getPriority().CompareTo(i1.getPriority()); });
            }

        }


        private TextCommandResult SetOwner(TextCommandCallingArgs args)
        {

            IServerPlayerData playerData = _coreServerAPI.PlayerData.GetPlayerDataByLastKnownName((string)args[0]);

            if (playerData == null)
            {
                return TextCommandResult.Error("No player with that name.");
            }
            BlockPos pos = new BlockPos((int)args[1] + (_coreServerAPI.WorldManager.MapSizeX / 2),
                (int)args[2],
                (int)args[3] + (_coreServerAPI.WorldManager.MapSizeZ / 2),
                0);

            BlockEntity entity = _coreServerAPI.World.BlockAccessor.GetBlockEntity(pos);
            if (entity == null)
            {
                return TextCommandResult.Error("Please target a Viconomy Stall or Register. (Entity)");
            }

            string playerUUID = playerData.PlayerUID;
            if (entity is BEViconBase) {
                ((BEViconBase)entity).SetOwner(playerUUID, playerData.LastKnownPlayername);
            }
            else if (entity is BEVRegister) 
            {
                BEVRegister register = ((BEVRegister)entity);
                ShopRegistry.ClearShop(register.ID);
                register.Owner = playerUUID;
                UpdateShop(playerUUID, register.ID, playerData.LastKnownPlayername + "'s Shop", register.Pos);
            } else
            {
                return TextCommandResult.Error("Please target a Viconomy Stall or Register. (Wrong Class)");
            }

            return TextCommandResult.Success("Set owner to " + playerData.LastKnownPlayername + " for " +pos.ToString());
        }

        private void RegisterCustomIcon(String key)
        {
            _coreClientAPI.Gui.Icons.CustomIcons["vicon-"+key] = delegate (Context ctx, int x, int y, float w, float h, double[] rgba)
           {
               AssetLocation loc = new AssetLocation("vinconomy:textures/icons/slot-" + key + ".svg");
               IAsset asset = _coreClientAPI.Assets.TryGet(loc, true);
               int color = ColorUtil.ColorFromRgba(175, 200, 175, 125);
               _coreClientAPI.Gui.DrawSvg(asset, ctx.GetTarget() as ImageSurface, x, y, (int)w, (int)h, color);
           };
        }

        public RayTraceResults DoPlayerRaytrace(IClientPlayer player)
        {
            BlockSelection blockSelection = null;
            EntitySelection entitySelection = null;

            Vec3d camPos = new Vec3d(player.Entity.SidedPos.X, player.Entity.SidedPos.Y + player.Entity.LocalEyePos.Y, player.Entity.SidedPos.Z);

            _coreClientAPI.World.RayTraceForSelection(camPos, player.CameraPitch, player.CameraYaw, 300f, ref blockSelection, ref entitySelection, null, null);

            return new RayTraceResults(blockSelection, entitySelection);

        }

        private void OnRecieveRegistry(RegistryUpdatePacket packet)
        {
            ShopRegistry = new ShopRegistry(DB);
            if (packet.registry != null) {
                foreach (ShopUpdatePacket item in packet.registry)
                {
                    this.ShopRegistry.AddShop(new ShopRegistration(item));
                }
            }
            if (ShopMapLayer != null)
                this.ShopMapLayer.RebuildMapComponents();
        }

        private void OnRecieveRegistryUpdate(ShopUpdatePacket packet)
        {
            if (packet.IsRemoval)
            {
                ShopRegistry.ClearShop(packet.ID);
            } else
            {
                ShopRegistry.UpdateShopFromServer(packet);
            }

            if (ShopMapLayer != null)
                this.ShopMapLayer.RebuildMapComponents();
        }

        public BEVRegister GetShopRegister(string owner, int registerID)
        {
            if (owner == null)
            {
                return null;
            }

            ShopRegistration register = ShopRegistry.GetShop(registerID);
            if (register == null || register.Position == null) { return null; }

            //Make sure they still own the shop
            if (register.Owner != owner)
            {
                return null;
            }

            BEVRegister viconRegister = _coreServerAPI.World.BlockAccessor.GetBlockEntity(register.Position) as BEVRegister;
            if (viconRegister != null)
            {
                return viconRegister;
            } else
            {
                ShopRegistry.ClearShop(registerID);
            }
            return null;
        }

        public ShopRegistry GetRegistry()
        {
            return ShopRegistry;
        }

        private void OnSaveGameLoading()
        {
            this._coreServerAPI.Logger.Debug("+============== Loading Viconomy ==============+");

            DB.LoadShops(ShopRegistry);

            this._coreServerAPI.Logger.Debug("| Loaded " + ShopRegistry.GetCount() + " Shops");

            foreach (string ownerId in ShopRegistry.GetAllShopOwners())
            {
                var shops = ShopRegistry.GetShopsForOwner(ownerId);
                this._coreServerAPI.Logger.Debug("|  Loaded " + shops.Length + " Shops for Owner: " + ownerId);
                foreach (ShopRegistration shop in shops)
                {
                    this._coreServerAPI.Logger.Debug("|   Loading Shop " + shop.Name);

                    if (shop.Position == null)
                    {
                        ShopRegistry.GetShopsForOwner(ownerId);
                        this._coreServerAPI.Logger.Debug("|     Shop " + shop.Name + " (" + shop.ID + ") does not exist anymore. Removing...");
                    }
                }
            }

            this._coreServerAPI.Logger.Debug("=============== Loaded Viconomy ================");
        }

        private void SendAllPublicRegisters(IServerPlayer player)
        {
            List<ShopRegistration> shops = this.ShopRegistry.GetAllShops();
            List<ShopUpdatePacket> updates = new List<ShopUpdatePacket>();
            if (shops != null)
            {
               foreach (ShopRegistration shop in shops)
                {
                    if (shop.IsWaypointBroadcasted || shop.Owner == player.PlayerUID)
                    {
                        updates.Add(new ShopUpdatePacket(shop));
                    }
                }
            }
            _serverChannel.SendPacket(new RegistryUpdatePacket(updates), player);
        }

        public ShopRegistration AddShop(string owner, string ownerName, string name, BlockPos pos)
        {
            if (_coreServerAPI != null)
            {
                ShopRegistration reg = this.GetRegistry().AddShop(owner, ownerName, name, pos);
               
                if (reg.IsWaypointBroadcasted)
                {
                    BroadcastShopUpdate(reg.ID);
                }
                else
                {
                    IServerPlayer player = (IServerPlayer)_coreServerAPI.World.PlayerByUid(owner);
                    if (player.ConnectionState == EnumClientState.Playing)
                    {
                        SendShopOwnerUpdate(reg, player);
                    }
                }
                return reg;
            }
            return null;
        }



        public ShopRegistration UpdateShop(string owner, int iD, string name, BlockPos pos)
        {
            ShopRegistration reg = ShopRegistry.UpdateShop(iD, name, pos);
            if (_coreServerAPI != null)
            {
                if (reg.IsWaypointBroadcasted)
                {
                    BroadcastShopUpdate(iD);
                } else
                {
                    IServerPlayer player = (IServerPlayer)_coreServerAPI.World.PlayerByUid(owner);
                    if (player.ConnectionState == EnumClientState.Playing)
                    {
                        SendShopOwnerUpdate(reg, player);
                    }
                }                
            }
            return reg;
        }

        public static void PrintClientMessage(IPlayer player, string message, object[] args = null)
        {
            if (player == null)
                return;

            if (args == null)
            {
                args = Array.Empty<object>();
            }
            if (player is IServerPlayer)
            {
                (player as IServerPlayer).SendMessage(0, Lang.GetL((player as IServerPlayer)?.LanguageCode ?? "en", message, args), EnumChatType.OwnMessage, null);
            }
            else
            {
                ((IClientPlayer)player).ShowChatNotification(Lang.Get(message, args));
            }
        }


        public IItemRenderer GetRenderer(ItemStack stack)
        {
            List<IItemRenderer> typeRenderers = renderers[stack.Class];
            foreach (IItemRenderer renderer in typeRenderers)
            {
                if (renderer.canHandle(stack))
                    return renderer;
            }

            //This should have returned already, but in case someone fucked up
            this._coreServerAPI.Logger.Error("Did not get a renderer for item " + stack.Collectible.Code.Path);
            if (stack.Class == EnumItemClass.Block)
                return new BlockRenderer();
            else
                return new ItemRenderer();
        }

        public void UpdateShopWaypoint(int ID, bool enabled, string icon, int color)
        {
            ShopRegistry.UpdateShopWaypoint(ID, enabled, icon, color);
            BroadcastShopUpdate(ID);
        }

        private void BroadcastShopUpdate(int shopId)
        {
            ShopRegistration shop = ShopRegistry.GetShop(shopId);
            if (shop != null)
            {
                ShopUpdatePacket update = new ShopUpdatePacket(shop);
                _serverChannel.BroadcastPacket(update);
            }
        }

        private void SendShopOwnerUpdate(ShopRegistration shop, IServerPlayer player)
        {
            ShopUpdatePacket update;
            if (shop == null)
            {
                update = new ShopUpdatePacket(shop.ID);
            }
            else
            {
                update = new ShopUpdatePacket(shop);
            }

            _serverChannel.SendPacket(update, player);
        }

        #region Event Delegates

        /// <summary>
        /// Called when an item is purchased. <br/><br/>
        /// player: The player making the purchase<br/>
        /// register: The register that payment is meant to go to<br/>
        /// stallSlot: the stall slot the player is purchasing from<br/>
        /// product: The (clone) stack of items to be transfered to the player - Do not modify this<br/>
        /// payment: the stack of items representing payment to be stored in the Register
        /// </summary>
        public void PurchasedItem(TradeResult result, ItemStack product, ItemStack payment)
        {
            OnPurchasedItem?.Invoke(result, product, payment);
            DB.SavePurchase(result);
        }
        public event OnPurchasedItemDelegate OnPurchasedItem;



        /// <summary>
        /// Called befire an item is purchased to determine if the player is allowed to purchase this item. Return true if it should allow the player to purchase the item <br/><br/>
        /// player: The player making the purchase<br/>
        /// register: The register that payment is meant to go to<br/>
        /// stallSlot: the stall slot the player is purchasing from<br/>
        /// desiredAmount: How many sales are in this transaction
        /// </summary>
        /// 
        //TODO: Multicast support
        public bool CanPurchaseItem(IPlayer player, BEViconBase stall, BEVRegister register, int stallSlot, int desiredAmount)
        {
            bool result = true;
            if (OnCanPurchaseItem != null)
            {
                result = OnCanPurchaseItem.Invoke(player, stall, register, stallSlot, desiredAmount);
            }
            return result;
        }
        public event CanPurchaseItemDelegate OnCanPurchaseItem;

        public bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel)
        {
            bool result = true;
            if (OnTryPlaceBlock != null)
            {
                Delegate[] delegates = OnTryPlaceBlock.GetInvocationList();
                foreach (Delegate delegator in delegates)
                {
                    result = ((TryPlaceBlockDelegate)delegator).Invoke(world, byPlayer, itemstack, blockSel, result);
                }
            }
            
            return result;
        }
        public event TryPlaceBlockDelegate OnTryPlaceBlock;

        public bool BlockBroken(AssetLocation code, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier)
        {
            bool result = true;
            if (OnTryPlaceBlock != null)
            {
                result = OnBlockBroken.Invoke(code, world, pos, byPlayer, dropQuantityMultiplier);
            }
            return result;
        }
        public event OnBlockBrokenDelegate OnBlockBroken;

        public void BlockPlaced(AssetLocation code, IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            OnBlockPlaced?.Invoke(code, world, blockPos, byItemStack);
        }
        public event OnBlockPlacedDelegate OnBlockPlaced;

        public EnumWorldAccessResponse TestAccess(IPlayer player, BlockSelection blockSelection, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response)
        {
            if (OnTestAccess != null)
            {
                EnumWorldAccessResponse multicastResult = response;
                Delegate[] delegates = OnTestAccess.GetInvocationList();
                foreach (Delegate delegator in delegates)
                {
                    multicastResult = ((OnTestAccessDelegate)delegator).Invoke(player, blockSelection, accessType, claimant, multicastResult);
                }
                return multicastResult;
            }
            else return response;
        }
        public event OnTestAccessDelegate OnTestAccess;

        public EnumWorldAccessResponse AllowStallUse(IPlayer player, BlockSelection blockSelection, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response)
        {
            ICoreAPI api = this._coreServerAPI;
            if (api == null)
            {
                // We are the Client!
                api = this._coreClientAPI;
            }
            
            //this._coreClientAPI.Logger.Debug("AllowStallUse - Claimant is:" + claimant + " Response: " + response + " accessType: " + accessType);

            if (accessType == EnumBlockAccessFlags.Use && response == EnumWorldAccessResponse.LandClaimed)
            {
                Block block = blockSelection.Block;
                if (block == null)
                {
                    // I dont know why Block isnt set.
                    block = api.World.BlockAccessor.GetBlock(blockSelection.Position);
                }
                if (block is BlockVContainer || block is BlockVClothingDisplay || block is BlockVClothingDisplayTop)   
                    return EnumWorldAccessResponse.Granted;
            }
            return response;
        }

        #endregion

    }
}
