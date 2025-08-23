using System;
using System.IO;
using System.Numerics;
using System.Text;
using Viconomy.GUI;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.Slots;
using Viconomy.Renderer;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVinconSculpturePad : BEVinconBase
    {
        
        protected InventoryBase inventory;
        int sizeXZ = 3;
        int sizeY = 3;
        int maxSizeXZ =3;
        int maxSizeY = 3;
        string sculptureName;
        public override InventoryBase Inventory { get { return this.inventory; } }

        public override int DisplayedItems => maxSizeXZ * maxSizeXZ * maxSizeY;

        public BEVinconSculpturePad()
        {
            ConfigureInventory();
            this.inventory.SlotModified += Inventory_SlotModified;
        }

        protected void Inventory_SlotModified(int slot)
        {
            UpdateMeshes();
            this.MarkDirty(true, null);
        }

        public virtual void ConfigureInventory()
        {
            this.inventory = new ViconSculptureInventory((maxSizeXZ * maxSizeXZ * maxSizeY) + 1, null, null);
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
                    RequestPurchaseItem(slotIndex, ctrlMod ? BulkPurchaseAmount : 1);
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
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_PRICE);
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

            

            if (!CanSell())
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_PRODUCT);
                return;
            }


            if (modSystem.CanPurchaseItem(player, this, register, stallSlot, desiredAmount))
            {
                PurchaseItem(player, stallSlot, desiredAmount, register);
            }

        }

        private bool CanSell()
        {
            bool hasAtleastOneForSale = false;
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeXZ; z++)
                {
                    for (int x = 0; x < sizeXZ; x++)
                    {
                        ViconSculptureBlockSlot slot = GetSlotForGrid(x, y, z);
                        if (!slot.isDisabled)
                        {
                            if (slot.Empty)
                            {
                                return false;
                            }
                            else
                            {
                                hasAtleastOneForSale = true;
                            }
                        }
                    }
                }
            }
            return hasAtleastOneForSale;
        }

        public ItemStack GenNewSculptureBundle()
        {
            ItemStack stack = new ItemStack(Api.World.GetItem(new AssetLocation("vinconomy:sculpturebundle")), 1);
            TreeAttribute treeAttr = new TreeAttribute();
            treeAttr.SetString("SculptureName", GetSculptureName());
            treeAttr.SetInt("SizeX", sizeXZ);
            treeAttr.SetInt("SizeY", sizeY);
            treeAttr.SetInt("SizeZ", sizeXZ);
            
            TreeAttribute contents = new TreeAttribute();

            //int i = 0;
            int numBlocks = 0;
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeXZ; z++)
                {
                    for (int x = 0; x < sizeXZ; x++)
                    {
                        ViconSculptureBlockSlot slot = GetSlotForGrid(x, y, z);
                        if (!slot.Empty && !slot.isDisabled)
                        {
                            numBlocks++;
                            contents.SetItemstack(String.Format("{0}-{1}-{2}", x, y, z), TradingUtil.GetItemStackClone(slot, 1));
                            //i++;
                        }
                        
                    }
                }
            }

            treeAttr.SetInt("NumBlocks", numBlocks);
            treeAttr.SetAttribute("Contents", contents);

            stack.Attributes = treeAttr;
            return stack;
        }

        protected override void UpdateStallForSlot(int slot)
        {
            //TODO: Make a Sculpture Bundle of the sculpture items from GenNewSculptureBundle
        }

        public override void PurchaseItem(IPlayer player, int stallSlot, int desiredAmount, BEVinconRegister shopRegister)
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
            request.isAdminShop = this.IsAdminShop;


            TradeResult result = TradingUtil.TryPurchaseItem(request);
            if (result.error != null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, result.error);
            }
            else
            {
                // TradingUtil did not remove the items since we manually set the productSourceSlots to a new inventory.
                // Go back and remove an item
                if (!IsAdminShop)
                {
                    //int i = 0;
                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int z = 0; z < sizeXZ; z++)
                        {
                            for (int x = 0; x < sizeXZ; x++)
                            {
                                ViconSculptureBlockSlot slot = GetSlotForGrid(x, y, z);
                                if (!slot.Empty && !slot.isDisabled)
                                {
                                    slot.TakeOut(1);
                                }
                                //i++;
                            }
                        }
                    }
                }

                this.MarkDirty(true, null);
                this.UpdateMeshes();
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
                    dialogTitle = Lang.Get("vinconomy:gui-stall-owner", new string[] {name});
                } else
                {
                    dialogTitle = Lang.Get("vinconomy:gui-stall-unowned");
                }
                isOwner = reader.ReadBoolean();
                tree.FromBytes(reader);
            }
            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            if (isOwner)
                this.invDialog = new GuiViconSculpturePadOwner(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI);
            else
                this.invDialog = new GuiDialogViconSculpturePadCustomer(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI);
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

                case VinConstants.SET_ITEM_PRICE:
                    int price = 0;
                    int stall = 0;

                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        stall = reader.ReadInt32();
                        price = reader.ReadInt32();
                    }
                    SetItemPrice(player, stall, price);
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

                case VinConstants.SET_SCULPTURE_SLOT:
                    
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        int x = reader.ReadInt32();
                        int y = reader.ReadInt32();
                        int z = reader.ReadInt32();
                        bool disabled = reader.ReadBoolean();
                        SetSlotEnabled(player, x, y, z, disabled);
                        //((ICoreServerAPI) this.Api).Network.BroadcastBlockEntityPacket
                    }
                    break;

                case VinConstants.SET_SCULPTURE_XZ:

                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        int size = reader.ReadInt32();
                        SetSizeXZ(player, size);
                    }
                    break;

                case VinConstants.SET_SCULPTURE_Y:

                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        int size = reader.ReadInt32();
                        SetSizeY(player, size);
                    }
                    break;

                case VinConstants.SET_ITEM_NAME:

                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        SetSculptureName(player, reader.ReadString());
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

        protected void SetItemPrice(IPlayer byPlayer, int stallSlot, int price)
        {
            if (!CanAccess(byPlayer))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            ItemSlot slot = this.inventory[0];
            if (slot.Itemstack != null)
            {
                slot.Itemstack.StackSize = price;
                slot.MarkDirty();
            }

        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("SizeXZ", GetSizeXZ());
            tree.SetInt("SizeY", GetSizeY());
            tree.SetInt("MaxSizeXZ", GetMaxSizeXZ());
            tree.SetInt("MaxSizeY", GetMaxSizeY());
            tree.SetString("SculptureName", sculptureName);
            ITreeAttribute slotTree = tree.GetOrAddTreeAttribute("DisabledSlots");
            for (int y = 0; y < maxSizeY; y++)
            {
                for (int z = 0; z < maxSizeXZ; z++)
                {
                    for (int x = 0; x < maxSizeXZ; x++)
                    {
                        slotTree.SetBool(x + "-" + y + "-" + z, GetSlotForGrid(x, y, z).isDisabled);
                    }
                }
            }

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            sizeXZ = tree.GetInt("SizeXZ", 3);
            sizeY = tree.GetInt("SizeY", 3);
            maxSizeXZ = tree.GetInt("MaxSizeXZ", 5);
            maxSizeY = tree.GetInt("MaxSizeY", 5);
            sculptureName = tree.GetString("SculptureName", "Sculpture");
            ITreeAttribute slotTree = tree.GetOrAddTreeAttribute("DisabledSlots");
            for (int y = 0; y < maxSizeY; y++)
            {
                for (int z = 0; z < maxSizeXZ; z++)
                {
                    for (int x = 0; x < maxSizeXZ; x++)
                    {
                        GetSlotForGrid(x, y, z).isDisabled = slotTree.GetBool(x + "-" + y + "-" + z);
                    }
                }
            }

            ICoreClientAPI coreClientAPI = this.Api as ICoreClientAPI;
            if (coreClientAPI != null)
            {
                UpdateMeshes();
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
                        coreClientAPI.Network.SendBlockEntityPacket(this.Pos, VinConstants.PURCHASE_ITEMS, data);
                        coreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    }
                }
            }
        }

        #endregion

        #region Model
        protected override void UpdateMesh(int index)
        {
            ItemSlot slot = this.inventory[index];
            if (Api != null && Api.Side != EnumAppSide.Server && slot != null && !slot.Empty)
            {
                getOrCreateMesh(slot.Itemstack, index);
            }
        }
        protected override void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            int index = 0;
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeXZ; z++)
                {
                    for (int x = 0; x < sizeXZ; x++)
                    {
                        ViconSculptureBlockSlot slot = GetSlotForGrid(x, y, z);
                        if (slot != null && !slot.Empty && !slot.isDisabled && tfMatrices != null)
                            mesher.AddMeshData(getOrCreateMesh(slot.Itemstack, index), tfMatrices[index]);
                        index++;
                    }
                }
            }
        }

        protected override float[][] GenTransformationMatrices()
        {

            float[][] tfMatrices = new float[DisplayedItems][];
            int i = 0;
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeXZ; z++)
                {
                    for (int x = 0; x < sizeXZ; x++)
                    {
                        // Possible Values
                        // XZ:1 Y:1 = 0 | XZ:1 Y:2 = 0.25 | XZ:1 Y:3 = 1
                        // XZ:2 Y:1 = 0 | XZ:2 Y:2 = 0.00 | XZ:2 Y:3 = 0.5
                        // XZ:3 Y:1 = 0 | XZ:3 Y:2 = 0.00 | XZ:3 Y:3 = 0

                        // XZ:1 Y:3 = 1
                        // XZ:2 Y:3 = 0.5
                        // XZ 1 Y:2 = 0.25


                        float scale = 1.0f / Math.Max(sizeXZ,sizeY);
                        float offsetXZ = 0f;

                        //TODO: There is probably some algorithm to solve for this.
                        if (sizeXZ == 1 && sizeY == 3)
                            offsetXZ = 1;
                        else if (sizeXZ == 1 && sizeY == 2)
                            offsetXZ = 0.5f;
                        else if (sizeXZ == 2 && sizeY == 3)
                            offsetXZ = 0.5f;
                        


                        Matrixf matrix = new Matrixf()
                            .Scale(scale, scale, scale)
                            .Translate(0.5/scale,0,0.5/scale)
                            .RotateYDeg(Block.Shape.rotateY)
                            
                            .Translate(x+ offsetXZ, y+(0.0625f/ scale), z + offsetXZ)
                            .Translate(-0.5 / scale, 0, -0.5 / scale);
                        int index = i;
                        tfMatrices[index] = matrix.Values;
                        i++;
                    }
                }
            }

            return tfMatrices;
        }

        protected override MeshData getOrCreateMesh(ItemStack stack, int index)
        {
            MeshData modeldata = GetMesh(stack);
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
                    string meshCacheKey = GetMeshCacheKey(stack);
                    MeshCache[meshCacheKey] = modeldata;
                }
            }



            return modeldata;
        }

        #endregion

        private void SetSizeY(IPlayer player, int size)
        {
            if (!CanAccess(player))
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            sizeY = size;
            this.MarkDirty(true);
            //genTransformationMatrices();
            ICoreClientAPI coreClientAPI = this.Api as ICoreClientAPI;
            if (coreClientAPI != null)
            {
                UpdateMeshes();
            }

        }

        private void SetSizeXZ(IPlayer player, int size)
        {
            if (!CanAccess(player))
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            sizeXZ = size;
            this.MarkDirty(true);
            //genTransformationMatrices();
            ICoreClientAPI coreClientAPI = this.Api as ICoreClientAPI;
            if (coreClientAPI != null)
            {
                UpdateMeshes();
            }

        }

        private void SetSlotEnabled(IPlayer player, int x, int y, int z, bool disabled)
        {
            if (!CanAccess(player))
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }
            GetSlotForGrid(x, y, z).isDisabled = disabled;
            this.MarkDirty(true);
        }

        private void SetSculptureName(IPlayer player, string name)
        {
            if (!CanAccess(player))
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }
            sculptureName = name;
            this.MarkDirty(true);
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.invDialog != null && this.invDialog.IsOpened())
            {
                this.invDialog.TryClose();
            }
            if (this.invDialog != null)
            {
                this.invDialog.Dispose(); 
            }
            
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {


                ItemSlot currency = inventory[0];
                if (currency.Itemstack != null && CanSell())
                {
                    dsc.AppendLine(Lang.Get("vinconomy:for-sale", new Object[] { 1, 1, GetSculptureName(), currency.Itemstack.StackSize, currency.Itemstack.GetName() }));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("vinconomy:not-for-sale", new Object[] { 1 }));
                }

            
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

        public int GetMaxSizeXZ()
        {
            return maxSizeXZ;
        }

        public int GetMaxSizeY()
        {
            return maxSizeY;
        }

        public ViconSculptureBlockSlot GetSlotForGrid(int x, int y, int z)
        {
            int layerOffset = y * GetMaxSizeXZ() * GetMaxSizeXZ();
            int zOffset = z * GetMaxSizeXZ();
            return (ViconSculptureBlockSlot)this.Inventory[layerOffset + zOffset + x + 1];
        }

        public string GetSculptureName()
        {
            return sculptureName;
        }

    }

}
