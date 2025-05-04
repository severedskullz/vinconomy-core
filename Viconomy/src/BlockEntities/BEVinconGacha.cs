using System;
using System.IO;
using System.Text;
using Viconomy.GUI;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.Slots;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockEntities
{
    public class BEVinconGacha : BEVinconBase
    {
        
        protected GuiDialogBlockEntity invDialog;
        protected ViconomyGachaInventory inventory;
        private MeshData[] meshes;
        public bool useTotalRandomizer { get; private set; }

        public override InventoryBase Inventory { get { return this.inventory; } }

        public override int DisplayedItems => StallSlotCount;

        public BEVinconGacha()
        {
            this.inventory = new ViconomyGachaInventory(10, null, Api);
            this.inventory.SlotModified += Inventory_SlotModified;
        }

        protected void Inventory_SlotModified(int slot)
        {
            //updateMeshes();
            this.MarkDirty(true, null);
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            int slotIndex = blockSel.SelectionBoxIndex;
            //Console.WriteLine("Calling OnPlayerRightClick from " + Api.Side);
            bool shiftMod = byPlayer.Entity.Controls.Sneak;
           
            if (byPlayer.PlayerUID == Owner)
            {
                if (shiftMod)
                {
                    if (this.inventory.HasProduct())
                    {
                        ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                        ItemSlot currency = this.inventory.GetCurrency();
                        if (currency != null && TradingUtil.isMatchingItem(currency.Itemstack, handSlot.Itemstack, byPlayer.Entity.World))
                        {
                            RequestPurchaseItem();
                        }                      
                    }
                }
                else
                {
                    // Open shop admin gui
                    OpenShopForPlayer(byPlayer, blockSel.SelectionBoxIndex);
                }
            } 
            else
            {
                if ( shiftMod )
                {
                    //Purchase the items.
                    RequestPurchaseItem();
                } else
                {
                    // Open the shop inventory for that block selection
                    OpenShopForPlayer(byPlayer, blockSel.SelectionBoxIndex);
                }
            }
            return true;
        }

        private void TryPurchaseItem(IPlayer player, byte[] data) {

           

            //Console.WriteLine(Api.Side + ": We tried to purchase item!");
            //PrintClientMessage(player, Api.Side + ": We tried to purchase item!");
            
            ItemSlot currency = this.inventory.GetCurrency();
            if (currency.Itemstack == null) {
                //PrintClientMessage(player, "vinconomy:item-cost", new Object[] { currency.Itemstack.StackSize, currency.Itemstack.GetName() });
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
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.COULDNT_GET_REGISTER);
                return;
            }

            ViconItemSlot stockSlot = this.inventory.GetRandomItem(this.useTotalRandomizer);
            if (stockSlot == null)
            {
                //Console.WriteLine(Api.Side + ": Not enough stock to purchase item");
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_PRODUCT);
                return;
            }

            if (modSystem.CanPurchaseItem(player, this, register, 0, 1))
            {
                PurchaseItem(player, stockSlot.itemSlot, 1, register);
            }

        }



      

        #region GUI

        private void OpenShopForPlayer(IPlayer byPlayer, int selectedStall)
        {
            if (this.Api.World is IServerWorldAccessor)
            {
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
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos, VinConstants.OPEN_GUI, data);
                
                if (byPlayer.PlayerUID == Owner)
                    byPlayer.InventoryManager.OpenInventory(this.inventory);
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
                    dialogTitle = Lang.Get("vinconomy:gui-stall-owner", new string[] { name });
                }
                else
                {
                    dialogTitle = Lang.Get("vinconomy:gui-stall-unowned");
                }
                isOwner = reader.ReadBoolean();
                tree.FromBytes(reader);
            }
            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            if (isOwner)
                this.invDialog = new GuiViconGachaOwner(dialogTitle, this.inventory, this.Pos, this.Api as ICoreClientAPI);
            else
                this.invDialog = new GuiDialogViconGachaCustomer(dialogTitle, this.inventory, this.Pos, this.Api as ICoreClientAPI);
            //this.invDialog.OpenSound = this.OpenSound;
            //this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;
                capi.Network.SendBlockEntityPacket(this.Pos, VinConstants.CLOSE_GUI, null);
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
            Console.WriteLine(Api.Side + ": Attempted to close GUI");
        }

        #endregion


        #region Networking

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedClientPacket " + packetid);
            //PrintClientMessage(player, Api.Side + ": OnRecievedClientPacket");
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            switch (packetid)
            {
                case VinConstants.OPEN_GUI:
                    if (inventoryManager == null)
                    {
                        return;
                    }
                    inventoryManager.OpenInventory(this.Inventory);
                    break;

                case VinConstants.CLOSE_GUI:
                    if (inventoryManager != null)
                    {
                        inventoryManager.CloseInventory(this.Inventory);
                    }
                    break;

                case VinConstants.PURCHASE_ITEMS:
                    TryPurchaseItem(player, data);
                    break;

                case VinConstants.SET_REGISTER_ID:
                    SetStallRegisterID(player, data);
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

                case VinConstants.SET_TOTAL_RANDOMIZER:
                    bool isTotalRandom = false;
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        isTotalRandom = reader.ReadBoolean();
                    }
                    SetUseTotalRandomizer(isTotalRandom);
                    break;

                default:
                    if (packetid < 1000)
                    {
                        if (player.PlayerUID != Owner)
                        {
                            ((IServerPlayer)player).Disconnect("Nice try, but that wasn't yours. (Tried to access Stall they didn't own)");
                            return;
                        }

                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos).MarkModified();
                        return;
                    }
                    break;
            }
        }

        protected void SetUseTotalRandomizer(bool useTotalRandomizer)
        {
            this.useTotalRandomizer = useTotalRandomizer;
            this.MarkDirty();
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

        private void RequestPurchaseItem()
        {
            //this.Api.Logger.Chat("Attempting to purchase item from slot " + slot);
            if (this.Api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI coreClientAPI = this.Api as ICoreClientAPI;
                if (coreClientAPI != null)
                {
                    coreClientAPI.Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.PURCHASE_ITEMS, null);
                    coreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                }
            }
        }

        #endregion
        protected override float[][] genTransformationMatrices()
        {
            return new float[][] { };
        }

        protected override void updateMesh(int index)
        {
            //Do nothing.
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            
            if (this.inventory[0].Itemstack == null)
            {
                return false;
            }
            //mesher.AddMeshData(this.meshes[this.inventory[0].StackSize], 1);

            return false;
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.invDialog != null)
            {
                if (invDialog.IsOpened())
                {
                    this.invDialog.TryClose();
                }

                //this.invDialog.Dispose(); // Got a Null Pointer when I had this closed the game when menu was open... how is invDialog null here?
                this.invDialog = null;
            }

        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            GuiDialogBlockEntity guiDialogBlockEntity = this.invDialog;
            if (guiDialogBlockEntity != null && guiDialogBlockEntity.IsOpened())
            {
                GuiDialogBlockEntity guiDialogBlockEntity2 = this.invDialog;
                if (guiDialogBlockEntity2 != null)
                {
                    guiDialogBlockEntity2.TryClose();
                }
            }
            GuiDialogBlockEntity guiDialogBlockEntity3 = this.invDialog;
            if (guiDialogBlockEntity3 == null)
            {
                return;
            }
            guiDialogBlockEntity3.Dispose();
        }

       
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            int numItems = inventory.GetTotalItems();
            ItemSlot currency = inventory.GetCurrency();
            if (numItems > 0 && currency != null && currency.Itemstack != null)
            {
                dsc.AppendLine(Lang.Get("vinconomy:for-sale", new Object[] { 0, 1, "Mystery Item", currency.Itemstack.StackSize, currency.Itemstack.GetName() }));
                dsc.AppendLine();
                for (int i = 1; i < inventory.Slots.Length; i++)
                {
                    if (inventory.Slots[i].Itemstack != null)
                    {
                        dsc.AppendLine(Lang.Get("vinconomy:gacha-possibility", new Object[] { inventory.GetChanceForSlot(i,useTotalRandomizer), inventory.Slots[i].Itemstack.GetName() }));
                    }
                    
                }
                
            } else
            {
                dsc.AppendLine(Lang.Get("vinconomy:not-for-sale", new Object[] { 0 }));
            }

            //base.GetBlockInfo(forPlayer, dsc);
        }

        public override void DropContents(Vec3d atPos)
        {
            this.Inventory.DropAll(atPos, 0);
        }

        public override float GetPerishRate()
        {
            return 0;
        }

        public override ItemSlot[] GetSlotsForStall(int stallSlot)
        {

            return new ItemSlot[] { inventory[stallSlot] };
        }

        public override ItemSlot GetCurrencyForStall(int stallSlot)
        {
            return inventory[0];
        }

        public override int GetNumItemsPerPurchaseForStall(int stallSlot)
        {
            return 1;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("useTotalRandomizer", useTotalRandomizer);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            this.useTotalRandomizer = tree.GetBool("useTotalRandomizer");
        }

  


    }

}
