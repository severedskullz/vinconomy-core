using Microsoft.Win32;
using System;
using System.IO;
using System.Numerics;
using Viconomy.BlockTypes;
using Viconomy.Delegates;
using Viconomy.GUI;
using Viconomy.Inventory;
using Viconomy.ItemTypes;
using Viconomy.Registry;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEViconTeller : BlockEntityContainer
    {
        private BlockVTeller block;
        private ViconomyTellerInventory inventory;
        private GuiViconTeller invDialog;
        internal string RegisterID;
        internal bool isAdminShop;

        public string ID { get; internal set; }
        public string Owner { get; internal set; }
        public string OwnerName { get; internal set; }

        public override InventoryBase Inventory => inventory;

        public override string InventoryClassName => "ViconomyRegisteryInventory";

        public BEViconTeller()
        {
            this.inventory = new ViconomyTellerInventory(this, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            this.block = (BlockVTeller) api.World.BlockAccessor.GetBlock(this.Pos);

            base.Initialize(api);          
                       
        }

        public void UpdateTeller(string Owner, string OwnerName, string ID, string Name)
        {
            ViconomyCore modSystem = this.Api.ModLoader.GetModSystem<ViconomyCore>();
            
            /*
            if (ID == null)
            {
                //Console.WriteLine("Register block was placed and did not have ID Set...");
                ViconRegister register = modSystem.AddRegister(Owner, OwnerName, OwnerName + "'s Shop", this.Pos);

                ID = register.ID;
            }
            else
            {
                //Console.WriteLine("Register block was placed and had ID Set... Updating");
                ViconRegister register = modSystem.UpdateRegister(Owner, ID, Name, this.Pos);
            }
            */

            this.ID = ID;
            this.Owner = Owner;
            this.OwnerName = OwnerName;
            
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            ViconomyCore modSystem = this.Api.ModLoader.GetModSystem<ViconomyCore>();
            //modSystem.registers.ClearRegisterPos(Owner, ID);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            ID = tree.GetString("ID");
            Owner = tree.GetString("Owner");
            OwnerName = tree.GetString("OwnerName");

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("ID", ID);
            tree.SetString("Owner", Owner);
            tree.SetString("OwnerName", OwnerName);
        }


        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            OpenGUIForPlayer(byPlayer);
            return true;
        }

        private void OpenGUIForPlayer(IPlayer byPlayer)
        {
            if (this.Api.World is IServerWorldAccessor)
            {
                ViconomyCore modSystem = Api.ModLoader.GetModSystem<ViconomyCore>();
                ViconRegister register = modSystem.GetRegistry().GetRegister(Owner, ID);

                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write("VinconomyInventory");
                    if (register != null && register.Name != null)
                    {
                        writer.Write(register.Name);
                    } else
                    {
                        writer.Write((OwnerName == null ? "Unowned" : OwnerName + "'s") + " Teller Machine");
                    }
                    writer.Write(byPlayer.PlayerUID == Owner);
                    
                    TreeAttribute tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
              ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.TOGGLE_GUI, data);
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
                        BinaryReader reader = new BinaryReader(ms);
                        string Name = reader.ReadString();
                        if (player.PlayerUID == Owner)
                        {
                            UpdateTeller(Owner, OwnerName, ID, Name);
                        }
                        else
                        {
                            ((IServerPlayer)player).SendMessage(0, Lang.Get("viconomy:doesnt-own", new object[0]), EnumChatType.OwnMessage);
                        }
                    }
                    break;
                case VinConstants.CURRENCY_CONVERSION:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        int slot = reader.ReadInt32();
                        DoCurrencyConversionForSlot(player, slot);
                    }
                    ((IServerPlayer)player).SendMessage(0, "it worked?", EnumChatType.OwnMessage);
                    break;
                default:
                    if (packetid < 1000)
                    {
                        if (player.PlayerUID != Owner)
                        {
                            if (!((ICoreServerAPI)Api).Server.IsDedicated)
                            {
                                ViconomyCore.PrintClientMessage(player, "Nice Try, but that isn't yours... If this wasn't singleplayer, you would have been kicked.", new object[] { });
                            }
                            else
                            {
                                ((IServerPlayer)player).Disconnect("Nice try, but that wasn't yours. (Tried to access Register they didn't own)");
                            }
                            return;
                        }

                        //Console.Write("Handling Inv Packet");
                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z).MarkModified();
                        return;
                    }
                    break;
            }
        }

        private void DoCurrencyConversionForSlot(IPlayer player, int slot)
        {
            bool isLeft = slot % 2 == 0;
            ItemSlot toCurrency = Inventory[slot];
            ItemSlot fromCurrency = Inventory[slot + (isLeft ? 1 : -1)];

            TradeRequest request = new TradeRequest();
            request.customer = player;
            request.numPurchases = 1;
            request.productNeeded = toCurrency.Itemstack;
            request.productSourceSlots = this.inventory.Slots;
            request.currencyNeeded = fromCurrency.Itemstack;
            //request.currencySourceSlots = TradingUtil.GetAllValidCurrencyFor(player, fromCurrency.Itemstack).ToArray();


        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedServerPacket " + packetid);
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)this.Api.World;
            if (packetid == VinConstants.TOGGLE_GUI)
            {
                if (this.invDialog != null)
                {
                    //Console.WriteLine(Api.Side + ": Toggling GUI OFF");
                    CloseGui(clientWorld);
                }
                else
                {
                    //Console.WriteLine(Api.Side + ": Toggling GUI ON");
                    OpenShopGui(data);
                }

            }
            if (packetid == VinConstants.OPEN_GUI)
            {
                OpenShopGui(data);
            }
            if (packetid == VinConstants.CLOSE_GUI)
            {
                CloseGui(clientWorld);
            }

        }

        private void OpenShopGui(byte[] data)
        {
            TreeAttribute tree = new TreeAttribute();
            string dialogTitle;
            bool isOwner;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                reader.ReadString();
                dialogTitle = reader.ReadString();
                isOwner = reader.ReadBoolean();
                tree.FromBytes(reader);
            }
            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            this.invDialog = new GuiViconTeller(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, isOwner);
            //this.invDialog.OpenSound = this.OpenSound;
            //this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;

                ((ClientCoreAPI) Api).Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.CLOSE_GUI, null);
            };
            //Console.WriteLine(Api.Side + ": Attempted to open Shop GUI");
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
            //Console.WriteLine(Api.Side + ": Attempted to close GUI");
        }

        /***
         *  Records the purchase of an item. 
         */
        public void PurchasedItem(IPlayer player, BEViconStall stall, ItemStack productClone, ItemStack payment)
        {
            OnRecordPurchase?.Invoke(player, stall, productClone, payment);
            RecordPurchase(player, productClone, payment);
        }
        public event OnRecordPurchaseDelegate OnRecordPurchase;


        private void RecordPurchase(IPlayer player, ItemStack product, ItemStack payment)
        {
            EnumMonth month = this.Api.World.Calendar.MonthName;
            int year = this.Api.World.Calendar.Year;

            foreach (ItemSlot slot in this.inventory) { 
                if (slot.Itemstack?.Collectible is ItemLedger)
                {
                    ITreeAttribute attr = slot.Itemstack.Attributes;
                    //TODO: Convert to SQL. This is going to be a lot of data, fast.

                    //I dont know if Attributes is ever null... Oh well
                    if (attr == null)
                    {
                        attr = new TreeAttribute();
                        slot.Itemstack.Attributes = attr;
                    }

                    ITreeAttribute itYear = attr.GetOrAddTreeAttribute("Year-" + year);
                    ITreeAttribute itMonth = itYear.GetOrAddTreeAttribute("Month-" + month);
                    ITreeAttribute itProduct = itMonth.GetOrAddTreeAttribute(product.Collectible.Code.Path);



                    ITreeAttribute itProfits = itMonth.GetOrAddTreeAttribute("Profits");
                    ITreeAttribute itCustomers = itProduct.GetOrAddTreeAttribute("Customers");

                    int numSales = itProduct.GetInt("Sales", 0) +1;
                    itProduct.SetInt("Sales", numSales);

                    ITreeAttribute sale = itProduct.GetOrAddTreeAttribute("Sale-" + numSales);

                    sale.SetString("Customer", player.PlayerUID);
                    sale.SetString("Product" , product.GetName());
                    sale.SetInt("ProductAmt", product.StackSize);
                    sale.SetString("Payment", payment.GetName());
                    sale.SetInt("PaymentAmt", payment.StackSize);

                    return;
                }
            }
        }
    }

}