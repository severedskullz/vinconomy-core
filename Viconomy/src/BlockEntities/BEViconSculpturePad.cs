using System;
using System.IO;
using System.Reflection;
using System.Text;
using Viconomy.Filters;
using Viconomy.GUI;
using Viconomy.Inventory;
using Viconomy.Renderer;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;
using static System.Formats.Asn1.AsnWriter;

namespace Viconomy.BlockEntities
{
    public class BEViconSculpturePad : BEViconBase
    {
        
        protected GuiDialogBlockEntity invDialog;
        protected InventoryGeneric inventory;
        int sizeXZ;
        int sizeY;
        public override InventoryBase Inventory { get { return this.inventory; } }

        public override int DisplayedItems => sizeXZ * sizeXZ * sizeY;

        public BEViconSculpturePad()
        {
            sizeXZ = 3;
            sizeY = 3;
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
            this.inventory = new InventoryGeneric((sizeXZ * sizeXZ * sizeY) + 1, null, null, OnNewSlot);
            
            /*for (int i = 0; i < StallSlotCount; i++)
            {
                inventory.SetSlotFilter(i, ViconomyFilters.IsGenericItem);
                inventory.SetSlotBackground(i, "vicon-general");
            }*/
        }

        private ItemSlot OnNewSlot(int slotId, InventoryGeneric self)
        {
            if (slotId == 0)
            {
                return new ViconCurrencySlot(self);
            } else
            {
                return new ViconSculptureBlockSlot(self,slotId);
            }
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            int slotIndex = blockSel.SelectionBoxIndex;
            //Console.WriteLine("Calling OnPlayerRightClick from " + Api.Side);
            bool shiftMod = byPlayer.Entity.Controls.Sneak;
            bool ctrlMod = byPlayer.Entity.Controls.Sprint;

            if (byPlayer.PlayerUID == Owner)
            {
               
                if (shiftMod)
                {
                  
                    ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                    ItemSlot currency = this.inventory[0];
                    if (currency != null && TradingUtil.isMatchingCurrency(currency.Itemstack, handSlot.Itemstack))
                    {
                        RequestPurchaseItem(slotIndex, ctrlMod ? BulkPurchaseAmount : 1);
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
                    RequestPurchaseItem(blockSel.SelectionBoxIndex, ctrlMod ? BulkPurchaseAmount : 1);
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
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                stallSlot = binaryReader.ReadInt32();
                desiredAmount = binaryReader.ReadInt32();
            }


            //Console.WriteLine(Api.Side + ": We tried to purchase item!");
            //PrintClientMessage(player, Api.Side + ": We tried to purchase item!");
            
            ItemSlot currency = this.inventory[0];
            

            if (currency.Itemstack == null) {
                //PrintClientMessage(player, "vinconomy:item-cost", new Object[] { currency.Itemstack.StackSize, currency.Itemstack.GetName() });
                ViconomyCore.PrintClientMessage(player, TradingConstants.NO_PRICE, null);
                return;
            } 
            
            // Does the shop have a register ID set?
            if (this.RegisterID == null && !this.isAdminShop)
            {
                ViconomyCore.PrintClientMessage(player, TradingConstants.NOT_REGISTERED);
                return;
            }

            
            // Is there a shop with the given Register ID?
            BEVRegister register = modSystem.GetShopRegister(this.Owner, this.RegisterID);
            if (register == null && !this.isAdminShop)
            {
                ViconomyCore.PrintClientMessage(player, TradingConstants.NOT_REGISTERED);
                return;
            }
  
            for (int i = 1; i < inventory.Count; i++)
            {
                if (inventory[i].Itemstack == null)
                {
                    ViconomyCore.PrintClientMessage(player, TradingConstants.NO_PRODUCT);
                    return;
                }
            }
           

            if (modSystem.CanPurchaseItem(player, this, register, stallSlot, desiredAmount))
            {
                PurchaseItem(player, stallSlot, desiredAmount, register);
            }

        }

        private ItemStack GenNewSculptureBundle()
        {
            ItemStack stack = new ItemStack(Api.World.GetItem(new AssetLocation("vinconomy:sculpturebundle")), 1);
            TreeAttribute treeAttr = new TreeAttribute();
            treeAttr.SetString("SculptureName", "My Sculpture");
            treeAttr.SetInt("SizeX", sizeXZ);
            treeAttr.SetInt("SizeY", sizeY);
            treeAttr.SetInt("SizeZ", sizeXZ);
            
            TreeAttribute contents = new TreeAttribute();

            int i = 0;
            int numBlocks = 0;
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeXZ; z++)
                {
                    for (int x = 0; x < sizeXZ; x++)
                    {
                        ItemSlot slot = Inventory[i + 1];
                        if (!slot.Empty)
                        {
                            numBlocks++;
                            contents.SetItemstack(String.Format("{0}-{1}-{2}", x, y, z), TradingUtil.GetItemStackClone(Inventory[i + 1], 1));
                            i++;
                        }
                        
                    }
                }
            }

            treeAttr.SetInt("NumBlocks", numBlocks);
            treeAttr.SetAttribute("Contents", contents);

            stack.Attributes = treeAttr;
            return stack;
        }

        public override void PurchaseItem(IPlayer player, int stallSlot, int desiredAmount, BEVRegister shopRegister)
        {
            InventoryGeneric genInv = new InventoryGeneric(1, "purchase-inv" + Inventory.InventoryID, Api);
            genInv[0].Itemstack = GenNewSculptureBundle();

            ItemSlot[] slots = new ItemSlot[] { genInv[0] };

            TradeRequest request = new TradeRequest();
            request.customer = player;
            request.shopRegister = shopRegister;
            request.sellingEntity = this;
            request.productNeeded = slots[0].Itemstack;
            request.productSourceSlots = slots;
            request.numPurchases = desiredAmount;
            request.coreApi = this.Api;
            request.currencyNeeded = TradingUtil.GetItemStackClone(GetCurrencyForStall(stallSlot));
            request.isAdminShop = this.isAdminShop;

            TradeResult result = TradingUtil.TryPurchaseItem(request);
            if (result.error != null)
            {
                ViconomyCore.PrintClientMessage(player, result.error);
            }
            else
            {
                // TradingUtil did not remove the items since we manually set the productSourceSlots to a new inventory.
                // Go back and remove an item
                if (!isAdminShop)
                {
                    int i = 0;
                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int z = 0; z < sizeXZ; z++)
                        {
                            for (int x = 0; x < sizeXZ; x++)
                            {
                                ItemSlot slot = Inventory[i];
                                if (!slot.Empty)
                                {
                                    slot.TakeOut(1);
                                }
                                i++;
                            }
                        }
                    }
                }
               


                this.MarkDirty(true, null);
                this.updateMeshes();
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
                this.invDialog = new GuiViconSculpturePadOwner(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
            else
                this.invDialog = new GuiViconSculpturePadOwner(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
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
            ItemSlot slot = this.inventory[index];
            if (Api != null && Api.Side != EnumAppSide.Server && slot != null && !slot.Empty)
            {
                getOrCreateMesh(slot.Itemstack, index);
            }
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            for (int index = 0; index < DisplayedItems; index++)
            {
                ItemSlot slot = this.inventory[index+1];
                if (slot != null && !slot.Empty && tfMatrices != null)
                    mesher.AddMeshData(getOrCreateMesh(slot.Itemstack, index), tfMatrices[index]);
            }
            return false;
        }

        protected override float[][] genTransformationMatrices()
        {

            float[][] tfMatrices = new float[DisplayedItems][];
            int i = 0;
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeXZ; z++)
                {
                    for (int x = 0; x < sizeXZ; x++)
                    {
                        float scale = 1.0f / sizeY;
                        Matrixf matrix = new Matrixf().RotateYDeg(this.block.Shape.rotateY).Scale(scale, scale,scale).Translate(x, y+0.2f, z);
                        int index = i;
                        tfMatrices[index] = matrix.Values;
                        i++;
                    }
                }
            }

            return tfMatrices;
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
           
            //dsc.AppendLine();
            //base.GetBlockInfo(forPlayer, dsc);
        }

        public override void DropContents(Vec3d atPos)
        {
            this.Inventory.DropAll(atPos, 0);
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

        public override ItemSlot[] GetSlotsForStall(int stallSlot)
        {
            return inventory.Copy(1, DisplayedItems);
        }

        public override ItemSlot GetCurrencyForStall(int stallSlot)
        {
            return inventory[0];
        }

        public override int GetNumItemsPerPurchaseForStall(int stallSlot)
        {
            return 1;
        }

        public int GetSizeXZ()
        {
            return sizeXZ;
        }

        public int GetSizeY()
        {
            return sizeY;
        }
    }

}
