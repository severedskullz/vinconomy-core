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
using Cairo;
using System.Collections.Generic;
using Viconomy.Renderer;
using Viconomy.src.Renderer;
using Viconomy.ItemTypes;
using Vintagestory;

namespace Viconomy
{
    using RayTraceResults = Tuple<BlockSelection, EntitySelection>;

    public class ViconomyCore : ModSystem
    {
        private ICoreServerAPI _coreServerAPI;
        private ICoreClientAPI _coreClientAPI;

        private IClientNetworkChannel _clientChannel;
        private IServerNetworkChannel _serverChannel;

        public ShopRegistry registers = new ShopRegistry();
        private Dictionary<EnumItemClass, List<IItemRenderer>> renderers;
        private bool isAddingRegisters;

        public override double ExecuteOrder() => 1;

        public ViconConfig Config { get; internal set; }

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
                .RegisterMessageType(typeof(RegistryUpdatePacket));

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
            api.Event.GameWorldSave += OnSaveGameSaving;
            api.Event.PlayerJoin += SendRegisterUpdate;

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

        }


        public override void StartClientSide(ICoreClientAPI api)
        {
            _coreClientAPI = api;
            _clientChannel = api.Network.GetChannel("Vinconomy");
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<RegistryUpdatePacket>(this.OnRecieveRegistryUpdate));
            this.registers = new ShopRegistry();

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
        }



        private void BeginRendererRegistration()
        {
            isAddingRegisters = true;
        }

        private void RegisterRenderer(IItemRenderer blockRenderer)
        {
            renderers[blockRenderer.getRendererClass()].Add(blockRenderer);
            if (!isAddingRegisters)
            {
                renderers[blockRenderer.getRendererClass()].Sort((i1, i2) => { return i2.getPriority().CompareTo(i1.getPriority()); });
            }
        }
        private void EndRendererRegistration()
        {
            isAddingRegisters = false;
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
            BlockPos pos = new BlockPos((int)args[1] + (_coreServerAPI.WorldManager.MapSizeX / 2), (int)args[2], (int)args[3] + (_coreServerAPI.WorldManager.MapSizeZ / 2));

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
                registers.ClearRegister(register.Owner, register.ID);
                register.Owner = playerUUID;
                UpdateRegister(playerUUID, register.ID, playerData.LastKnownPlayername + "'s Shop", register.Pos);
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

        private void OnRecieveRegistryUpdate(RegistryUpdatePacket packet)
        {
            this.registers = new ShopRegistry();
            if (packet.registry != null) {
                foreach (RegistryUpdate item in packet.registry)
                {
                    //TODO: Figure out how to get local player.
                    this.registers.AddRegister(new ViconRegister() { Name = item.Name, ID = item.ID , Owner = item.Owner});
                }
            }            
        }

        public BEVRegister GetShopRegister(string owner, string registerID)
        {
            if (registerID == null || owner == null)
            {
                return null;
            }

            ViconRegister register = registers.GetRegister(owner, registerID);
            if (register == null || register.Position == null) { return null; }

            BEVRegister viconRegister = _coreServerAPI.World.BlockAccessor.GetBlockEntity(register.Position) as BEVRegister;
            if (viconRegister != null)
            {
                return viconRegister;
            } else
            {
                registers.ClearRegister(owner, registerID);
            }
            return null;
        }

        public ShopRegistry GetRegistry()
        {
            return registers;
        }

        private void OnSaveGameLoading()
        {
            this._coreServerAPI.Logger.Debug("=============== Loading Viconomy ===============");
            registers = _coreServerAPI.WorldManager.SaveGame.GetData("vinconomy:registers", new ShopRegistry());
            if (registers != null)
            {
                this._coreServerAPI.Logger.Debug("= Loaded " + registers.GetCount() + " registers");

                foreach (string ownerId in registers.registers.Keys)
                {
                    var shops = registers.registers[ownerId];

                    //Shops can be null if they didnt have any registers place and we deserialized the Dictionary with nothing in it.
                    if (shops == null) continue;

                    this._coreServerAPI.Logger.Debug("== Loading " + shops.Values.Count + " registers for Owner: " + ownerId);
                    foreach (string shopId in shops.Keys)
                    {
                        var shop = registers.registers[ownerId][shopId];
                        this._coreServerAPI.Logger.Debug("=== Loading shop " + shop.Name);

                        if (shop.Position == null)
                        {
                            registers.registers[ownerId].Remove(shopId);
                            this._coreServerAPI.Logger.Debug("==== Shop " + shop.Name + " (" + shop.ID + ") does not exist anymore. Removing...");
                        }
                    }
                }
            } else
            {
                this._coreServerAPI.Logger.Debug("= Could not load Viconomy savegame data. Recreating Shop Registry. Shops will need to be remade and reinitialized");
                registers = new ShopRegistry();
            }
           
           
            this._coreServerAPI.Logger.Debug("=============== Loaded Viconomy ================");
        }

        private void OnSaveGameSaving()
        {
            _coreServerAPI.WorldManager.SaveGame.StoreData<ShopRegistry>("vinconomy:registers", registers);
        }

        private void SendRegisterUpdate(IServerPlayer player)
        {
            ViconRegister[] shops = this.registers.GetRegistersForOwner(player.PlayerUID);
            RegistryUpdate[] updates = new RegistryUpdate[0];
            if (shops != null)
            {
                updates = new RegistryUpdate[shops.Length];

                for (int i = 0; i < shops.Length; i++)
                {
                    updates[i] = new RegistryUpdate(shops[i].Owner, shops[i].ID, shops[i].Name);
                }
            }
            

            _serverChannel.SendPacket(new RegistryUpdatePacket(updates), new IServerPlayer[]{ player });
        }

        public ViconRegister AddRegister(string owner, string ownerName, string name, BlockPos pos)
        {
            ViconRegister reg = this.GetRegistry().AddRegister(owner, ownerName, name, pos);
            if (_coreServerAPI != null)
            {
                IServerPlayer player = (IServerPlayer)_coreServerAPI.World.PlayerByUid(owner);
                if (player.ConnectionState == EnumClientState.Playing)
                {
                    SendRegisterUpdate(player);
                }
            }
           
            return reg;
        }

        public ViconRegister UpdateRegister(string owner, string iD, string name, BlockPos pos)
        {
            ViconRegister reg = registers.UpdateRegister(owner, iD, name, pos);
            if (_coreServerAPI != null)
            {
                IServerPlayer player = (IServerPlayer)_coreServerAPI.World.PlayerByUid(owner);
                if (player != null && player.ConnectionState == EnumClientState.Playing)
                {
                    SendRegisterUpdate(player);
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
                ((IServerPlayer)player).SendMessage(0, Lang.Get(message, args), EnumChatType.OwnMessage, null);
            }
            else
            {
                ((IClientPlayer)player).ShowChatNotification(Lang.Get(message, args));
            }
        }

        #region Event Delegates

        /// <summary>
        /// Called when an item is purchased. <br/><br/>
        /// player: The player making the purchase<br/>
        /// register: The register that payment is meant to go to<br/>
        /// stallSlot: the stall slot the player is purchasing from<br/>
        /// product: The (clone) stack of items to be transfered to the player - Do not modify this<br/>
        /// payment: the stack of items representing payment to be stored in the Register<br/>
        /// numSales: How many sales are in this transaction
        /// </summary>
        public void PurchasedItem(IPlayer player, BEViconBase stall, BEVRegister register, ItemStack product, ItemStack payment)
        {
            OnPurchasedItem?.Invoke(player, stall, register, product, payment);
        }
        public event OnPurchasedItemDelegate OnPurchasedItem;



        /// <summary>
        /// Called befire an item is purchased to determine if the player is allowed to purchase this item. Return true if it should allow the player to purchase the item <br/><br/>
        /// player: The player making the purchase<br/>
        /// register: The register that payment is meant to go to<br/>
        /// stallSlot: the stall slot the player is purchasing from<br/>
        /// product: the item slot which is going to be used to purchase from<br/>
        /// payment: the stack of items representing payment the player will owe<br/>
        /// desiredAmount: How many sales are in this transaction
        /// </summary>
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
                result = OnTryPlaceBlock.Invoke(world, byPlayer,itemstack, blockSel);
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
                    block = api.World.BlockAccessor.GetBlock(blockSelection.Position.X, blockSelection.Position.Y, blockSelection.Position.Z);
                }
                if (block is BlockVContainer || block is BlockVClothingDisplay || block is BlockVClothingDisplayTop)   
                    return EnumWorldAccessResponse.Granted;
            }
            return response;
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

        public event OnTestAccessDelegate OnTestAccess;

        #endregion

    }
}
