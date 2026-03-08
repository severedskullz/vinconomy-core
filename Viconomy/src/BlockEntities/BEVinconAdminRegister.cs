using System.Collections.Generic;
using System.IO;
using Viconomy.Registry;
using Vinconomy.BlockEntities.TextureSwappable;
using Vinconomy.GUI;
using Vinconomy.Inventory.Impl;
using Vinconomy.Registry;
using Vinconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vinconomy.BlockEntities
{
    public class BEVinconAdminRegister : BETextureSwappableBlockContainer, IShopRoot, IInteractable
    {
        public const byte TYPE_CURRENCY = 0;
        public const byte TYPE_PRODUCT = 1;

        private VinconGenericInventory inventory;
        private GuiVinconAdminRegister invDialog;
        private VinconomyCoreSystem modSystem;
        public int ID { get; internal set; } = -1;
        public string Owner { get; internal set; }
        public string OwnerName { get; internal set; }

        public override InventoryBase Inventory => inventory;

        public override string InventoryClassName => "ViconomyRegisteryInventory";


        public BEVinconAdminRegister()
        {
            this.inventory = new VinconGenericInventory(1, null, this.Api, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
        }

        public void UpdateShop(string Owner, string OwnerName, int ID, string Name)
        {          
            if (Api.Side == EnumAppSide.Server)
            {
                if (ID <= 0)
                {
                    //Console.WriteLine("Register block was placed and did not have ID Set...");
                    ShopRegistration register = modSystem.AddShop(Owner, OwnerName, OwnerName + "'s Shop", this.Pos);

                    ID = register.ID;
                }
                else
                {
                    ShopRegistration reg = modSystem.GetRegistry().GetShop(ID);
                    if (reg != null)
                    {
                        //Console.WriteLine("Register block was placed and had ID Set... Updating");
                        ShopRegistration register = modSystem.UpdateShop(Owner, ID, Name, this.Pos);
                    } else {
                        modSystem.Mod.Logger.Warning("Somehow the shop with ID " + ID + " got removed. Recreating it...");
                        ShopRegistration register = modSystem.AddShop(Owner, OwnerName, Name, this.Pos, ID);
                        ID = register.ID; //Note: This *should* be redundant, but leaving in for a sanity check.
                    }
                    
                }
            }

            this.ID = ID;
            this.Owner = Owner;
            this.OwnerName = OwnerName;
            
        }

        public void UpdateShopConfiguration(string desc, string shortDesc, string webHook)
        {
            modSystem.UpdateShopConfig(ID, desc, shortDesc, webHook);  
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            modSystem.GetRegistry().ClearShopPos(ID);
        }

        public bool CanAccess(IPlayer player)
        {
            if (player.PlayerUID == this.Owner)
                return true;

            ShopRegistration reg = modSystem.GetRegistry().GetShop(ID);
            if (reg == null)
            {
                modSystem.Mod.Logger.Error("Couldnt find shop registration for register " + ID);
                return false;
            }
            return reg.CanAccess(player);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            ID = tree.GetInt("ID");
            Owner = tree.GetString("Owner");
            OwnerName = tree.GetString("OwnerName");

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetInt("ID", ID);
            tree.SetString("Owner", Owner);
            tree.SetString("OwnerName", OwnerName);
        }

        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            bool shiftMod = byPlayer.Entity.Controls.Sneak;

            ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (shiftMod && (handSlot.Itemstack?.Item?.Code.ToString() == "vinconomy:ledger" || handSlot.Itemstack?.Item?.Code.ToString() == "vinconomy:catalog"))
            {
                if (Api.Side == EnumAppSide.Server)
                {
                    IServerPlayer player = ((IServerPlayer)byPlayer);
                    if (CanAccess(player))
                    {
                        if (handSlot.Itemstack.Attributes.GetInt("ShopId", -1) <= 0)
                        {
                            VinconomyCoreSystem modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
                            ShopRegistration shop = modSystem.GetRegistry().GetShop(ID);
                            player.SendMessage(0, Lang.Get("vinconomy:ledger-set", [shop.Name]), EnumChatType.OwnMessage);
                            handSlot.Itemstack.Attributes.SetInt("ShopId", ID);
                            handSlot.Itemstack.Attributes.SetString("Owner", Owner);
                            handSlot.Itemstack.Attributes.SetString("ShopName", shop.Name);
                            handSlot.MarkDirty();

                        }
                        else
                        {
                            player.SendMessage(0, Lang.Get("vinconomy:ledger-already-set", []), EnumChatType.OwnMessage);
                        }
                    }
                    else
                    {
                        player.SendMessage(0, Lang.Get("vinconomy:doesnt-own", []), EnumChatType.OwnMessage);
                    }
                }
            }
            else if (CanAccess(byPlayer))
            {
                // Open shop admin gui
                OpenAdminForPlayer(byPlayer);
            }
            else
            {
                if (Api.Side == EnumAppSide.Server)
                {
                    ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("vinconomy:doesnt-own", []), EnumChatType.OwnMessage);
                }
               
            }

            return true;
        }

        private void OpenAdminForPlayer(IPlayer byPlayer)
        {
            if (this.Api.World is IServerWorldAccessor)
            {
                ShopRegistration register = modSystem.GetRegistry().GetShop(ID);
                //((ServerMain)Api.World).PlayerDataManager.GetPlayerDataByLastKnownName();

                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    if (register != null && register.Name != null)
                    {
                        writer.Write(register.Name);
                    } else if (OwnerName != null) {
                         writer.Write(Lang.Get("vinconomy:gui-shop-owner", [ OwnerName ]));
                    } else {
                        writer.Write(Lang.Get("vinconomy:gui-shop-unowned"));    
                    }

                    TreeAttribute tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
              ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos, VinConstants.TOGGLE_GUI, data);
                byPlayer.InventoryManager.OpenInventory(this.inventory);
            }
        }


        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedClientPacket " + packetid);
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            switch (packetid)
            {
                case VinConstants.OPEN_GUI:

                    //Todo: Check if owner before we open the inventory?
                    if (inventoryManager == null)
                    {
                        return;
                    }
                    inventoryManager.OpenInventory(this.Inventory);
                    break;

                case VinConstants.CLOSE_GUI:
                    //Todo: Check if owner before we open the inventory?
                    if (inventoryManager != null)
                    {
                        inventoryManager.CloseInventory(this.Inventory);
                    }
                    break;
                case VinConstants.SET_SHOP_NAME:
                    using (MemoryStream ms = new MemoryStream(data))
                    {

                        if (CanAccess(player))
                        {
                            BinaryReader reader = new BinaryReader(ms);
                            string name = reader.ReadString();
                            string description = reader.ReadString();
                            string shortDescription = reader.ReadString();
                            string webhook = reader.ReadString();

                            shortDescription = shortDescription.Replace("\r\n", "\n").Replace('\n', ' ');
                            //description = description.Replace("\r\n", "\n").Replace('\n', ' ');
                            UpdateShop(Owner, OwnerName, ID, name);
                            modSystem.UpdateShopConfig(ID, description, shortDescription, webhook);
                        }
                        else
                        {
                            ((IServerPlayer)player).SendMessage(0, Lang.Get("viconomy:doesnt-own", []), EnumChatType.OwnMessage);
                        }
                    }
                    break;
                case VinConstants.SET_WAYPOINT:
                    using (MemoryStream ms = new MemoryStream(data))
                    {

                        if (CanAccess(player))
                        {
                            BinaryReader reader = new BinaryReader(ms);
                            bool enabled = reader.ReadBoolean();
                            string icon = reader.ReadString();
                            int color = reader.ReadInt32();
                            modSystem.UpdateShopWaypoint(ID, enabled, icon, color);
                        }
                        else
                        {
                            ((IServerPlayer)player).SendMessage(0, Lang.Get("viconomy:doesnt-own", []), EnumChatType.OwnMessage);
                        }
                    }
                    break;
                case VinConstants.SEARCH_ITEM:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        if (player.PlayerUID == Owner)
                        {
                            BinaryReader reader = new BinaryReader(ms);
                            string itemCode = reader.ReadString();
                            int itemType = reader.ReadInt32(); // 0 Currency, 1 Product

                            if (itemType == TYPE_CURRENCY)
                            {
                                SendClientSearchItemInfo(player, modSystem.DB.GetCurrencyDefinitionResults(this.ID, itemCode), TYPE_CURRENCY, itemCode != "");
                            } else
                            {
                                SendClientSearchItemInfo(player, modSystem.DB.GetProductDefinitionResults(this.ID, itemCode), TYPE_PRODUCT, itemCode != "");
                            }
                        }
                    }
                    break;
                case VinConstants.LOAD_ITEM:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        if (CanAccess(player))
                        {
                            BinaryReader reader = new BinaryReader(ms);
                            int itemType = reader.ReadInt32(); // 0 Currency, 1 Product
                            int itemId = reader.ReadInt32();

                            if (itemType == TYPE_CURRENCY)
                            {
                                SendClientItemInfo(player, modSystem.DB.GetCurrencyDefinition(itemId, this.ID));
                            }
                            else
                            {
                                SendClientItemInfo(player, modSystem.DB.GetProductDefinition(itemId, this.ID));
                            }
                        }
                    }
                    break;

                    //TODO: This is VERY network heavy for what it is that we are doing, especially since we are doing this for each change. Figure out a better way later on.
                case VinConstants.SAVE_ITEM:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        if (CanAccess(player))
                        {
                            BinaryReader reader = new BinaryReader(ms);
                            int itemType = reader.ReadByte(); // 0 Currency, 1 Product
                            int length = reader.ReadInt32();
                            if (itemType == TYPE_CURRENCY)
                            {
                                CurrencyDefinition def = SerializerUtil.Deserialize<CurrencyDefinition>(reader.ReadBytes(length));
                                modSystem.DB.CreateOrUpdateCurrencyDefinition(def);
                            }
                            else
                            {
                                ProductDefinition def = SerializerUtil.Deserialize<ProductDefinition>(reader.ReadBytes(length));
                                modSystem.DB.CreateOrUpdateProductDefinition(def);
                            }
                        }
                    }
                    break;
                case VinConstants.CREATE_ITEM:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        if (CanAccess(player))
                        {
                            EntryResultDefinition def = SerializerUtil.Deserialize<EntryResultDefinition>(data);

                            if (def.Type == TYPE_CURRENCY)
                            {
                                CurrencyDefinition curDef = new CurrencyDefinition();
                                curDef.ShopId = def.ShopId;
                                curDef.CurrencyCode = def.Code;
                                curDef.CurrencyAttributes = def.Attributes;
                                curDef.Supply = 0;
 
                                SendClientItemInfo(player, modSystem.DB.CreateOrUpdateCurrencyDefinition(curDef));
                            }
                            else
                            {
                                ProductDefinition prodDef = new ProductDefinition();
                                prodDef.ShopId = def.ShopId;
                                prodDef.ProductCode = def.Code;
                                prodDef.ProductQuantity = 1;
                                prodDef.ProductAttributes = def.Attributes;
                                prodDef.CurrencyCode = def.Code;
                                prodDef.CurrencyQuantity = 1;
                                prodDef.CurrencyAttributes = def.Attributes;
                                prodDef.Supply = 0;
                                SendClientItemInfo(player, modSystem.DB.CreateOrUpdateProductDefinition(prodDef));
                            }
                        }
                    }
                    break;

                default:
                    if (packetid < 1000)
                    {
                        if (!CanAccess(player))
                        {
                            if (!((ICoreServerAPI)Api).Server.IsDedicated)
                            {
                                VinconomyCoreSystem.PrintClientMessage(player, "Nice Try, but that isn't yours... If this wasn't singleplayer, you would have been kicked.", new object[] { });
                            }
                            else
                            {
                                ((IServerPlayer)player).Disconnect("Nice try, but that wasn't yours. (Tried to access Register they didn't own)");
                            }
                            return;
                        }

                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
                        return;
                    }
                    break;
            }
        }


        private void SendClientSearchItemInfo(IPlayer player, List<EntryResultDefinition> defs, int type, bool isPartial = false)
        {
            using (MemoryStream mos = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(mos);
                writer.Write((byte)type);
                writer.Write(isPartial);
                byte[] data = SerializerUtil.Serialize(defs);
                writer.Write(data.Length);
                writer.Write(data);
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)player, this.Pos, VinConstants.SEARCH_ITEM, mos.ToArray());
            }
        }


        private void SendClientItemInfo(IPlayer player, CurrencyDefinition def)
        {
            using (MemoryStream mos = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(mos);
                writer.Write(TYPE_CURRENCY);
                byte[] data = SerializerUtil.Serialize(def);
                writer.Write(data);
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)player, this.Pos, VinConstants.LOAD_ITEM, mos.ToArray());
            }
        }

        private void SendClientItemInfo(IPlayer player,ProductDefinition def)
        {
            using (MemoryStream mos = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(mos);
                writer.Write(TYPE_PRODUCT);
                byte[] data = SerializerUtil.Serialize(def);
                writer.Write(data);
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)player, this.Pos, VinConstants.LOAD_ITEM, mos.ToArray());
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedServerPacket " + packetid);
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)this.Api.World;
            switch(packetid)
            {
                case VinConstants.TOGGLE_GUI:
                    if (this.invDialog != null)
                    {
                        CloseGui(clientWorld);
                    }
                    else
                    {
                        OpenGui(data);
                    }
                    break;
                case VinConstants.OPEN_GUI:
                    OpenGui(data);
                    break;
                case VinConstants.CLOSE_GUI:
                    CloseGui(clientWorld);
                    break;
                case VinConstants.LOAD_ITEM:
                    LoadItemInfo(data);
                    break;
                case VinConstants.SEARCH_ITEM:
                    OnClientSearchItemInfo(data);
                    break;
                default: 
                    break;
            }
 
        }

        private void LoadItemInfo(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                int type = reader.ReadByte();
                if (type == TYPE_CURRENCY)
                {
                    CurrencyDefinition currency = SerializerUtil.Deserialize<CurrencyDefinition>(reader.ReadBytes(data.Length - 1));
                    invDialog.OnLoadCurrency(currency);
                } else
                {
                    ProductDefinition product = SerializerUtil.Deserialize<ProductDefinition>(reader.ReadBytes(data.Length - 1));
                    invDialog.OnLoadProduct(product);
                }
            }
        }

        private void OnClientSearchItemInfo(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                int type = reader.ReadByte(); //TODO: Is this actually needed anymore?
                bool isPartial = reader.ReadBoolean();
                int length = reader.ReadInt32();

                List<EntryResultDefinition> currency = SerializerUtil.Deserialize<List<EntryResultDefinition>>(reader.ReadBytes(length));
                invDialog.OnSearchResults(type, currency, isPartial);
               
            }
        }

        private void OpenGui(byte[] data)
        {
            TreeAttribute tree = new TreeAttribute();
            string dialogTitle;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                dialogTitle = reader.ReadString();
                tree.FromBytes(reader);
            }
            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            ShopRegistration shopRegistry = modSystem.GetRegistry().GetShop(ID);

            if (shopRegistry == null)
            {
                ((ICoreClientAPI)Api).World.Player.ShowChatNotification($"Could not find client-side registry info for this shop ID {ID}. Ensure you have access to this register and ensure the shop exists in the Vinconomy database. (Failsafe)");
                return;
            }
              

            this.invDialog = new GuiVinconAdminRegister(shopRegistry, inventory, this.Pos, this.Api as ICoreClientAPI);
            this.invDialog.OpenSound = new SoundAttributes("sounds/effect/cashregister", true);
            // this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;

                ((ClientCoreAPI) Api).Network.SendBlockEntityPacket(this.Pos, VinConstants.CLOSE_GUI, null);
            };

        }

        private void CloseGui(IClientWorldAccessor clientWorld)
        {
            clientWorld.Player.InventoryManager.CloseInventory(this.Inventory);

            if (this.invDialog != null)
            {
                if (this.invDialog.IsOpened())
                {
                    this.invDialog.TryClose();
                }

            }

            if (this.invDialog != null)
            {
                this.invDialog.Dispose();
            }

            this.invDialog = null;
        }

        public void SetOwner(IPlayer player)
        {
            Owner = player.PlayerUID;
            OwnerName = player.PlayerName;
        }
        public void SetOwner(string playerUUID, string playerName)
        {
            Owner = playerUUID;
            OwnerName = playerName;
        }
    }

}