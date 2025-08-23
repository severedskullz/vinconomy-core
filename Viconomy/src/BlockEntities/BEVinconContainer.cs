using System;
using System.IO;
using System.Text;
using Viconomy.Filters;
using Viconomy.GUI;
using Viconomy.Inventory.StallSlots;
using Viconomy.Renderer;
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
    public class BEVinconContainer : BEVinconBase
    {

        protected ViconBaseInventory inventory;
        protected bool bypassShelvableAttributes;

        public override InventoryBase Inventory { get { return this.inventory; } }

        public override int DisplayedItems => StallSlotCount;
        protected override bool OverrideBaseShape => inventory.ChiselDecoSlot.Itemstack == null;
        public BEVinconContainer() 
        {
            ConfigureInventory();
            Inventory.SlotModified += Inventory_SlotModified;
        }


        protected void Inventory_SlotModified(int slot)
        {
            UpdateMeshes();
            this.MarkDirty(true, null);
        }

        public virtual void ConfigureInventory()
        {
            ViconItemInventory inv = new ViconItemInventory(this, null, Api, StallSlotCount, StacksPerSlot);
            for (int i = 0; i < StallSlotCount; i++)
            {
                inv.SetSlotFilter(i, ViconomyFilters.IsGenericItem);
                inv.SetSlotBackground(i, "vicon-general");
            }
            inventory = inv;
        }
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            int slotIndex = GetStallSlotForSelectionIndex(blockSel.SelectionBoxIndex);  

            if (slotIndex < 0)
                slotIndex = 0;
            
            //Console.WriteLine("Calling OnPlayerRightClick from " + Api.Side);
            bool shiftMod = byPlayer.Entity.Controls.Sneak;
            bool ctrlMod = byPlayer.Entity.Controls.Sprint;


            ItemSlot hotbarslot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (CanAccess(byPlayer) && !VinconomyCoreSystem.ShouldForceCustomerScreen)
            {
                ItemSlot item = inventory.FindFirstNonEmptyStockSlot(slotIndex);

                if (shiftMod)
                {
                    if (item != null)
                    {
                        ItemSlot currency = inventory.GetCurrencyForStallSlot(slotIndex);
                        if (currency != null && TradingUtil.isMatchingItem(currency.Itemstack, hotbarslot.Itemstack, Api.World))
                        {
                            RequestPurchaseItem(slotIndex, ctrlMod ? BulkPurchaseAmount : 1);
                        }
                        else
                        {
                            TryPut(hotbarslot, slotIndex, ctrlMod);
                        }
                    }
                    else
                    {
                        //Add items to slot
                        TryPut(hotbarslot, slotIndex, ctrlMod);
                    }
                }
                else
                {
                    // Open shop admin gui
                    OpenShopForPlayer(byPlayer, slotIndex);
                }
            }
            else
            {
                if (shiftMod)
                {
                    //Purchase the items.
                    RequestPurchaseItem(slotIndex, ctrlMod ? BulkPurchaseAmount : 1);
                }
                else
                {
                    // Open the shop inventory for that block selection
                    OpenShopForPlayer(byPlayer, slotIndex);
                }
            }
            return true;
        }

        private void TryPurchaseItem(IPlayer player, byte[] data)
        {

            int stallSlot;
            int numPurchases;
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                stallSlot = binaryReader.ReadInt32();
                numPurchases = binaryReader.ReadInt32();
            }

            if (numPurchases <= 0)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.PURCHASED_ZERO, null);
                return;
            }

            //Console.WriteLine(Api.Side + ": We tried to purchase item!");
            //PrintClientMessage(player, Api.Side + ": We tried to purchase item!");

            ItemSlot currency = inventory.GetCurrencyForStallSlot(stallSlot);


            if (currency.Itemstack == null)
            {
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

            ItemSlot[] stockSlots = inventory.GetSlotsForStallSlot(stallSlot);
            ItemSlot purchaseSlot = null;

            //Find the first slot available that we can purchase from
            foreach (var stockSlot in stockSlots)
            {
                if (stockSlot.StackSize >= 0)
                {
                    purchaseSlot = stockSlot;
                    break;
                }
            }
            if (purchaseSlot == null)
            {
                //Console.WriteLine(Api.Side + ": Not enough stock to purchase item");
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_PRODUCT);
                return;
            }

            /*
            if (GetRequiredToolType(stallSlot) != ToolType.NONE && GetRequiredTool(player, stallSlot) == null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_TOOL);
                return;
            }
            */

            if (modSystem.CanPurchaseItem(player, this, register, stallSlot, numPurchases))
            {
                PurchaseItem(player, stallSlot, numPurchases, register);
            }

        }


        #region GUI

        protected void OpenShopForPlayer(IPlayer byPlayer, int selectedStall)
        {

            if (Api.Side == EnumAppSide.Server)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(OwnerName == null ? "" : OwnerName);
                    writer.Write((byte)StallSlotCount);
                    writer.Write((byte)StacksPerSlot);
                    writer.Write((byte)selectedStall);
                    writer.Write(CanAccess(byPlayer));
                    TreeAttribute tree = new TreeAttribute();
                    inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, this.Pos, VinConstants.OPEN_GUI, data);

                if (byPlayer.PlayerUID == this.Owner)
                    byPlayer.InventoryManager.OpenInventory(this.inventory);
            }
        }

        protected virtual void OpenShopGui(byte[] data)
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
                string name = reader.ReadString();
                if (name.Length > 0)
                {
                    dialogTitle = Lang.Get("vinconomy:gui-stall-owner", new string[] { name });
                }
                else
                {
                    dialogTitle = Lang.Get("vinconomy:gui-stall-unowned");
                }
                stallSlots = (int)reader.ReadByte();
                itemsPerStallSlot = (int)reader.ReadByte();
                stallSelection = (int)reader.ReadByte();
                isOwner = reader.ReadBoolean();
                tree.FromBytes(reader);
            }

            this.Inventory.FromTreeAttributes(tree);
            this.Inventory.ResolveBlocksOrItems();
            if (isOwner && !VinconomyCoreSystem.ShouldForceCustomerScreen)
                this.invDialog = new GuiViconStallOwner(dialogTitle, this.Inventory, isOwner, this.Pos, this.Api as ICoreClientAPI, stallSelection);
            else
                this.invDialog = new GuiDialogViconStallCustomer<ViconItemSlot>(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
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

        protected virtual void CloseGui(IClientWorldAccessor clientWorld)
        {
            clientWorld.Player.InventoryManager.CloseInventory(this.Inventory);

            if (invDialog != null)
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

        #endregion


        #region Networking

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedClientPacket " + packetid);
            //PrintClientMessage(player, Api.Side + ": OnRecievedClientPacket");
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            switch (packetid)
            {
                /*
                case VinConstants.OPEN_GUI:
                    if (inventoryManager == null)
                    {
                        return;
                    }
                    inventoryManager.OpenInventory(this.Inventory);
                    break;
                */

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

            ItemSlot slot = this.inventory.StallSlots[stallSlot].currency;
            if (slot.Itemstack != null)
            {
                slot.Itemstack.StackSize = price;
                slot.MarkDirty();
            }
            
        }

        protected void SetStallItemsPerPurchase(IPlayer byPlayer, byte[] data)
        {
            if (!CanAccess(byPlayer))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
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
            //ViconomyCoreSystem.PrintClientMessage(byPlayer, "set quantity to " + amountItems, new object[] { amountItems });
            this.MarkDirty();
        }





        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedServerPacket " + packetid);
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)this.Api.World;
            if (packetid == VinConstants.TOGGLE_GUI)
            {
                if (invDialog != null)
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

        protected void RequestPurchaseItem(int slot, int amount)
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

        #endregion

        protected override void UpdateMesh(int index)
        {
            ItemSlot slot = inventory.FindFirstNonEmptyStockSlot(index);
            if (Api != null && Api.Side != EnumAppSide.Server && slot != null && !slot.Empty)
            {
                getOrCreateMesh(slot.Itemstack, index);
            }
        }

        protected override void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (mesher == null)
                return;

            if (shouldRenderInventory)
            {
                for (int i = 0; i < StallSlotCount; i++)
                {
                    try
                    {
                        ItemSlot slot = inventory.FindFirstNonEmptyStockSlot(i);
                        if (slot != null && !slot.Empty && tfMatrices != null)
                        {
                            MeshData mesh = getOrCreateMesh(slot.Itemstack, i);
                            if (mesh != null)
                            {
                                mesher.AddMeshData(mesh, tfMatrices[i]);
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Had some trouble rendering a mesh in a stall for item");
                    }

                }
            }

        }

        protected virtual void TesselateDecoBlock(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (inventory.ChiselDecoSlot.Itemstack != null)
            {
                ItemStack stack = inventory.ChiselDecoSlot.Itemstack;
                MeshData mesh = modSystem.GetRenderer(stack).createMesh(this, stack, 0);
                mesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, (float)((Block.Shape.rotateY * Math.PI) / 180), 0);
                mesher.AddMeshData(mesh);
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            TesselateDisplayedItems(mesher, tessThreadTesselator);
            TesselateDecoBlock(mesher, tessThreadTesselator);
            return base.OnTesselation(mesher, tessThreadTesselator);
        }

        protected override float[][] GenTransformationMatrices()
        {

            float[][] tfMatrices = new float[StallSlotCount][];
            for (int index = 0; index < StallSlotCount; index++)
            {
                float scale = 0.35f;
                ItemSlot slot = inventory.FindFirstNonEmptyStockSlot(index);
                if (slot != null)
                {
                    if (slot.Itemstack.Collectible.Code.Path.StartsWith("crock")
                        || slot.Itemstack.Collectible.Code.Path.StartsWith("bowl")
                        || slot.Itemstack.Collectible.Code.Path.StartsWith("claypot")
                        || slot.Itemstack.Class != EnumItemClass.Block)
                    {
                        scale = .85f;
                    }
                }
                Cuboidf sb = Block.SelectionBoxes[index];
                float left = .25f - (scale / 2);
                float right = left + .5f;

                float x = (index % 2 == 0) ? left : right;
                float y = sb.YSize <= .45f ? sb.MaxY - 0.37f + (.45f - sb.YSize) : sb.MaxY - 0.37f;
                float z = (index / 2 == 0) ? left : right;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f).Scale(scale, scale, scale);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }

        protected virtual bool TryAddItemToStall(ItemSlot activeSlot, int stallSlot, bool bulk)
        {
            ItemSlot[] slots = inventory.GetSlotsForStallSlot(stallSlot);
            int amountItem = bulk ? activeSlot.Itemstack.StackSize : 1;
            bool movedItems = false;

            for (int i = 0; i < StacksPerSlot; i++)
            {
                if (activeSlot.Itemstack != null)
                {
                    int moved = activeSlot.TryPutInto(this.Api.World, slots[i], amountItem);
                    amountItem -= moved;
                    if (moved > 0)
                    {
                        movedItems = true;
                        activeSlot.MarkDirty();
                        slots[i].MarkDirty();
                    }

                    if (amountItem <= 0)
                    {
                        break;
                    }
                }
            }
            return movedItems;
        }

        protected bool TryPut(ItemSlot slot, int stallSlot, bool bulk)
        {
            if (slot?.Itemstack == null)
            {
                return false;
            }

            if (TryAddItemToStall(slot, stallSlot, bulk))
            {
                MarkDirty(true, null);
                ICoreClientAPI coreClientAPI = this.Api as ICoreClientAPI;
                if (coreClientAPI != null)
                {
                    coreClientAPI.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    UpdateMeshes();
                }
                return true;
            }

            return false;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            int i = 0;
            foreach (StallSlotBase slot in inventory.StallSlots)
            {
                i++;
                ItemSlot stock = slot.FindFirstNonEmptyStockSlot();
                ItemSlot currency = slot.currency;
                if (stock != null && stock.Itemstack != null && currency.Itemstack != null)
                {
                    dsc.AppendLine(Lang.Get("vinconomy:for-sale", new Object[] { i, slot.itemsPerPurchase, slot.GetProductName(Api), currency.Itemstack.StackSize, slot.GetCurrencyName(Api) }));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("vinconomy:not-for-sale", new Object[] { i }));
                }

            }

            base.GetBlockInfo(forPlayer, dsc);
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
            MeshData modeldata = GetMesh(stack);
            if (modeldata != null)
            {
                return modeldata;
            }

            IItemRenderer renderer = modSystem.GetRenderer(stack);
            if (renderer != null)
            {
                modeldata = renderer.createMesh(this, stack, index);

                //Bypass the Display and Shelvable transforms for Armor Stands, where we want the model coordinates to match the character, not the zero'd positions.
                if (!bypassShelvableAttributes)
                {
                    // pick our preselected Attribute Transform Code
                    if (stack.Collectible.Attributes?[AttributeTransformCode].Exists ?? false)
                    {
                        ModelTransform modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                        modelTransform.EnsureDefaultValues();
                        modeldata.ModelTransform(modelTransform);
                    }
                    else if (AttributeTransformCode == "onShelfTransform" && (stack.Collectible.Attributes?["onDisplayTransform"].Exists ?? false))
                    {
                        ModelTransform modelTransform2 = stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();
                        modelTransform2.EnsureDefaultValues();
                        modeldata.ModelTransform(modelTransform2);
                    } else if (stack.Collectible.Attributes?["groundStorageTransform"].Exists ?? false)
                    {
                        ModelTransform modelTransform = stack.Collectible.Attributes?["groundStorageTransform"].AsObject<ModelTransform>();
                        modelTransform.EnsureDefaultValues();
                        modeldata.ModelTransform(modelTransform);
                    }

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

        public override ItemSlot[] GetSlotsForStall(int stallSlot)
        {
            return inventory.StallSlots[stallSlot].GetSlots();
        }

        public override ItemSlot GetCurrencyForStall(int stallSlot)
        {
            return inventory.StallSlots[stallSlot].currency;
        }

        public override int GetNumItemsPerPurchaseForStall(int stallSlot)
        {
            return inventory.StallSlots[stallSlot].itemsPerPurchase;
        }

        public override ItemStack FindFirstNonEmptyStockStack(int stallSlot)
        {
            return inventory.StallSlots[stallSlot].FindFirstNonEmptyStockSlot()?.Itemstack;
        }
    }
}

