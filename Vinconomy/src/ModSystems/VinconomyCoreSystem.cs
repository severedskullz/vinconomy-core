
using Commercially.Common.Blocks.BlockTypes;
using Commercially.Common.Interactions;
using Commercially.Common.Inventory.Slots;
using Commercially.Common.ModSystems;
using Commercially.Common.Registry;
using Commercially.Common.Registry.Packets;
using Commercially.Common.Util;
using Commercially.Vinconomy.BlockEntityBehaviors;
using Commercially.Vinconomy.BlockEntityBehaviors.DisplayProviders;
using Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders;
using Commercially.Vinconomy.Config;
using Commercially.Vinconomy.Database;
using Commercially.Vinconomy.GUI.Tabs;
using Commercially.Vinconomy.Interactions;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Network.Packets;
using Commercially.Vinconomy.Registry;
using Commercially.Vinconomy.Trading;
using Commercially.Vinconomy.Trading.Processor;
using Commercially.Vinconomy.Util;
using System;
using System.Collections.Generic;
using Vinconomy.Delegates;
using Vinconomy.GUI;
using Vinconomy.ItemTypes;
using Vinconomy.Map;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.ModSystems
{
    public class VinconomyCoreSystem : ModSystem
    {
        private const string ShopRegisterType = "ShopRegister";
        private ICoreServerAPI _CoreServerAPI;
        private ICoreClientAPI _CoreClientAPI;
        private IServerNetworkChannel _ServerChannel;
        private IClientNetworkChannel _ClientChannel;

        public CommerciallyModSystem CommerciallySystem { get; private set; }
        public Dictionary<long, ShopConfiguration> ShopConfiguration { get; private set; }

        private static Dictionary<string, Type> StallTypes;

        private readonly string CONFIG_NAME = "vinconomy.json";
        public VinconomyConfig Config;
        public VinconomyDatabase DB { get; private set; }
        public ShopMapLayer ShopMapLayer { get; internal set; }

        private GuiDialogGeneric ShopCatalogGui;

        public override double ExecuteOrder() => 1.1;

        public VinconomyConfig ResetModConfig()
        {
            VinconomyConfig config = new();

            return config;
        }

        public override void StartPre(ICoreAPI api)
        {
            StallTypes = [];


            try
            {
                VinconomyConfig config = api.LoadModConfig<VinconomyConfig>(CONFIG_NAME);
                if (config == null)
                {
                    config = ResetModConfig();
                    api.StoreModConfig(config, CONFIG_NAME);
                }

                Config = config;
            }
            catch
            {
                Config = ResetModConfig();
                Mod.Logger.Error("Could not load Mod Config for Vinconomy. Loading defaults instead. Check your config and ensure there are no errors.");
                //api.StoreModConfig<ViconConfig>(new ViconConfig(), filename);
            }

            if (api.Side == EnumAppSide.Server)
                DB = new VinconomyDatabase((ICoreServerAPI)api);



            base.StartPre(api);
        }




        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            CommerciallySystem = api.ModLoader.GetModSystem<CommerciallyModSystem>();
            ShopConfiguration = new Dictionary<long, ShopConfiguration>();

            api.Network.RegisterChannel(VinConstants.VINCONOMY_CHANNEL)
                //.RegisterMessageType(typeof(RegistryUpdatePacket))
                //.RegisterMessageType(typeof(ShopUpdatePacket))
                .RegisterMessageType(typeof(CatalogRequestPacket))
                .RegisterMessageType(typeof(CatalogResponsePacket));

           // api.Event.OnTestBlockAccess += TestAccess;
            

            //api.RegisterBlockClass("Commercially.BlockCommercial", typeof(BlockCommercialBase));

            //api.RegisterBlockEntityClass("Commercially.BECommercialBase", typeof(BECommercialBase));

            Lifecycle_RegisterBlockEntityBehaviors(api);
            Lifecycle_RegisterItemClasses(api);
            Lifecycle_RegisterStallTypes(api);
            Lifecycle_RegisterModularTabs(api);
            Lifecycle_RegisterInteractions(api);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (_CoreClientAPI != null)
            {
                Dictionary<string, MeshData> cache = ObjectCacheUtil.TryGet<Dictionary<string, MeshData>>(_CoreClientAPI, "stallMeshesDisplay");
                if (cache != null)
                {
                    foreach (MeshData data in cache.Values) { data.Dispose(); }
                    ObjectCacheUtil.Delete(_CoreClientAPI, "stallMeshesDisplay");
                }
            }
        }
        public void Lifecycle_RegisterInteractions(ICoreAPI api)
        {
            CommerciallySystem.RegisterInteraction(AddStockInteraction.Key, new AddStockInteraction());
            CommerciallySystem.RegisterInteraction(AddMealInteraction.Key, new AddMealInteraction());
            CommerciallySystem.RegisterInteraction(PurchaseItemInteraction.Key, new PurchaseItemInteraction());
            CommerciallySystem.RegisterInteraction(OpenStallInteraction.Key, new OpenStallInteraction());
            CommerciallySystem.RegisterInteraction(BindLedgerInteraction.Key, new BindLedgerInteraction());
            CommerciallySystem.RegisterInteraction(OpenRegisterInteraction.Key, new OpenRegisterInteraction());
            
        }

        public void Lifecycle_RegisterStallTypes(ICoreAPI api)
        {
            RegisterStallType("GenericStallSlot", typeof(GenericStallSlot));
            RegisterStallType("MealStallSlot", typeof(MealStallSlot));
            RegisterStallType("LiquidStallSlot", typeof(LiquidStallSlot));
            RegisterStallType("TellerStallSlot", typeof(TellerStallSlot));
            RegisterStallType("SculptureStallSlot", typeof(SculptureStallSlot));
            RegisterStallType("PurchaseStallSlot", typeof(PurchaseStallSlot));
            RegisterStallType("GachaStallSlot", typeof(GachaStallSlot));
        }

        public void Lifecycle_RegisterModularTabs(ICoreAPI api)
        {
            ModularGUIModSystem guiSystem = api.ModLoader.GetModSystem<ModularGUIModSystem>();
            guiSystem.RegisterTabType(ShopCustomerTab.CODE, typeof(ShopCustomerTab));
            guiSystem.RegisterTabType(ShopOwnerTab.CODE, typeof(ShopOwnerTab));
            guiSystem.RegisterTabType(DisplayDebugTab.CODE, typeof(DisplayDebugTab));
            guiSystem.RegisterTabType(RegisterConfigTab.CODE, typeof(RegisterConfigTab));
            guiSystem.RegisterTabType(CouponCutterTab.CODE, typeof(CouponCutterTab));
            guiSystem.RegisterTabType(MealShopOwnerTab.CODE, typeof(MealShopOwnerTab));
            guiSystem.RegisterTabType(ClothingStandCustomerTab.CODE, typeof(ClothingStandCustomerTab));
            guiSystem.RegisterTabType(SculptureShopOwnerTab.CODE, typeof(SculptureShopOwnerTab));
            guiSystem.RegisterTabType(PurchaseStallOwnerTab.CODE, typeof(PurchaseStallOwnerTab));
            guiSystem.RegisterTabType(GachaStallOwnerTab.CODE, typeof(GachaStallOwnerTab));
            guiSystem.RegisterTabType(GachaStallCustomerTab.CODE, typeof(GachaStallCustomerTab));
            guiSystem.RegisterTabType(LiquidShopOwnerTab.CODE, typeof(LiquidShopOwnerTab));
            guiSystem.RegisterTabType(RegisterContentsTab.CODE, typeof(RegisterContentsTab));
        }

        public void Lifecycle_RegisterItemClasses(ICoreAPI api)
        {
            api.RegisterItemClass("Vinconomy.Ledger", typeof(ItemLedger));
            api.RegisterItemClass("Vinconomy.Catalog", typeof(ItemCatalog));
            api.RegisterItemClass("Vinconomy.SculptureBundle", typeof(ItemSculptureBundle));
            api.RegisterItemClass("Vinconomy.GachaBall", typeof(ItemGachaBall));
            api.RegisterItemClass("Vinconomy.Tenretni", typeof(ItemTenretniBook));
            api.RegisterItemClass("Vinconomy.Coupon", typeof(ItemCoupon));  
        }

        public void Lifecycle_RegisterBlockEntityBehaviors(ICoreAPI api)
        {
            api.RegisterBlockEntityBehaviorClass("Vinconomy.Stall", typeof(BEStallBehavior));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.GachaStall", typeof(BEGachaStallBehavior));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.Register", typeof(BEShopBehavior));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.CouponCutter", typeof(BECouponCutterBehavior));


            api.RegisterBlockEntityBehaviorClass("Vinconomy.RegisterInventory", typeof(RegisterInventoryProvider));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.GachaStallInventory", typeof(GachaStallInventoryProvider));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.StallInventory", typeof(GenericStallInventoryProvider));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.LiquidInventory", typeof(LiquidStallInventoryProvider));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.MealInventory", typeof(MealStallInventoryProvider));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.SculptureInventory", typeof(SculptureStallInventoryProvider));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.PurchaseInventory", typeof(PurchaseStallInventoryProvider));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.TellerInventory", typeof(TellerInventoryProvider));

            api.RegisterBlockEntityBehaviorClass("Vinconomy.MealDisplay", typeof(DisplayMealContentsBehavior));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.LiquidDisplay", typeof(DisplayLiquidContentsBehavior));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.StallDisplay", typeof(DisplayContentsBehavior));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.SculptureDisplay", typeof(DisplaySculptureBehavior));
            api.RegisterBlockEntityBehaviorClass("Vinconomy.PurchaseDisplay", typeof(DisplayPurchaseContentsBehavior));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _CoreServerAPI = api;
            
            _ServerChannel = api.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL);
            _ServerChannel.SetMessageHandler(new NetworkClientMessageHandler<CatalogRequestPacket>(OnRecieveCatalogRequest));            

            DB.InitializeDB();
            ShopConfiguration = DB.LoadShopConfiguration();

        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            _CoreClientAPI = api;
            _ClientChannel = api.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL);
            _ClientChannel.SetMessageHandler(new NetworkServerMessageHandler<CatalogResponsePacket>(this.OnRecieveCatalogResponse));

            api.RegisterLinkProtocol("viewmap", OnMapLinkClicked);
            //api.ModLoader.GetModSystem<WorldMapManager>().RegisterMapLayer<ShopMapLayer>("vinconomyShop", 20);
        }

        private void OnMapLinkClicked(LinkTextComponent component)
        {

            string[] array = component.Href.Substring("viewmap://".Length).Split('=');
            int x = int.Parse(array[0]);
            int y = int.Parse(array[1]);
            int z = int.Parse(array[2]);
            WorldMapManager mapMan = _CoreClientAPI.ModLoader.GetModSystem<WorldMapManager>();
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

        private void OnRecieveCatalogRequest(IServerPlayer fromPlayer, CatalogRequestPacket request)
        {
            CatalogResponsePacket response = new CatalogResponsePacket();
            long shopId = request.ShopId;
            if (shopId > 0)
            {
                OwnableRegistration reg = CommerciallySystem.OwnableRegistry.GetOwnable(shopId);
                if (reg != null)
                {
                    response.ShopCatalog = RequestShopCatalog(reg, true);
                }
            }

            if (shopId <= 0 || request.IncludeShopList)
            {
                //TODO: Will need to make this more flexible at some point for modders. Its fine for now, as I dont expect them to move *that* fast.
                List<OwnableRegistration> regs = CommerciallySystem.OwnableRegistry.GetAllOwnablesForType(ShopRegisterType);
                response.ShopList = new List<ShopCatalog>();
                foreach (OwnableRegistration reg in regs)
                {
                    response.ShopList.Add(RequestShopCatalog(reg, false));
                }
            }

            _ServerChannel.SendPacket(response, fromPlayer);
        }

        public ShopCatalog RequestShopCatalog(OwnableRegistration shop, bool includeProductList)
        {
            ShopCatalog catalog = new ShopCatalog();

            OwnableEntry entry = new OwnableEntry
            {
                Name = shop.Name,
                OwnerName = shop.OwnerName,
                ID = shop.ID,
            };

            if (shop.BroadcastWaypoint)
            {
                entry.IsWaypointBroadcasted = true;
                entry.X = shop.X - _CoreServerAPI.WorldManager.MapSizeX / 2;
                entry.Y = shop.Y;
                entry.Z = shop.Z - _CoreServerAPI.WorldManager.MapSizeZ / 2;
                entry.WorldX = shop.X;
                entry.WorldZ = shop.Z;
            }
            catalog.Ownable = entry;


            ShopConfiguration.TryGetValue(shop.ID, out ShopConfiguration config);
            if (config != null)
            {
                catalog.Description = config.Description;
                catalog.ShortDescription = config.ShortDescription;
            }


            if (includeProductList)
            {
                ShopProductList products = DB.GetShopProducts(shop.ID);
                catalog.ProductList = products;
            }
            return catalog;

        }

        private void OnRecieveCatalogResponse(CatalogResponsePacket response)
        {
            if (response.ShopCatalog != null)
            {
                ShopCatalogGui = new GuiVinconShopCatalog("Shop Catalog", response.ShopCatalog, response.ShopList, _CoreClientAPI);

            }
            else
            {
                ShopCatalogGui = new GuiVinconCatalog("Shop Catalog", response.ShopList, _CoreClientAPI);
            }

            ShopCatalogGui.TryOpen();
        }

        public static void RegisterStallType(string className, Type type) {
            StallTypes.Add(className, type);
        }

        public static Type GetStallType(string className)
        {
            return StallTypes[className];
        }

        public IShopComponent GetShop(string ownerUID, long? parentID)
        {
            return CommerciallySystem.GetOwnable(ownerUID, parentID)?.GetComponent<IShopComponent>(); ;
        }

        public bool CanPurchaseItem(IPlayer player, IStallComponent bEShopBehavior, IShopComponent register, int stallSlot, int numPurchases)
        {
            return true;
        }
        
        private SortedList<int, PreProcessTrade> PreValidateTradeHandlers = new SortedList<int, PreProcessTrade>();
        public void RegisterPreValidateTradeHandler(int priority, PreProcessTrade hook)
        {
            PreValidateTradeHandlers.Add(priority, hook);
        }

        private SortedList<int, PostProcessTrade> PostValidateTradeHandlers = new SortedList<int, PostProcessTrade>();
        public void RegisterPostValidateTradeHandlers(int priority, PostProcessTrade hook)
        {
            PostValidateTradeHandlers.Add(priority, hook);
        }

        private SortedList<int, PreFinalizeTrade> PreProcessTradeHandlers = new SortedList<int, PreFinalizeTrade>();
        public void RegisterPreProcessTradeHandler(int priority, PreFinalizeTrade hook)
        {
            PreProcessTradeHandlers.Add(priority, hook);
        }

        private SortedList<int, PostFinalizeTrade> PostProcessTradeHandlers = new SortedList<int, PostFinalizeTrade>();

        public void RegisterPostProcessTradeHandler(int priority, PostFinalizeTrade hook)
        {
            PostProcessTradeHandlers.Add(priority, hook);
        }

        public ShopConfiguration GetShopConfiguration(long shopId)
        {
            if (ShopConfiguration.ContainsKey(shopId))
                return ShopConfiguration[shopId];
            return null;
        }

        public void UpdateStockForSlot(IStallComponent shop, int stallSlot, ItemStack product, int stockCount, ItemStack currency)
        {
            if (shop == null) return; //Unregistered Ownable. Shouldn't be possible to get here, but just in case
            this.Mod.Logger.Debug($"Got Stock Update: {shop.Ownable.Name} @ ({shop.GetPos().X} {shop.GetPos().Y} {shop.GetPos().Z}) - {stallSlot} = {stockCount}x {product} for {currency}");
            DB.SaveProductListing(shop, stallSlot, product, stockCount, currency);
        }

        public PurchaseResult TryPurchaseItem(PurchaseRequest request)
        {
            // Dev Note: I tried to make this as flexible as possible. If there are not enough "hook" spots, at the very least you can Harmony Patch the individual methods
            // Im hoping it won't come to that, and this should be enough for just about every use case I can think if, but if you need more then feel free to let me know!

            // Anything related to permissions should be in the Pre/Post Process step - some things I can think of are villages/cities where you need to be a member of that
            // village or city to be able to buy things from there, modifying the cost per purchase number to include taxes which will be sent to a city "vault" or something
            // in the PreFinalizeTrade hook before we send the rest to the Register, or if you wanted to have some sort of general whitelist/blacklist.

            // Anything related to recording sales like the Ledger system, modifying where payment/product goes, sending off ingame messages to the seller, or interacting with
            // an external API like the cross-server-trading server should go in the Pre/Post Finalize steps. At this point the trade is valid, the items have been removed from
            // the source slots and its just a matter of sending things where they should go

            // Step 1: Validate the trade by checking if we have enough currency, enough stock, permissions to trade, etc.
            PurchaseResult result = ValidateTrade(request);
            if (result.ErrorMsg != null) return result;

            // Step 2: At this point the trade is "Valid" and we can commit to the trade.
            // Extract all the items from the Source Slots into the TradeResult's aggregated item stacks
            ExtractItems(result);

            //Step 3: Log the sale to the ledger before the items are removed from the aggregates or processed by other mods
            LogPurchase(result);

            //Step 4: Now that we have taken the currency from the player, product from the shop, etc. we need to put the items in their proper places
            CommitTrade(result);

            return result;
        }

        public static PurchaseResult ValidateTrade(PurchaseRequest request, bool runProcessing = true)
        {
            PurchaseResult result = new PurchaseResult(request);

            //There must be a ICurrencySinkProvider somewhere... Either we set it to the parent entity, or itself.
            if (!request.IsAdminShop && request.StallSlot.GetCurrencySink(request) == null)
                return SetErrorAndReturn(result, TradingConstants.NOT_REGISTERED);

            if (request.CurrencyNeeded == null)
                return SetErrorAndReturn(result, TradingConstants.NO_PRICE);

            if (request.ProductNeeded == null)
                return SetErrorAndReturn(result, TradingConstants.NO_PRODUCT);

            if (!GenericPurchaseProcessor.HasEnoughStock(request))
                return SetErrorAndReturn(result, TradingConstants.NOT_ENOUGH_STOCK);

            if (!GenericPurchaseProcessor.CanPlayerAfford(request))
                return SetErrorAndReturn(result, TradingConstants.NOT_ENOUGH_MONEY);

            if (request.NumPurchases <= 0)
                return SetErrorAndReturn(result, TradingConstants.PURCHASED_ZERO);

            if (!GenericPurchaseProcessor.HasEnoughContainerCapacity(request))
                return SetErrorAndReturn(result, TradingConstants.NOT_ENOUGH_CAPACITY);

            if (!GenericPurchaseProcessor.HasEnoughDurability(request))
                return SetErrorAndReturn(result, TradingConstants.NO_TOOL);

            if (!GenericPurchaseProcessor.HasRequiredTradePass(request))
                return SetErrorAndReturn(result, TradingConstants.NO_PASS);

            if (!GenericPurchaseProcessor.CanFitPaymentIntoParent(request))
                return SetErrorAndReturn(result, TradingConstants.NO_REGISTER_SPACE);

            return result;
        }

        private void ExtractItems(PurchaseResult result)
        {
            PurchaseRequest req = result.Request;
            // Defer processing logic to the stall. This way I can abstract that mess between buying/selling.
            // Purchase Crates for instance deposit the purchased goods into the stall itself, and not the register.
            IStallSlot stall = req.StallSlot;


            /// Products
            int totalProductNeeded = req.GetFinalProductNeeded();
            if (stall is IContainedStallSlot container)
            {
                result.ProductStacks = container.ExtractProduct(totalProductNeeded, req.NumPurchases, req.ContainerSourceSlots, req.IsAdminShop);
            }
            else
            {
                result.ProductStacks = stall.ExtractProduct(totalProductNeeded, req.NumPurchases, req.IsAdminShop);
            }

            if (stall is ITooledStallSlot tooled)
            {
                tooled.ExtractDurability(req.NumPurchases, req.IsAdminShop);
            }

            /// Currency
            int totalCurrencyToMove = req.GetFinalCurrencyNeeded();
            AggregatedSlots currency = result.Request.CurrencySourceSlots;
            foreach (ItemSlot slot in currency)
            {
                ItemStack takenStack = slot.TakeOut(totalCurrencyToMove);
                if (takenStack != null)
                {
                    this.Mod.Logger.Debug($"Took out {takenStack.StackSize}x {takenStack} product from Currency Stacks");
                    totalCurrencyToMove -= takenStack.StackSize;
                    result.CurrencyStacks.Add(takenStack);
                    slot.MarkDirty();
                }

                if (totalCurrencyToMove <= 0)
                {
                    if (totalCurrencyToMove < 0)
                    {
                        this.Mod.Logger.Error($"Somehow removed {Math.Abs(totalCurrencyToMove)} extra items from Currency");
                    }
                    break;
                }

            }

            /// Coupons
            ItemSlot coupons = result.Request.CouponSourceSlots;
            if (coupons != null)
            {
                int totalCouponsToMove = result.Request.NumPurchases;
                AggregatedStacks couponStacks = result.CouponStacks;

                ItemStack takenStack = coupons.TakeOut(totalCouponsToMove);
                if (takenStack != null)
                {
                    this.Mod.Logger.Debug($"Took out {takenStack.StackSize}x {takenStack} product from Coupon Stacks");
                    totalCouponsToMove -= takenStack.StackSize;
                    couponStacks.Add(takenStack);
                    coupons.MarkDirty();
                }

                if (totalCouponsToMove != 0)
                {
                    if (totalCouponsToMove < 0)
                    {
                        this.Mod.Logger.Error($"Somehow removed {Math.Abs(totalCouponsToMove)} extra items from Coupons");
                    }
                    else
                    {
                        this.Mod.Logger.Error($"Somehow missing {totalCouponsToMove} items from Coupons");
                    }
                }

            }
            result.Request.SellingEntity.GetBlockEntity().MarkDirty();
        }

        private void LogPurchase(PurchaseResult result)
        {
            DB.SavePurchase(result);
        }

        private void CommitTrade(PurchaseResult result)
        {
            CommerciallyModSystem.PrintClientMessage(result.Request.Customer, TradingConstants.PURCHASED_ITEMS, new object[] {
                result.TotalProductAmount,
                result.Request.ProductNeeded.GetName(),
                result.TotalCurrencyAmount,
                result.Request.CurrencyNeeded.GetName()
            });

            //PreProcessTrade(result);
            ProcessTrade(result);
            //PostProcessTrade(result);
        }

        private void ProcessTrade(PurchaseResult result)
        {
            /// Give Player Product
            AssetLocation sound = null;
            AggregatedStacks products = result.ProductStacks;
            IPlayer player = result.Request.Customer;
            while (products.CanRemoveStack())
            {
                ItemStack stack = products.RemoveStack();

                if (stack != null)
                {
                    this.Mod.Logger.Debug($"Adding {stack.StackSize}x {stack} product to Parent");
                    if (stack.Block?.Sounds?.Place.Location != null)
                    {
                        sound = stack.Block?.Sounds?.Place.Location;
                    }

                    player.InventoryManager.TryGiveItemstack(stack, true);
                    if (stack.StackSize > 0)
                    {
                        result.Request.Api.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ.Add(0.5), null);
                    }
                }
            }
            result.Request.Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), player.Entity, player, true, 16f, 1f);


            /// Give Ownable Currency
            ICurrencySinkProvider currencyProvider = result.Request.StallSlot.GetCurrencySink(result.Request);
            VinconBaseInventory vinconInv = currencyProvider?.Inventory as VinconBaseInventory;
            bool shouldAddToParent = vinconInv == null || vinconInv.DiscardCurrency == false;
            if (currencyProvider != null)
            {
                ItemSlot[] slots = currencyProvider.CurrencySlots;
                while (result.CurrencyStacks.CanRemoveStack())
                {
                    ItemStack nextStack = result.CurrencyStacks.RemoveStack();
                    if (shouldAddToParent) {
                        this.Mod.Logger.Debug($"Adding {nextStack.StackSize}x {nextStack} currency to Parent");
                        AddItemToSlots(result.Request.Api, nextStack, slots);
                    }
                    
                }
                currencyProvider.GetBlockEntity().MarkDirty();
            } else if (!result.Request.IsAdminShop){
                ItemStack nextStack = result.CurrencyStacks.RemoveStack();
                this.Mod.Logger.Error($"Error moving {nextStack.StackSize}x {nextStack} currency to Parent Currency Provider (Null)");
            }

            /// Give Ownable Coupons
            ICouponSinkProvider couponProvider = result.Request.StallSlot.GetCouponSink(result.Request);
            if (couponProvider != null)
            {
                ItemSlot[] slots = couponProvider.CouponSlots;
                while (result.CouponStacks.CanRemoveStack())
                {
                    ItemStack nextStack = result.CouponStacks.RemoveStack();
                    if (shouldAddToParent)
                    {
                        this.Mod.Logger.Debug($"Adding {nextStack.StackSize}x {nextStack} currency to Parent");
                        AddItemToSlots(result.Request.Api, nextStack, slots);
                    }
                }
                couponProvider.GetBlockEntity().MarkDirty();
            }
            else if (!result.Request.IsAdminShop)
            {
                ItemStack nextStack = result.CurrencyStacks.RemoveStack();
                this.Mod.Logger.Error($"Error moving {nextStack.StackSize}x {nextStack} currency to Parent Coupon Provider (Null)");
            }
        }

        public static PurchaseResult SetErrorAndReturn(PurchaseResult result, string error)
        {
            result.ErrorMsg = error;
            result.Request.NumPurchases = 0;
            return result;
        }

        public static bool AddItemToSlots(ICoreAPI api, ItemStack stack, ItemSlot[] slots)
        {
            if (stack == null || stack.StackSize == 0) return false;

            ItemSlot dslot = new ItemSlot(null);
            dslot.Itemstack = stack;

            int amountLeft = stack.StackSize;

            foreach (var slot in slots)
            {
                if (!slot.CanHold(dslot)) continue;

                amountLeft -= dslot.TryPutInto(api.World, slot, amountLeft);
                slot.MarkDirty();

                if (amountLeft <= 0) break;
            }

            return amountLeft <= 0;
        }

        public void UpdateShopConfiguration(long id, string description, string shortDescription, string webhook)
        {
            ShopConfiguration config = GetShopConfiguration(id);
            if (config == null)
            {
                config = new ShopConfiguration()
                {
                    Id = id,
                    Description = description,
                    ShortDescription = shortDescription,
                    WebHook = webhook
                };
                ShopConfiguration[id] = config;
            } else
            {
                config.Description = description;
                config.ShortDescription = shortDescription;
                config.WebHook = webhook;
            }

            DB.SaveShopConfiguration(config);

        }

       
    }    
}