using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Viconomy.Filters;
using Viconomy.GUI;
using Viconomy.Inventory;
using Viconomy.Renderer;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEViconStall : BlockEntityDisplay
    {
        ViconomyCore modSystem;

        protected ViconomyInventory inventory;
        protected GuiDialogBlockEntity invDialog;
        protected Block block;

        public override int DisplayedItems => StallSlotCount;

        protected AssetLocation OpenSound;
        protected AssetLocation CloseSound;

        public string Owner;
        public string OwnerName;
        public string RegisterID;
        public bool isAdminShip;

      
        //public virtual AssetLocation OpenSound { get; set; } = new AssetLocation("sounds/block/chestopen");
        //public virtual AssetLocation CloseSound { get; set; } = new AssetLocation("sounds/block/chestclose");
        public override InventoryBase Inventory { get { return this.inventory; } }
        public override string InventoryClassName { get { return "VinconomyInventory"; } }

        public virtual int StallSlotCount { get; protected set; } = 4;
        public virtual int StacksPerSlot { get; protected set; } = 9;
        public virtual int BulkPurchaseAmount { get; protected set; } = 5;



        public BEViconStall()
        {
            ConfigureInventory();
            this.inventory.SlotModified += Inventory_SlotModified;
        }

        protected void Inventory_SlotModified(int slot)
        {
            updateMeshes();
            this.MarkDirty(true, null);
        }

        public virtual void ConfigureInventory()
        {
            this.inventory = new ViconomyInventory(this, null, Api, StallSlotCount, StacksPerSlot);
            for (int i = 0; i < StallSlotCount; i++)
            {
                inventory.SetSlotFilter(i, ViconomyFilters.IsGenericItem);
                inventory.SetSlotBackground(i, "vicon-general");
            }
        }


        public override void Initialize(ICoreAPI api)
        {
            modSystem = api.ModLoader.GetModSystem<ViconomyCore>();

            this.block = api.World.BlockAccessor.GetBlock(this.Pos); 

            base.Initialize(api);

            JsonObject attributes = block.Attributes;
            string openSound = null;
            if (attributes != null && attributes["openSound"] != null)
            {
                openSound = attributes["openSound"].AsString(null);
            }

            string closeSound = null;
            if (attributes != null && attributes["openSound"] != null)
            {
                closeSound = attributes["openSound"].AsString(null); ;
            }

            this.OpenSound = (openSound == null) ? null : AssetLocation.Create(openSound, block.Code.Domain);
            this.CloseSound = (closeSound == null) ? null : AssetLocation.Create(closeSound, block.Code.Domain);            
        }

        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            //Console.WriteLine("Calling OnPlayerRightClick from " + Api.Side);
            bool shiftMod = byPlayer.Entity.Controls.Sneak;
            bool ctrlMod = byPlayer.Entity.Controls.Sprint;


            ItemSlot hotbarslot = byPlayer.InventoryManager.ActiveHotbarSlot;


            if (byPlayer.PlayerUID == Owner)
            {
               if(shiftMod)
               {
                    //Add items to slot
                    TryPut(hotbarslot, blockSel, ctrlMod);
               } else
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
                    int desiredAmount = ctrlMod ? BulkPurchaseAmount : 1;
                    RequestPurchaseItem(blockSel.SelectionBoxIndex, desiredAmount);
                    //TryPurchaseItem(byPlayer, hotbarslot, blockSel, ctrlMod);
                } else
                {
                    // Open the shop inventory for that block selection
                    OpenShopForPlayer(byPlayer, blockSel.SelectionBoxIndex);
                }
            }
            return true;
        }

        private void TryPurchaseItem(IPlayer player, byte[] data) {

            int stallSlot;
            int desiredAmount;
            bool useHandSlot;
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                stallSlot = binaryReader.ReadInt32();
                desiredAmount = binaryReader.ReadInt32();
                useHandSlot = binaryReader.ReadBoolean();
            }


            //Console.WriteLine(Api.Side + ": We tried to purchase item!");
            //PrintClientMessage(player, Api.Side + ": We tried to purchase item!");
            
            ItemSlot currency = this.inventory.GetCurrencyForSelection(stallSlot);
            

            if (currency.Itemstack == null) {
                //PrintClientMessage(player, "vinconomy:item-cost", new Object[] { currency.Itemstack.StackSize, currency.Itemstack.GetName() });
                ViconomyCore.PrintClientMessage(player, "vinconomy:no-price", null);
                return;
            } 
            
            // Does the shop have a register ID set?
            if (this.RegisterID == null && !this.isAdminShip)
            {
                ViconomyCore.PrintClientMessage(player, "vinconomy:not-registered-with-shop");
                return;
            }

            
            // Is there a shop with the given Register ID?
            BEVRegister register = modSystem.GetShopRegister(this.Owner, this.RegisterID);
            if (register == null && !this.isAdminShip)
            {
                ViconomyCore.PrintClientMessage(player, "vinconomy:not-registered-with-shop");
                return;
            }
  
            ItemSlot[] stockSlots = this.inventory.GetSlotsForSelection(stallSlot);
            int itemsPerPurchase = this.inventory.GetItemsPerPurchase(stallSlot);
            ItemSlot purchaseSlot = null;
            
            //Find the first slot available that we can purchase from
            foreach (var stockSlot in stockSlots)
            {
                if (stockSlot.StackSize >= itemsPerPurchase)
                {
                    purchaseSlot = stockSlot;
                    break;
                }
            }

            if (purchaseSlot == null)
            {
                //Console.WriteLine(Api.Side + ": Not enough stock to purchase item");
                ViconomyCore.PrintClientMessage(player, "vinconomy:no-product");
                return;
            }
            if (modSystem.CanPurchaseItem(player, this, register, stallSlot, purchaseSlot, currency, desiredAmount))
            {
                if (useHandSlot)
                {
                    PurchaseWithHandSlot(player, purchaseSlot, stallSlot, desiredAmount, register);
                }
                else
                {
                    PurchaseWithInventory(player, purchaseSlot, stallSlot, desiredAmount, register);
                }
            }

        }

        public void PurchaseWithInventory(IPlayer player, ItemSlot purchaseSlot, int stallSlot, int desiredAmount, BEVRegister shopRegister)
        {
            //TODO
        }

        public void PurchaseWithHandSlot(IPlayer player, ItemSlot purchaseSlot, int stallSlot, int desiredAmount, BEVRegister shopRegister)
        {
            int itemsPerPurchase = this.inventory.StallSlots[stallSlot].itemsPerPurchase;
            ItemSlot currency = this.inventory.GetCurrencyForSelection(stallSlot);

            ItemSlot handItem = player.InventoryManager.ActiveHotbarSlot;
            if (!currency.Itemstack.Equals(Api.World, handItem.Itemstack, null) || handItem.StackSize < currency.StackSize)
            {
                //Console.WriteLine(Api.Side + ": Not enough money!");
                ViconomyCore.PrintClientMessage(player, "vinconomy:not-enough-money");
                return;
            }

            desiredAmount = Math.Min(desiredAmount, handItem.Itemstack.StackSize / currency.Itemstack.StackSize);
            desiredAmount = Math.Min(purchaseSlot.StackSize / itemsPerPurchase, desiredAmount);
            if (desiredAmount > 0)
            {
                int price = currency.Itemstack.StackSize * desiredAmount;
                int productAmount = desiredAmount * itemsPerPurchase;
                ItemStack currencyStack = handItem.Itemstack.Clone();
                currencyStack.StackSize = price;

                //Console.WriteLine(Api.Side + ": Checking if we can hold!");
                //PrintClientMessage(player, Api.Side + ": We tried to purchase item!");
                if (shopRegister == null || shopRegister.CanHold(currencyStack))
                {
                    PurchaseItem(player, shopRegister, purchaseSlot, handItem, productAmount, price);
                }
                else
                {
                    ViconomyCore.PrintClientMessage(player, "vinconomy:not-enough-stock");
                    //Console.WriteLine(Api.Side + ": Something went horribly wrong - Not enough stock to purchase item");
                }
            }
            else
            {
                ViconomyCore.PrintClientMessage(player, "vinconomy:purchased-zero-quantity");
                //Console.WriteLine(Api.Side + ": Something went horribly wrong - Tried to take 0 stock!");
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
                    writer.Write("VinconomyInventory");
                    writer.Write((OwnerName == null ? "Unowned" : OwnerName + "'s") + " Stall");
                    writer.Write((byte)StallSlotCount);
                    writer.Write((byte)StacksPerSlot);
                    writer.Write((byte)selectedStall);
                    writer.Write(byPlayer.PlayerUID == Owner);
                    TreeAttribute tree = new TreeAttribute();
                    this.inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
                ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.OPEN_GUI, data);
                
                if (byPlayer.PlayerUID == Owner)
                    byPlayer.InventoryManager.OpenInventory(this.inventory);
                }
        }

        private void OpenShopGui(byte[] data)
        {
            TreeAttribute tree = new TreeAttribute();
            string dialogTitle;
            int stallSlots;
            int itemsPerStallSlot;
            int stallSelection;
            bool isOwner;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                reader.ReadString();
                dialogTitle = reader.ReadString();
                stallSlots = (int)reader.ReadByte();
                itemsPerStallSlot = (int)reader.ReadByte();
                stallSelection = (int)reader.ReadByte();
                isOwner = reader.ReadBoolean();
                tree.FromBytes(reader);
            }
            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            if (isOwner)
                this.invDialog = new GuiDialogViconStallOwner(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
            else
                this.invDialog = new GuiDialogViconStallCustomer(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
            this.invDialog.OpenSound = this.OpenSound;
            this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;
                capi.Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.CLOSE_GUI, null);
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

                case VinConstants.SET_ITEMS_PER_PURCHASE:
                    SetStallItemsPerPurchase(player, data);
                    break;

                case VinConstants.SET_SHOP_ID:
                    SetStallID(player, data);
                    break;

                case VinConstants.SET_ADMIN_SHOP:
                    SetAdminShop(player, data);
                    break;

                default:
                    if (packetid < 1000)
                    {
                        if (player.PlayerUID != Owner)
                        {
                            if ( !((ICoreServerAPI)Api).Server.IsDedicated )
                            {
                                ViconomyCore.PrintClientMessage(player, "Nice Try, but that isn't yours... If this wasn't singleplayer, you would have been kicked.", new object[] { });
                            } else
                            {
                                ((IServerPlayer)player).Disconnect("Nice try, but that wasn't yours. (Tried to access Stall they didn't own)");
                            }
                            return;
                        }

                        this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                        this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z).MarkModified();
                        return;
                    }
                    break;
            }
        }

        private void SetStallItemsPerPurchase(IPlayer byPlayer, byte[] data)
        {
            if (byPlayer.PlayerUID != this.Owner)
            {
                ViconomyCore.PrintClientMessage(byPlayer, "vinconomy:doesnt-own", new object[] { });
                return;
            }

            int stallSlot;
            int amountItems;
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                stallSlot = (int)reader.ReadInt32();
                amountItems = (int)reader.ReadInt32();
            }

            this.inventory.StallSlots[stallSlot].itemsPerPurchase = amountItems;
            //PrintClientMessage(byPlayer, "set quantity to " + amountItems, new object[] { amountItems });
            this.MarkDirty();
        }

        private void SetStallID(IPlayer byPlayer, byte[] data)
        {
            if (byPlayer.PlayerUID != this.Owner)
            {
                ViconomyCore.PrintClientMessage(byPlayer, "vinconomy:doesnt-own", new object[] { });
                return;
            }

            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                string ID = reader.ReadString();
                if (ID != "None")
                {
                    this.RegisterID = ID;
                }
                else
                {
                    this.RegisterID = null;
                }
            }

            //PrintClientMessage(byPlayer, "set ID to " + this.RegisterID);
            this.MarkDirty();
        }

        private void SetAdminShop(IPlayer byPlayer, byte[] data)
        {
            if (byPlayer.PlayerUID != this.Owner)
            {
                ViconomyCore.PrintClientMessage(byPlayer, "vinconomy:doesnt-own", new object[] { });
                return;
            }

            if (!byPlayer.HasPrivilege("gamemode"))
            {
                ViconomyCore.PrintClientMessage(byPlayer, "vinconomy:no-privelege", new object[] { });
                return;
            }

            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                this.isAdminShip = reader.ReadBoolean();
            }

            //PrintClientMessage(byPlayer, "set Admin Shop to " + this.isAdminShip);
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
                    Console.WriteLine(Api.Side + ": Toggling GUI OFF");
                    CloseGui(clientWorld);
                }
                else
                {
                    Console.WriteLine(Api.Side + ": Toggling GUI ON");
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

        private void RequestPurchaseItem(int slot, int amount)
        {
            if (this.Api.Side == EnumAppSide.Client)
            {
               
                //this.Api.Logger.Chat("Attempting to purchase item from slot " + slot);
                if (this.Api.Side == EnumAppSide.Client)
                {
                    byte[] data;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        BinaryWriter writer = new BinaryWriter(ms);
                        writer.Write(slot);
                        writer.Write(amount);
                        writer.Write(true);
                        data = ms.ToArray();
                    }
                    ICoreClientAPI coreClientAPI = this.Api as ICoreClientAPI;
                    if (coreClientAPI != null)
                    {
                        coreClientAPI.Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, VinConstants.PURCHASE_ITEMS, data);
                        coreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    }
                }
            }
        }

        #endregion


        protected override void updateMesh(int index)
        {
            ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(index);
            if (Api != null && Api.Side != EnumAppSide.Server && slot != null && !slot.Empty)
            {
                getOrCreateMesh(slot.Itemstack, index);
            }
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            for (int i = 0; i < StallSlotCount; i++)
            {
                ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(i);
                if (slot != null && !slot.Empty && tfMatrices != null)
                {
                    mesher.AddMeshData(getOrCreateMesh(slot.Itemstack, i), tfMatrices[i]);
                }
            }

            return false;
        }

        protected override float[][] genTransformationMatrices()
        {

            float[][] tfMatrices = new float[StallSlotCount][];
            for (int index = 0; index < StallSlotCount; index++)
            {
                float scale = 0.35f;
                ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(index);
                if (slot != null)
                    if (slot.Itemstack.Class != EnumItemClass.Block)
                    {
                        scale = .85f;
                    }

                Cuboidf sb = block.SelectionBoxes[index];
                float left = .25f - (scale / 2);
                float right = left + .5f;

                float x = (index % 2 == 0) ? left : right;
                float y = sb.YSize <= .45f ? sb.MaxY - 0.37f + (.45f - sb.YSize) : sb.MaxY - 0.37f;
                float z = (index / 2 == 0) ? left : right;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(this.block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f).Scale(scale, scale, scale);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }
    



        private bool TryPut(ItemSlot slot, BlockSelection blockSel, bool bulk)
        {

            if (slot?.Itemstack == null)
            {
                return false;
            }

            int stallSlot = blockSel.SelectionBoxIndex;
            ItemSlot[] slots = this.inventory.GetSlotsForSelection(stallSlot);
            int amountItem = bulk ? slot.Itemstack.StackSize : 1;
            bool movedItems = false;
           
            for (int i = 0; i < StacksPerSlot; i++)
            {
                if (slot.Itemstack != null)
                {
                    int moved = slot.TryPutInto(this.Api.World, slots[i], bulk ? slot.Itemstack.StackSize : 1);
                    amountItem -= moved;
                    if (moved > 0)
                    {
                        movedItems = true;
                    }

                    if (amountItem <= 0)
                    {
                        break;
                    }
                }
            }
            

          
            if (movedItems) {
                //this.updateMeshes();
                this.MarkDirty(true, null);
                ICoreClientAPI coreClientAPI = this.Api as ICoreClientAPI;
                if (coreClientAPI != null)
                {
                    coreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    updateMeshes();
                }
            }
          
            return false;
        }

        private ItemStack GetProductStack(ItemSlot purchaseSlot, int amount)
        {
            ItemStack productStack = null;
            if (!purchaseSlot.Empty && purchaseSlot.StackSize >= amount)
            {
                
                if (isAdminShip)
                {
                    productStack = purchaseSlot.Itemstack.Clone();
                    productStack.StackSize = amount;
                }
                else
                {
                    productStack = purchaseSlot.TakeOut(amount);
                }
            }
            return productStack;
        }

        private ItemStack GetPaymentStack(ItemSlot paymentStack, int amount)
        {
            return paymentStack.TakeOut(amount);
        }


        private bool PurchaseItem(IPlayer byPlayer, BEVRegister shopRegister, ItemSlot purchaseSlot, ItemSlot currencySlot, int amount, int price)
        {
            if (this.Api.Side == EnumAppSide.Client)
            {
                return false;
            }
            //TODO: This is all serverside, having clientside code in here is pointless..
            if (!purchaseSlot.Empty && purchaseSlot.StackSize >= amount)
            {
                ItemStack productStack = GetProductStack(purchaseSlot, amount);
                ItemStack productStackClone = productStack.Clone();
                ItemStack paymentStack = GetPaymentStack(currencySlot, price);

                modSystem.PurchasedItem(byPlayer, this, shopRegister, productStackClone, paymentStack);

                GivePlayerProduct(byPlayer, productStack);
                
                if (shopRegister != null)
                {
                    shopRegister.PurchasedItem(byPlayer, this, productStackClone, paymentStack);
                    shopRegister.AddItem(paymentStack, price);
                }

                ViconomyCore.PrintClientMessage(byPlayer, "vinconomy:purchased-item", new object[] { amount, productStack.GetName(), price, paymentStack.GetName() });

                purchaseSlot.MarkDirty();
                currencySlot.MarkDirty();
                this.MarkDirty(true, null);
                this.updateMeshes();
                return true;
            }
            return false;
        }

        private void GivePlayerProduct(IPlayer byPlayer, ItemStack productStack)
        {
            byPlayer.InventoryManager.TryGiveItemstack(productStack, false);
            if (productStack.StackSize > 0)
            {
                this.Api.World.SpawnItemEntity(productStack, this.Pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
            }
            Block block = productStack.Block;
            AssetLocation assetLocation;
            if (block == null)
            {
                assetLocation = null;
            }
            else
            {
                BlockSounds sounds = block.Sounds;
                assetLocation = ((sounds != null) ? sounds.Place : null);
            }
            AssetLocation sound = assetLocation;
            this.Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16f, 1f);
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
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
            int i = 0;
            foreach (StallSlot slot in inventory.StallSlots)
            {
                i++;
                ItemSlot stock = slot.FindFirstNonEmptyStockSlot();
                ItemSlot currency = slot.currency;
                if (stock != null && stock.Itemstack != null && currency.Itemstack != null)
                {
                    dsc.AppendLine(Lang.Get("vinconomy:for-sale", new Object[] {i, slot.itemsPerPurchase, stock.Itemstack.GetName(), currency.Itemstack.StackSize,  currency.Itemstack.GetName()}));
                } else
                {
                    dsc.AppendLine(Lang.Get("vinconomy:not-for-sale", new Object[] { i }));
                }
                
            }
            //dsc.AppendLine();
            //base.GetBlockInfo(forPlayer, dsc);
        }

        public override void DropContents(Vec3d atPos)
        {
            this.Inventory.DropAll(atPos, 0);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("Owner",this.Owner);
            tree.SetString("OwnerName", this.OwnerName);
            tree.SetString("RegisterID", this.RegisterID);
            tree.SetBool("isAdminShop", this.isAdminShip);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            this.Owner = tree.GetString("Owner");
            this.OwnerName = tree.GetString("OwnerName");
            this.RegisterID = tree.GetString("RegisterID");
            this.isAdminShip = tree.GetBool("isAdminShop");

        }

        public override float GetPerishRate()
        {
            return inventory.GetTransitionSpeedMul(EnumTransitionType.Perish, null);
        }

        protected override MeshData getOrCreateMesh(ItemStack stack, int index)
        {
            MeshData modeldata = getMesh(stack);
            if (modeldata != null)
            {
                return modeldata;
            }


            IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
            if (containedMeshSource != null)
            {
                return containedMeshSource.GenMesh(stack, capi.BlockTextureAtlas, Pos);
            }

            IItemRenderer renderer = modSystem.GetRenderer(stack);
            if (renderer != null)
            {
                modeldata = renderer.createMesh(this, stack, index);

                if (stack.Collectible.Attributes?[AttributeTransformCode].Exists ?? false)
                {
                    ModelTransform modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                    modelTransform.EnsureDefaultValues();
                    modeldata.ModelTransform(modelTransform);
                }
                else if (AttributeTransformCode == "onshelfTransform" && (stack.Collectible.Attributes?["onDisplayTransform"].Exists ?? false))
                {
                    ModelTransform modelTransform2 = stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();
                    modelTransform2.EnsureDefaultValues();
                    modeldata.ModelTransform(modelTransform2);
                }

                if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
                {
                    modeldata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), MathF.PI / 2f, 0f, 0f);
                    modeldata.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
                    modeldata.Translate(0f, -15f / 32f, 0f);
                }


                if (renderer.shouldCache(stack))
                {
                    string meshCacheKey = getMeshCacheKey(stack);
                    MeshCache[meshCacheKey] = modeldata;
                }
            }

            

            return modeldata;
        }

        protected MeshData getOrCreateMeshOld(ItemStack stack, int index)
        {
            
            MeshData modeldata = getMesh(stack);
            if (modeldata != null)
            {
                return modeldata;
            }

            bool skipCache = false;
            IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
            if (containedMeshSource != null)
            {
                modeldata = containedMeshSource.GenMesh(stack, capi.BlockTextureAtlas, Pos);
            }

            if (modeldata == null)
            {
                ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
                if (stack.Block is BlockMicroBlock)
                {
                    skipCache = true;
                    ITreeAttribute treeAttribute = stack.Attributes;
                    if (treeAttribute == null)
                    {
                        treeAttribute = new TreeAttribute();
                    }
                    int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(treeAttribute, (this.Api as ICoreClientAPI).World);
                    IntArrayAttribute intArrayAttribute = treeAttribute["cuboids"] as IntArrayAttribute;
                    uint[] array = (intArrayAttribute != null) ? intArrayAttribute.AsUint : null;
                    if (array == null)
                    {
                        LongArrayAttribute longArrayAttribute = treeAttribute["cuboids"] as LongArrayAttribute;
                        array = ((longArrayAttribute != null) ? longArrayAttribute.AsUint : null);
                    }
                    List<uint> voxelCuboids = (array == null) ? new List<uint>() : new List<uint>(array);
                    modeldata = BlockEntityMicroBlock.CreateMesh(this.Api as ICoreClientAPI, voxelCuboids, materials);
                }
                else if(stack.Class == EnumItemClass.Block)
                {

                    if (stack.Block is BlockClutter)
                    {
                        Dictionary<string, MeshRef> clutterMeshRefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshRef>>(capi, (stack.Block as BlockShapeFromAttributes).ClassType + "MeshesInventory", () => new Dictionary<string, MeshRef>());
                        string type = stack.Attributes.GetString("type", "");
                        IShapeTypeProps cprops = (stack.Block as BlockShapeFromAttributes).GetTypeProps(type, stack, null);
                        if (cprops == null)
                        {
                            return null;
                        }
                        float rotX = stack.Attributes.GetFloat("rotX", 0f);
                        float rotY = stack.Attributes.GetFloat("rotY", 0f);
                        float rotZ = stack.Attributes.GetFloat("rotZ", 0f);
                        string otcode = stack.Attributes.GetString("overrideTextureCode", null);
                        string hashkey = string.Concat(new string[] { cprops.HashKey,"-",rotX.ToString(),"-",rotY.ToString(),"-",rotZ.ToString(),"-",otcode});
                        MeshRef meshref;
                        if (clutterMeshRefs.TryGetValue(hashkey, out meshref))
                        {
                            modeldata = (stack.Block as BlockShapeFromAttributes).GetOrCreateMesh(cprops, null, otcode);
                            modeldata = modeldata.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotX, rotY, rotZ);
                        }
                    } else
                    {
                        modeldata = coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                    }
                }
                else
                {
                    nowTesselatingObj = stack.Collectible;
                    nowTesselatingShape = null;
                    if (stack.Item.Shape?.Base != null)
                    {
                        nowTesselatingShape = coreClientAPI.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                    }

                    coreClientAPI.Tesselator.TesselateItem(stack.Item, out modeldata, this);
                    modeldata.RenderPassesAndExtraBits.Fill((short)2);
                }
            }

            if (stack.Collectible.Attributes?[AttributeTransformCode].Exists ?? false)
            {
                ModelTransform modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                modelTransform.EnsureDefaultValues();
                modeldata.ModelTransform(modelTransform);
            }
            else if (AttributeTransformCode == "onshelfTransform" && (stack.Collectible.Attributes?["onDisplayTransform"].Exists ?? false))
            {
                ModelTransform modelTransform2 = stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();
                modelTransform2.EnsureDefaultValues();
                modeldata.ModelTransform(modelTransform2);
            }

            if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
            {
                modeldata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), MathF.PI / 2f, 0f, 0f);
                modeldata.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
                modeldata.Translate(0f, -15f / 32f, 0f);
            }
            if (skipCache == false)
            {
                string meshCacheKey = getMeshCacheKey(stack);
                MeshCache[meshCacheKey] = modeldata;
            }

            return modeldata;
        }

        public void SetNowTesselatingObj(CollectibleObject collectible)
        {
            this.nowTesselatingObj = collectible;
        }

        public void SetNowTesselatingShape(Shape shape)
        {
            this.nowTesselatingShape = shape;
        }
    }

}
