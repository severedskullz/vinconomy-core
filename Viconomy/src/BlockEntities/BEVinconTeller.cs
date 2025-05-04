using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.GUI;
using Viconomy.Registry;
using Viconomy.Inventory.Impl;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;

namespace Viconomy.BlockEntities
{
    public class BEVinconTeller : BEVinconBase
    {
        private ViconomyTellerInventory inventory;
        private GuiViconTeller invDialog;


        public override InventoryBase Inventory => inventory;

        public override string InventoryClassName => "ViconomyRegisteryInventory";

        public BEVinconTeller()
        {
            this.inventory = new ViconomyTellerInventory(this, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }

        public void UpdateTeller(string Owner, string OwnerName)
        {
            this.Owner = Owner;
            this.OwnerName = OwnerName;
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            OpenGUIForPlayer(byPlayer);
            return true;
        }

        private void OpenGUIForPlayer(IPlayer byPlayer)
        {
            if (this.Api.World is IServerWorldAccessor)
            {
                VinconomyCoreSystem modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
                ShopRegistration register = modSystem.GetRegistry().GetShop(RegisterID);

                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(OwnerName == null ? "" : OwnerName);
                    writer.Write(byPlayer.PlayerUID == Owner);
                    TreeAttribute tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
              ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos, VinConstants.TOGGLE_GUI, data);
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
                case VinConstants.SET_ADMIN_SHOP:
                    bool isAdmin = false;
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        isAdmin = reader.ReadBoolean();
                    }
                    SetAdminShop(player, isAdmin);
                    break;

                case VinConstants.SET_REGISTER_ID:
                    SetStallRegisterID(player, data);
                    break;

                case VinConstants.CURRENCY_CONVERSION:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        int slot = reader.ReadInt32();

                        TryPurchaseItem(player, slot, 1);
                    }
                    break;
                default:
                    if (packetid < 1000)
                    {
                        if (player.PlayerUID != Owner)
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

                        //Console.Write("Handling Inv Packet");
                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
                        return;
                    }
                    break;
            }
        }

        private void TryPurchaseItem(IPlayer player, int stallSlot, int desiredAmount)
        {

            ItemSlot currency = GetCurrencyForStall(stallSlot);
            if (currency.Itemstack == null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_PRICE, null);
                return;
            }

            // Does the shop have a register ID set?
            if (this.RegisterID == -1 && !this.IsAdminShop)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NOT_REGISTERED);
                return;
            }


            // Is there a shop with the given Register ID?
            BEVinconRegister register = modSystem.GetShopRegister(this.Owner, this.RegisterID);
            if (register == null && !this.IsAdminShop)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NOT_REGISTERED);
                return;
            }

            if (modSystem.CanPurchaseItem(player, this, register, stallSlot, desiredAmount))
            {
                PurchaseItem(player, stallSlot, desiredAmount, register);
            }

        }

        public override void PurchaseItem(IPlayer player, int stallSlot, int desiredAmount, BEVinconRegister shopRegister)
        {
            bool isLeft = stallSlot % 2 == 0;
            ItemSlot toCurrency = Inventory[stallSlot];
            ItemSlot fromCurrency = Inventory[stallSlot + (isLeft ? 1 : -1)];

            List<ItemSlot> productSlots = new List<ItemSlot>();

            if (IsAdminShop)
            {
                InventoryGeneric genInv = new InventoryGeneric(1, "purchase-inv" + Inventory.InventoryID, Api);
                genInv[0].Itemstack = TradingUtil.GetItemStackClone(toCurrency);
                productSlots.Add(genInv[0]);
            } else if (shopRegister != null)
            {
                foreach (ItemSlot slot in shopRegister.Inventory)
                {
                    if (slot.Itemstack != null && toCurrency.Itemstack.Satisfies(slot.Itemstack))
                    {
                        productSlots.Add(slot);
                    }
                }
            }


            TradeRequest request = new TradeRequest();
            request.customer = player;
            request.numPurchases = 1;
            request.productNeeded = TradingUtil.GetItemStackClone(toCurrency);
            request.productSourceSlots = productSlots.ToArray();
            request.currencyNeeded = TradingUtil.GetItemStackClone(fromCurrency);
            request.shopRegister = shopRegister;
            request.sellingEntity = this;
            request.coreApi = this.Api;
            request.isAdminShop = this.IsAdminShop;

            TradeResult result = TradingUtil.TryPurchaseItem(request);
            if (result.error != null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, result.error);
            }
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
                string name = reader.ReadString();
                if (name.Length > 0)
                {
                    dialogTitle = Lang.Get("vinconomy:gui-teller-owner", new string[] { name });
                }
                else
                {
                    dialogTitle = Lang.Get("vinconomy:gui-teller-unowned");
                }
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

                ((ClientCoreAPI) Api).Network.SendBlockEntityPacket(this.Pos, VinConstants.CLOSE_GUI, null);
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

        public override ItemSlot[] GetSlotsForStall(int stallSlot)
        {
            throw new NotImplementedException();
        }

        public override ItemSlot GetCurrencyForStall(int stallSlot)
        {
            return this.inventory[stallSlot];
        }

        public override int GetNumItemsPerPurchaseForStall(int stallSlot)
        {
            return inventory[stallSlot].StackSize;
        }

        protected override float[][] genTransformationMatrices()
        {
            return null;
        }
    }

}