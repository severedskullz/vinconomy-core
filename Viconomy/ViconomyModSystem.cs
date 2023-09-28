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
using Microsoft.Win32;
using Viconomy.Inventory;
using Vintagestory.GameContent;
using System.Numerics;

namespace Viconomy
{
    public class ViconomyModSystem : ModSystem
    {
        private ICoreServerAPI _coreServerAPI;
        private ICoreClientAPI _coreClientAPI;

        private IClientNetworkChannel _clientChannel;
        private IServerNetworkChannel _serverChannel;

        public ShopRegistry registers = new ShopRegistry(); 

        public ViconConfig Config { get; internal set; }

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
 
            api.RegisterBlockClass("ViconContainer", typeof(BlockVContainer));
            api.RegisterBlockClass("ViconRegister", typeof(BlockVRegister));
            
            api.RegisterBlockEntityClass("BEViconStall", typeof(BEViconStall));
            api.RegisterBlockEntityClass("BEViconHelmetStand", typeof(BEViconHelmetStand));
            api.RegisterBlockEntityClass("BEVRegister", typeof(BEVRegister));
            api.RegisterBlockEntityClass("BEViconShelf", typeof(BEViconShelf));

            api.Network.RegisterChannel("Viconomy")
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
            _serverChannel = api.Network.GetChannel("Viconomy");
                       
            api.Event.SaveGameLoaded += OnSaveGameLoading;
            api.Event.GameWorldSave += OnSaveGameSaving;
            api.Event.PlayerJoin += SendRegisterUpdate;

            this.OnPurchasedItem += TEST_OnPurchasedItem;
            this.OnCanPurchaseItem += TEST_OnCanPurchaseItem;
            this.OnBlockBroken += TEST_OnBlockBroken;
            this.OnBlockPlaced += TEST_OnBlockPlaced;
            this.OnTryPlaceBlock += TEST_OnTryBlockPlaced;
        }

        private bool TEST_OnTryBlockPlaced(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel)
        {
                PrintClientMessage(byPlayer, "TRY BLOCK PLACED DELEGATE RAN");
                return true;
        }

        private void TEST_OnBlockPlaced(AssetLocation code, IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack)
        {
            
                this._coreServerAPI.BroadcastMessageToAllGroups(code.GetName() + ": ON BLOCK PLACED DELEGATE RAN", EnumChatType.OwnMessage);
            
        }

        private bool TEST_OnBlockBroken(AssetLocation code, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier)
        {
            PrintClientMessage(byPlayer, code.GetName() + ": ON BLOCK BROKEN DELEGATE RAN");
            return true;
        }

        private bool TEST_OnCanPurchaseItem(IPlayer player, BEViconStall stall, BEVRegister register, ItemSlot product, ItemSlot payment)
        {
            {
                PrintClientMessage(player, "CAN PURCHASE DELEGATE RAN");
                return true;
            }
        }

        private bool TEST_OnPurchasedItem(IPlayer player, BEViconStall stall, BEVRegister register, ItemStack product, ItemStack payment)
        {
            {
                PrintClientMessage(player, "PURCHASE DELEGATE RAN");
                return true;
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            _coreClientAPI = api;
            _clientChannel = api.Network.GetChannel("Viconomy");
            _clientChannel.SetMessageHandler(new NetworkServerMessageHandler<RegistryUpdatePacket>(this.OnRecieveRegistryUpdate));
            this.registers = new ShopRegistry();
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
            this._coreServerAPI.Logger.Debug("= Loaded " + registers.GetCount() + " registers");

            foreach (string ownerId in registers.registers.Keys)
            {
                var shops = registers.registers[ownerId];
                this._coreServerAPI.Logger.Debug("== Loading " + shops.Values.Count + " registers for Owner: " + ownerId);
                foreach (string shopId in shops.Keys)
                {
                    var shop = registers.registers[ownerId][shopId];
                    this._coreServerAPI.Logger.Debug("=== Loading shop " + shop.Name);

                    if (shop.Position == null)
                    {
                        registers.registers[ownerId].Remove(shopId);
                        this._coreServerAPI.Logger.Debug("==== Shop " + shop.Name + " (" + shop.ID +") does not exist anymore. Removing...");
                    }
                }
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
            

            _serverChannel.SendPacket<RegistryUpdatePacket>(new RegistryUpdatePacket(updates), new IServerPlayer[]{ player });
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
                if (player.ConnectionState == EnumClientState.Playing)
                {
                    SendRegisterUpdate(player);
                }
            }
            return reg;
        }

        public static void PrintClientMessage(IPlayer player, string message, object[] args = null)
        {
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
        /// product: The stack of items to be transfered to the player<br/>
        /// payment: the stack of items representing payment to be stored in the Register<br/>
        /// numSales: How many sales are in this transaction
        /// </summary>
        public void PurchasedItem(IPlayer player, BEViconStall stall, BEVRegister register, ItemStack product, ItemStack payment)
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
        public bool CanPurchaseItem(IPlayer player, BEViconStall stall, BEVRegister register, int stallSlot, ItemSlot purchaseSlot, ItemSlot currencySlot, int desiredAmount)
        {
            bool result = true;
            if (OnCanPurchaseItem != null)
            {
                result = OnCanPurchaseItem.Invoke(player, stall, register, purchaseSlot, currencySlot);
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

        #endregion

    }
}
