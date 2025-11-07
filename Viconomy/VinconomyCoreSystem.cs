using Cairo;
using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.BlockTypes;
using Viconomy.Config;
using Viconomy.Database;
using Viconomy.Delegates;
using Viconomy.Entities;
using Viconomy.GUI;
using Viconomy.ItemTypes;
using Viconomy.Map;
using Viconomy.Network;
using Viconomy.Registry;
using Viconomy.Renderer;
using Viconomy.src.BlockTypes;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Viconomy
{
        
    public class VinconomyCoreSystem : ModSystem
    {
        #region Variables 

        public const string CONFIG_NAME = "vinconomy-core.json";

        //Client Variables
        private ICoreClientAPI _coreClientAPI;
        private IClientNetworkChannel _clientChannel;
        private Dictionary<EnumItemClass, List<IItemRenderer>> renderers;
        private bool isRegisteringRenderers;
        public ShopMapLayer ShopMapLayer { get; set; }
        private GuiDialogGeneric ShopCatalogGui;
        public static bool ShouldForceCustomerScreen;

        //Server Variables
        private ICoreServerAPI _coreServerAPI;
        private IServerNetworkChannel _serverChannel;
        public ViconDatabase DB;


        //Shared Variables
        public ViconConfig Config { get; internal set; }
        private ShopRegistry ShopRegistry { get; set; }

        #endregion Variables

        #region ModSystem Stuff
        public override double ExecuteOrder() => 1;

        public override void Start(ICoreAPI api)
        {
            api.RegisterEntity("EntityVinconTrader", typeof(EntityVinconTrader));
            //api.RegisterEntityClass("EntityVinconTrader", typeof(EntityVinconTrader));

            // 5.0 Item Mappings
            api.RegisterItemClass("VinconCoupon", typeof(ItemCoupon));

            // 5.0 Block Mappings
            api.RegisterBlockClass("VinconLiquidContainer", typeof(BlockVLiquidContainer));
            api.RegisterBlockClass("VinconDualLiquidContainer", typeof(BlockVDualLiquidContainer));
            api.RegisterBlockClass("VinconFoodContainer", typeof(BlockVFoodContainer));
            api.RegisterBlockClass("VinconCouponCutter", typeof(BlockVCouponCutter));
            api.RegisterBlockClass("VinconPurchaseContainer", typeof(BlockVPurchaseContainer));

            // 5.0 Block Entity Mappings
            api.RegisterBlockEntityClass("BEVinconLiquidContainer", typeof(BEVinconLiquidContainer));
            api.RegisterBlockEntityClass("BEVinconDualLiquidContainer", typeof(BEVinconDualLiquidContainer));
            api.RegisterBlockEntityClass("BEVinconFoodContainer", typeof(BEVinconFoodContainer));
            api.RegisterBlockEntityClass("BEVinconCouponCutter", typeof(BEVinconCouponCutter));
            api.RegisterBlockEntityClass("BEVinconPurchaseContainer", typeof(BEVinconPurchaseContainer));

            // 3.0 Block Mappings
            api.RegisterBlockClass("VinconContainer", typeof(BlockVContainer));
            api.RegisterBlockClass("VinconRegister", typeof(BlockVRegister));
            api.RegisterBlockClass("VinconTeller", typeof(BlockVTeller));
            api.RegisterBlockClass("VinconClothingDisplay", typeof(BlockVClothingDisplay));
            api.RegisterBlockClass("VinconClothingDisplayTop", typeof(BlockVClothingDisplayTop));
            api.RegisterBlockClass("VinconGacha", typeof(BlockVGacha));
            api.RegisterBlockClass("VinconGachaLoader", typeof(BlockVGachaLoader));
            api.RegisterBlockClass("VinconJobboard", typeof(BlockVJobboard));
            api.RegisterBlockClass("VinconTradeCenter", typeof(BlockVTradeCenter));

            // 3.0 Block Entity Mappings
            api.RegisterBlockEntityClass("BEVinconContainer", typeof(BEVinconContainer));
            api.RegisterBlockEntityClass("BEVinconShelf", typeof(BEVinconShelf));
            api.RegisterBlockEntityClass("BEVinconHelmetStand", typeof(BEVinconHelmetStand));
            api.RegisterBlockEntityClass("BEVinconArmorStand", typeof(BEVinconArmorStand));
            api.RegisterBlockEntityClass("BEVinconRegister", typeof(BEVinconRegister));
            api.RegisterBlockEntityClass("BEVinconTeller", typeof(BEVinconTeller));
            api.RegisterBlockEntityClass("BEVinconSculpturePad", typeof(BEVinconSculpturePad));
            api.RegisterBlockEntityClass("BEVinconWeaponrack", typeof(BEVinconWeaponRack));
            api.RegisterBlockEntityClass("BEVinconToolrack", typeof(BEVinconToolRack));
            api.RegisterBlockEntityClass("BEVinconArmorStand", typeof(BEVinconArmorStand));
            api.RegisterBlockEntityClass("BEVinconGacha", typeof(BEVinconGacha));
            api.RegisterBlockEntityClass("BEVinconGachaLoader", typeof(BEVinconGachaLoader));
            api.RegisterBlockEntityClass("BEVinconJobboard", typeof(BEVinconJobboard));
            api.RegisterBlockEntityClass("BEVinconTradeCenter", typeof(BEVinconTradeCenter));


            //2.10 Legacy Block Entity
            api.RegisterBlockClass("ViconContainer", typeof(BlockVContainer));
            api.RegisterBlockClass("ViconRegister", typeof(BlockVRegister));
            api.RegisterBlockClass("ViconTeller", typeof(BlockVTeller));
            api.RegisterBlockClass("ViconClothingDisplay", typeof(BlockVClothingDisplay));
            api.RegisterBlockClass("ViconClothingDisplayTop", typeof(BlockVClothingDisplayTop));
            api.RegisterBlockClass("ViconJobboard", typeof(BlockVJobboard));
            api.RegisterBlockClass("ViconGacha", typeof(BlockVGacha));
            api.RegisterBlockClass("ViconGachaLoader", typeof(BlockVGachaLoader));

            // 2.10 Legacy Block Entity Mappings
            api.RegisterBlockEntityClass("BEViconShelf", typeof(BEVinconShelf));
            api.RegisterBlockEntityClass("BEViconHelmetStand", typeof(BEVinconHelmetStand));
            api.RegisterBlockEntityClass("BEViconArmorStand", typeof(BEVinconArmorStand));
            api.RegisterBlockEntityClass("BEVRegister", typeof(BEVinconRegister));
            api.RegisterBlockEntityClass("BEViconTeller", typeof(BEVinconTeller));
            api.RegisterBlockEntityClass("BEViconSculpturePad", typeof(BEVinconSculpturePad));
            api.RegisterBlockEntityClass("BEViconWeaponrack", typeof(BEVinconWeaponRack));
            api.RegisterBlockEntityClass("BEViconToolrack", typeof(BEVinconToolRack));
            api.RegisterBlockEntityClass("BEViconArmorStand", typeof(BEVinconArmorStand));
            api.RegisterBlockEntityClass("BEViconGacha", typeof(BEVinconGacha));
            api.RegisterBlockEntityClass("BEViconGachaLoader", typeof(BEVinconGachaLoader));
            api.RegisterBlockEntityClass("BEViconJobboard", typeof(BEVinconJobboard));
            

            //Item Types
            api.RegisterItemClass("ViconLedger", typeof(ItemLedger));
            api.RegisterItemClass("VinconCatalog", typeof(ItemCatalog));
            api.RegisterItemClass("ViconSculptureBundle", typeof(ItemSculptureBundle));
            api.RegisterItemClass("ViconGachaBall", typeof(ItemGachaBall));
            api.RegisterItemClass("ViconTenretni", typeof(ItemTenretniBook));

            api.Network.RegisterChannel(VinConstants.VINCONOMY_CHANNEL)
                .RegisterMessageType(typeof(RegistryUpdatePacket))
                .RegisterMessageType(typeof(ShopUpdatePacket))
                .RegisterMessageType(typeof(TenretniPacket))
                .RegisterMessageType(typeof(ShopCatalogRequestPacket))
                .RegisterMessageType(typeof(ShopCatalogResponsePacket));

        }

        public override void StartPre(ICoreAPI api)
        {
            
            try
            {
                ViconConfig config = api.LoadModConfig<ViconConfig>(CONFIG_NAME);
                if (config == null)
                {
                    config = ResetModConfig();
                    api.StoreModConfig(config, CONFIG_NAME);
                }

                this.Config = config;
            }
            catch
            {
                Config = ResetModConfig();
                this.Mod.Logger.Error("Could not load Mod Config for Vinconomy. Loading defaults instead. Check your config and ensure there are no errors.");
                //api.StoreModConfig<ViconConfig>(new ViconConfig(), filename);
            }

            base.StartPre(api);
        }
       
        public override void StartServerSide(ICoreServerAPI api)
        {
            _coreServerAPI = api;
            _serverChannel = api.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL);
            _serverChannel.SetMessageHandler(new NetworkClientMessageHandler<TenretniPacket>(this.OnRecieveTenretniInfo));
            _serverChannel.SetMessageHandler(new NetworkClientMessageHandler<ShopCatalogRequestPacket>(this.OnRecieveShopCatalogRequest));

            api.Event.SaveGameLoaded += OnSaveGameLoading;
            api.Event.PlayerJoin += SendAllPublicRegisters;

            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands.GetOrCreate("vinconomy")
                .WithAlias("vincon")
                .RequiresPrivilege(Privilege.chat)
                .BeginSubCommand("setowner")
                    .RequiresPrivilege(Privilege.gamemode)
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
            _clientChannel = api.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL);
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<RegistryUpdatePacket>(this.OnRecieveRegistry));
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<ShopUpdatePacket>(this.OnRecieveRegistryUpdate));
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<ShopCatalogResponsePacket>(this.OnRecieveShopCatalogResponse));

            ShopRegistry = new ShopRegistry(DB);

            renderers = new Dictionary<EnumItemClass, List<IItemRenderer>>();
            foreach (EnumItemClass i in Enum.GetValues(typeof(EnumItemClass)))
            {
                renderers[i] = new List<IItemRenderer>();
            }

            var parsers = api.ChatCommands.Parsers;
            api.ChatCommands.GetOrCreate("vinconomy")
               .WithAlias("vincon")
               .RequiresPrivilege(Privilege.chat)
               .BeginSubCommand("forcecustomer")
                   .WithAlias("fc")
                   .WithDescription("Forces display of Customer UIs even if they are the Owner")
                   .WithArgs(parsers.Bool("Enabled"))
                   .HandleWith(SetForcedCustomer)
               .EndSubCommand();

            BeginRendererRegistration();
            RegisterRenderer(new BlockRenderer());
            RegisterRenderer(new ItemRenderer());
            RegisterRenderer(new ClutterBlockRenderer());
            RegisterRenderer(new CoinItemRenderer());
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
            RegisterCustomIcon("paper");
            RegisterCustomIcon("payment");
            RegisterCustomIcon("produce");
            RegisterCustomIcon("shield");
            RegisterCustomIcon("toolrack");
            RegisterCustomIcon("weapon");

            _coreClientAPI.Event.OnTestBlockAccess += TestAccess;
            OnTestAccess += AllowStallUse;

            api.ModLoader.GetModSystem<WorldMapManager>().RegisterMapLayer<ShopMapLayer>("vinconomyShop", 20);

            api.RegisterLinkProtocol("viewmap", OnMapLinkClicked);


            // Ensure Pastebin is on the client whitelist for now, untill I get an actual client-sided whitelist implemented.
            // Also ensure that the BaseURL is on the CLIENT's whitelist, and not the server. We dont want people using "Pastebin.com" as the name, but actually points somewhere else. 
            if (Config.ViconTenretniWhitelists == null || Config.ViconTenretniWhitelists.Length == 0)
            {
                Config.ViconTenretniWhitelists = new ViconTenretniWhitelist[] { new ViconTenretniWhitelist { name = "Pastebin.com", baseURL = "https://pastebin.com/raw/" } };

            }

        }

        private TextCommandResult SetForcedCustomer(TextCommandCallingArgs args)
        {
            ShouldForceCustomerScreen = (bool)args[0];
            return TextCommandResult.Success("Customer UI Forcing is now " + ShouldForceCustomerScreen);
        }

        #endregion ModSystem Stuff

        public ViconConfig ResetModConfig()
        {
            ViconConfig config = new ViconConfig();
            config.ViconTenretniWhitelists = new ViconTenretniWhitelist[] { new ViconTenretniWhitelist { name = "Pastebin.com", baseURL = "https://pastebin.com/raw/" } };

            return config;
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
            if (entity is IOwnableStall)
            {
                ((IOwnableStall)entity).SetOwner(playerUUID, playerData.LastKnownPlayername);
            }
            else if (entity is BEVinconRegister)
            {
                BEVinconRegister register = ((BEVinconRegister)entity);
                ShopRegistry.ClearShop(register.ID);
                register.Owner = playerUUID;
                AddShop(playerUUID, playerData.LastKnownPlayername, playerData.LastKnownPlayername + "'s Shop", register.Pos, register.ID);
            }
            else
            {
                return TextCommandResult.Error("Please target a Viconomy Stall or Register. (Wrong Class)");
            }

            return TextCommandResult.Success("Set owner to " + playerData.LastKnownPlayername + " for " + pos.ToString());
        }

        public void BeginRendererRegistration()
        {
            isRegisteringRenderers = true;
        }

        public void RegisterRenderer(IItemRenderer blockRenderer)
        {
            renderers[blockRenderer.getRendererClass()].Add(blockRenderer);
            if (!isRegisteringRenderers)
            {
                renderers[blockRenderer.getRendererClass()].Sort((i1, i2) => { return i2.getPriority().CompareTo(i1.getPriority()); });
            }
        }
        
        public void EndRendererRegistration()
        {
            isRegisteringRenderers = false;
            foreach (EnumItemClass i in Enum.GetValues(typeof(EnumItemClass)))
            {
                renderers[i].Sort((i1, i2) => { return i2.getPriority().CompareTo(i1.getPriority()); });
            }

        }

        private void RegisterCustomIcon(string key)
        {
            _coreClientAPI.Gui.Icons.CustomIcons["vicon-"+key] = delegate (Context ctx, int x, int y, float w, float h, double[] rgba)
           {
               AssetLocation loc = new AssetLocation("vinconomy:textures/icons/slot-" + key + ".svg");
               IAsset asset = _coreClientAPI.Assets.TryGet(loc, true);
               int color = ColorUtil.ColorFromRgba(175, 200, 175, 125);
               _coreClientAPI.Gui.DrawSvg(asset, ctx.GetTarget() as ImageSurface, x, y, (int)w, (int)h, color);
           };
        }

        public BEVinconRegister GetShopRegister(string owner, int registerID)
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

            BEVinconRegister viconRegister = _coreServerAPI.World.BlockAccessor.GetBlockEntity(register.Position) as BEVinconRegister;
            if (viconRegister != null)
            {
                return viconRegister;
            } 
            //TODO: Disabling this until I figure out a better handling for registers in unloaded chunks
            //else
            //{
            //    ShopRegistry.ClearShopPos(registerID);
            //}
            return null;
        }

        public ShopRegistry GetRegistry()
        {
            return ShopRegistry;
        }
       
        public ShopRegistration AddShop(string owner, string ownerName, string name, BlockPos pos, int ID = -1)
        {
            if (_coreServerAPI != null)
            {
                ShopRegistration reg = this.GetRegistry().AddShop(owner, ownerName, name, pos, ID);
               
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
                BroadcastShopUpdate(iD);
                UpdateShopDelegates(reg);
            }

            return reg;
        }

        public void UpdateShopConfig(int iD, string desc, string shortDesc, string webHook)
        {
            ShopRegistration reg = ShopRegistry.UpdateShopConfig(iD, desc, shortDesc, webHook);
            if (_coreServerAPI != null)
            {
                BroadcastShopUpdate(iD);
                UpdateShopDelegates(reg);
            }
        }

        public void SendShopOwnerUpdate(ShopRegistration shop, IServerPlayer player)
        {
            ShopUpdatePacket update;
            if (shop == null)
            {
                update = new ShopUpdatePacket(shop.ID);
            }
            else
            {
                update = new ShopUpdatePacket(shop, true);
            }

            _serverChannel.SendPacket(update, player);
        }


        public static void PrintClientMessage(IPlayer player, string message, object[] args = null)
        {
            if (message == null)
                return;

            if (player == null)
                return;

            if (args == null)
            {
                args = [];
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
            this.Mod.Logger.Error("Did not get a renderer for item " + stack.Collectible.Code.Path);
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

                IServerPlayer owner = (IServerPlayer)_coreServerAPI.World.PlayerByUid(shop.Owner);

                List<IServerPlayer> excludeList = new List<IServerPlayer>
                {
                    owner
                };

                if (owner.ConnectionState == EnumClientState.Playing)
                {
                    SendShopOwnerUpdate(shop, owner);
                }

                //broadcast owner packet to all those with permission to the shop
                foreach (ShopAccess access in shop.Permissions.Values)
                {
                    IServerPlayer granted = (IServerPlayer)_coreServerAPI.World.PlayerByUid(access.PlayerUID);
                    SendShopOwnerUpdate(shop, granted);
                    excludeList.Add(granted); // Dont send them the waypoint update if its broadcasted, too...
                }

                if (shop.IsWaypointBroadcasted)
                {
                    ShopUpdatePacket update = new ShopUpdatePacket(shop, false);
                    _serverChannel.BroadcastPacket(update, excludeList.ToArray());
                }
            }
        }


        private void SendAllPublicRegisters(IServerPlayer player)
        {
            List<ShopRegistration> shops = this.ShopRegistry.GetAllShops();
            List<ShopUpdatePacket> updates = new List<ShopUpdatePacket>();
            if (shops != null)
            {
               foreach (ShopRegistration shop in shops)
                {
                    if (shop.IsWaypointBroadcasted || shop.CanAccess(player))
                    {
                        updates.Add(new ShopUpdatePacket(shop, shop.Owner == player.PlayerUID));
                    }
                }
            }
            _serverChannel.SendPacket(new RegistryUpdatePacket(updates), player);
        }
        
        #region Event Delegates

        //Deprecated until I convert everything over.
        public void PurchasedItem(TradeResult result, ItemStack product, ItemStack payment)
        {
            GenericTradeRequest req = new GenericTradeRequest(result.coreApi, result.customer);
            req.WithShop(result.shopRegister, result.sellingEntity, 0, result.sellingEntity.IsAdminShop);
            req.WithProduct(product, null, 0);
            req.WithCurrency(payment, null, 0);
            GenericTradeResult res = new GenericTradeResult(req, this);
            res.TransferedProductTotal = product.StackSize;
            res.TransferedCurrencyTotal = payment.StackSize;
            res.TransferedProduct.Add(product);
            res.TransferedCurrency.Add(payment);
            PurchasedItem(res);
        }

        /// <summary>
        /// Called when an item is purchased. <br/><br/>
        /// player: The player making the purchase<br/>
        /// register: The register that payment is meant to go to<br/>
        /// stallSlot: the stall slot the player is purchasing from<br/>
        /// product: The (clone) stack of items to be transfered to the player - Do not modify this<br/>
        /// payment: the stack of items representing payment to be stored in the Register
        /// </summary>
        /// 
        public void PurchasedItem(GenericTradeResult result)
        {
            ItemStack product = result.GetTotalTransferedProduct();
            ItemStack payment = result.GetTotalTransferedCurrency();

            if (payment == null && result.Request.GetFinalCurrencyNeededPerPurchase() == 0 && result.Request.CouponStackNeeded != null)
            {
                ItemStack couponStack = result.Request.CouponStackNeeded.Clone();
                couponStack.StackSize = result.Request.NumPurchases;
                payment = couponStack;
            }

            if (OnPurchasedItem != null)
            {
                Delegate[] delegates = OnPurchasedItem.GetInvocationList();
                foreach (Delegate delegator in delegates)
                {
                    try
                    {
                        ((OnPurchasedItemDelegate)delegator).Invoke(result, product, payment);
                    }
                    catch (Exception e)
                    {
                        this.Mod.Logger.Error(e);
                    }
                }
            }

            DB.SavePurchase(result, product, payment);
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
        public bool CanPurchaseItem(IPlayer player, BEVinconBase stall, BEVinconRegister register, int stallSlot, int desiredAmount)
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
                    try
                    {
                        result = ((TryPlaceBlockDelegate)delegator).Invoke(world, byPlayer, itemstack, blockSel, result);
                    } catch (Exception e)
                    {
                        this.Mod.Logger.Error(e);
                    }
                }
            }
            
            return result;
        }
        public event TryPlaceBlockDelegate OnTryPlaceBlock;

        public void UpdateShopProduct(BEVinconBase stall, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            if (OnUpdateShopProduct != null)
            {
                Delegate[] delegates = OnUpdateShopProduct.GetInvocationList();
                foreach (Delegate delegator in delegates)
                {
                    try
                    {
                        ((OnUpdateShopProductDelegate)delegator).Invoke(stall, stallSlot, product, numItemsPerPurchase, currency);
                    }
                    catch (Exception e)
                    {
                        this.Mod.Logger.Error(e);
                    }
                }
            }
        }
        public event OnUpdateShopProductDelegate OnUpdateShopProduct;

        public bool BlockBroken(AssetLocation code, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier)
        {
            bool result = true;
            if (OnBlockBroken != null)
            {
                Delegate[] delegates = OnBlockBroken.GetInvocationList();
                foreach (Delegate delegator in delegates)
                {
                    try
                    {
                        result = OnBlockBroken.Invoke(code, world, pos, byPlayer, dropQuantityMultiplier);
                    }
                    catch (Exception e)
                    {
                        this.Mod.Logger.Error(e);
                    }
                }
            }
            return result;
        }
        public event OnBlockBrokenDelegate OnBlockBroken;

        public void BlockPlaced(AssetLocation code, IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            if (OnBlockPlaced != null)
            {
                Delegate[] delegates = OnBlockPlaced.GetInvocationList();
                foreach (Delegate delegator in delegates)
                {
                    try
                    {
                        ((OnBlockPlacedDelegate)delegator).Invoke(code, world, blockPos, byItemStack);
                    }
                    catch (Exception e)
                    {
                        this.Mod.Logger.Error(e);
                    }
                }
            }
        }
        public event OnBlockPlacedDelegate OnBlockPlaced;

        public EnumWorldAccessResponse TestAccess(IPlayer player, BlockSelection blockSelection, EnumBlockAccessFlags accessType, ref string claimant, EnumWorldAccessResponse response)
        {
            if (OnTestAccess != null)
            {
                EnumWorldAccessResponse multicastResult = response;
                Delegate[] delegates = OnTestAccess.GetInvocationList();
                foreach (Delegate delegator in delegates)
                {
                    try { 
                        multicastResult = ((OnTestAccessDelegate)delegator).Invoke(player, blockSelection, accessType, claimant, multicastResult);
                    } catch (Exception e) {
                        this.Mod.Logger.Error(e);
                    }
            }
                return multicastResult;
            }
            else return response;
        }
        public event OnTestAccessDelegate OnTestAccess;

        public void UpdateShopDelegates(ShopRegistration shop)
        {
            if (OnUpdateShop != null)
            {
                Delegate[] delegates = OnUpdateShop.GetInvocationList();
                foreach (Delegate delegator in delegates)
                {
                    try
                    {
                        ((OnUpdateShopDelegate)delegator).Invoke(shop);
                    }
                    catch (Exception e)
                    {
                        this.Mod.Logger.Error(e);
                    }
                }
            }
        }
        public event OnUpdateShopDelegate OnUpdateShop;

        #endregion Event Delegates

        public EnumWorldAccessResponse AllowStallUse(IPlayer player, BlockSelection blockSelection, EnumBlockAccessFlags accessType, string claimant, EnumWorldAccessResponse response)
        {
            ICoreAPI api = this._coreServerAPI;
            if (api == null)
            {
                // We are the Client!
                api = this._coreClientAPI;
            }

            //this.Mod.Logger.Debug("AllowStallUse - Claimant is:" + claimant + " Response: " + response + " accessType: " + accessType);

            if (accessType == EnumBlockAccessFlags.Use && response == EnumWorldAccessResponse.LandClaimed)
            {
                Block block = blockSelection.Block;
                if (block == null)
                {
                    // I dont know why Block isnt set.
                    block = api.World.BlockAccessor.GetBlock(blockSelection.Position);
                }
                if (block is BlockVContainer || block is BlockVClothingDisplay || block is BlockVClothingDisplayTop || block is BlockVTeller)   
                    return EnumWorldAccessResponse.Granted;
            }
            return response;
        }

        public void UpdateStallProductForStall(BEVinconBase stall, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            int shopId = stall.RegisterID;
            BlockPos pos = stall.Pos;

            if (shopId <= 0 || product == null || currency == null) {
                DB.DeleteShopProduct(pos,stallSlot);
            } else
            {
                DB.UpdateShopProduct(shopId,pos,stallSlot,product,numItemsPerPurchase,currency);
            }

            UpdateShopProduct(stall, stallSlot, product, numItemsPerPurchase, currency);
        }

        public ShopCatalog RequestShopCatalog(ShopRegistration shop, bool includeProductList)
        {
            ShopCatalog catalog = new ShopCatalog
            {
                Name = shop.Name,
                OwnerName = shop.OwnerName,
                ID = shop.ID,
            };

            if (shop.IsWaypointBroadcasted)
            {
                catalog.IsWaypointBroadcasted = true;
                catalog.X = shop.X - _coreServerAPI.WorldManager.MapSizeX/2;
                catalog.Y = shop.Y;
                catalog.Z = shop.Z - _coreServerAPI.WorldManager.MapSizeZ/2;
                catalog.WorldX = shop.X;
                catalog.WorldZ = shop.Z;
            }

            catalog.Description = shop.Description;
            catalog.ShortDescription = shop.ShortDescription;

            if (includeProductList)
            {
                ShopProductList products = DB.GetShopProducts(shop.ID);
                catalog.Products = products;
            }
            return catalog;

        }

       

        #region Callbacks
        private void OnRecieveShopCatalogRequest(IServerPlayer fromPlayer, ShopCatalogRequestPacket request)
        {
            ShopCatalogResponsePacket response = new ShopCatalogResponsePacket();
            int shopId = request.ShopId; 
            if (shopId > 0) {
                ShopRegistration reg = ShopRegistry.GetShop(shopId);
                if (reg != null)
                {
                    response.ShopCatalog = RequestShopCatalog(reg, true);
                }
            } 

            if (shopId <= 0 || request.IncludeShopList)
            {
                List<ShopRegistration> regs = ShopRegistry.GetAllShops();
                response.ShopList = new List<ShopCatalog>();
                foreach (ShopRegistration reg in regs)
                {
                    response.ShopList.Add(RequestShopCatalog(reg, false));
                }
            }

            _serverChannel.SendPacket(response, fromPlayer);
        }

        private void OnRecieveShopCatalogResponse(ShopCatalogResponsePacket response)
        {
            if (response.ShopCatalog != null)
            {
                ShopCatalogGui = new GuiVinconShopCatalog("Shop Catalog", response.ShopCatalog, response.ShopList, _coreClientAPI);
                
            } else 
            {
                ShopCatalogGui = new GuiVinconCatalog("Shop Catalog", response.ShopList, _coreClientAPI);
            }


            ShopCatalogGui.TryOpen();
        }

        private void OnMapLinkClicked(LinkTextComponent component)
        {

            string[] array = component.Href.Substring("viewmap://".Length).Split('=');
            int x = int.Parse(array[0]);
            int y = int.Parse(array[1]);
            int z = int.Parse(array[2]);
            WorldMapManager mapMan = _coreClientAPI.ModLoader.GetModSystem<WorldMapManager>();
            if (!mapMan.worldMapDlg.IsOpened() || mapMan.worldMapDlg.DialogType != EnumDialogType.Dialog)
            {
                
                mapMan.ToggleMap(EnumDialogType.Dialog);
                //mapMan.worldMapDlg.TryOpen();
            }
            if (ShopCatalogGui != null && ShopCatalogGui.IsOpened())
            {
                ShopCatalogGui.TryClose();
            }
            (mapMan.worldMapDlg.SingleComposer.GetElement("mapElem") as GuiElementMap).CenterMapTo(new BlockPos(x, y, z, 1));
        }

        private void OnRecieveRegistry(RegistryUpdatePacket packet)
        {
            ShopRegistry = new ShopRegistry(DB);
            if (packet.registry != null)
            {
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
            this.Mod.Logger.Debug("Got an update packet for shop " + packet.ID + ". It has coords " + packet.X + "/" + packet.Y + "/" + packet.Z + " and broadcasting is " + packet.IsWaypointBroadcasted);
            if (packet.IsRemoval)
            {
                ShopRegistry.ClearShop(packet.ID);
            }
            else
            {
                ShopRegistry.UpdateShopFromServer(packet);
            }

            if (ShopMapLayer != null)
                this.ShopMapLayer.RebuildMapComponents();
        }

        private void OnRecieveTenretniInfo(IServerPlayer fromPlayer, TenretniPacket packet)
        {
            ItemSlot slot = fromPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot == null || slot.Itemstack == null || slot.Itemstack.Item == null)
            {
                return;
            }
            string name = slot.Itemstack.Item.Code.GetName();
            if (name.StartsWith("book-tenretni") && slot.Itemstack.Attributes.GetString("Archive") == null)
            {
                ITreeAttribute attr = slot.Itemstack.Attributes;
                attr.SetString("Archive", packet.Archive);
                attr.SetString("BaseURL", packet.BaseURL);
                attr.SetString("ID", packet.ID);
                attr.SetString("Name", packet.Name);
                attr.SetString("Scribe", fromPlayer.PlayerName);
                slot.MarkDirty();
            }
        }

        private void OnSaveGameLoading()
        {
            this.Mod.Logger.Debug("+============== Loading Viconomy ==============+");
            DB.CleanupShops();
            DB.LoadShops(ShopRegistry);

            this.Mod.Logger.Debug("| Loaded " + ShopRegistry.GetCount() + " Shops");

            foreach (string ownerId in ShopRegistry.GetAllShopOwners())
            {
                var shops = ShopRegistry.GetShopsForOwner(ownerId);
                this.Mod.Logger.Debug("|  Loaded " + shops.Length + " Shops for Owner: " + ownerId);
                foreach (ShopRegistration shop in shops)
                {

                    if (shop.Position == null)
                    {
                        this.Mod.Logger.Debug("|   Skipping Shop " + shop.Name + " (" + shop.ID + ") as it does not exist anymore");
                    }
                    else
                    {
                        this.Mod.Logger.Debug("|   Loading Shop " + shop.Name);

                    }
                }
            }

            this.Mod.Logger.Debug("=============== Loaded Viconomy ================");
        }

        public void PersistConfig()
        {
            if (_coreServerAPI != null)
            {
                _coreServerAPI.StoreModConfig(Config, CONFIG_NAME);
            }
           
        }

        #endregion Callbacks

        /*
        public RayTraceResults DoPlayerRaytrace(IClientPlayer player)
        {
            BlockSelection blockSelection = null;
            EntitySelection entitySelection = null;

            Vec3d camPos = new Vec3d(player.Entity.SidedPos.X, player.Entity.SidedPos.Y + player.Entity.LocalEyePos.Y, player.Entity.SidedPos.Z);

            _coreClientAPI.World.RayTraceForSelection(camPos, player.CameraPitch, player.CameraYaw, 300f, ref blockSelection, ref entitySelection, null, null);

            return new RayTraceResults(blockSelection, entitySelection);

        }
        */
    }


}
