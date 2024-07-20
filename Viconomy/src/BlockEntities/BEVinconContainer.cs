using System;
using System.IO;
using System.Text;
using Viconomy.Filters;
using Viconomy.GUI;
using Viconomy.Inventory;
using Viconomy.Renderer;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVinconContainer : BEVinconBase
    {

        protected GuiDialogBlockEntity invDialog;
        protected ViconomyInventory inventory;
        protected bool bypassShelvableAttributes;

        public override InventoryBase Inventory { get { return this.inventory; } }

        public override int DisplayedItems => StallSlotCount;

        public BEVinconContainer() 
        {
            ShouldRenderDisplayedItems = false;
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

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            int slotIndex = blockSel.SelectionBoxIndex;
            //Console.WriteLine("Calling OnPlayerRightClick from " + Api.Side);
            bool shiftMod = byPlayer.Entity.Controls.Sneak;
            bool ctrlMod = byPlayer.Entity.Controls.Sprint;


            ItemSlot hotbarslot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (byPlayer.PlayerUID == Owner)
            {
                ItemSlot item = this.inventory.FindFirstNonEmptyStockSlot(slotIndex);


                if (shiftMod)
                {
                    if (item != null)
                    {
                        ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                        ItemSlot currency = this.inventory.GetCurrencyForSelection(slotIndex);
                        if (currency != null && TradingUtil.isMatchingItem(currency.Itemstack, handSlot.Itemstack, byPlayer.Entity.World))
                        {
                            RequestPurchaseItem(slotIndex, ctrlMod ? BulkPurchaseAmount : 1);
                        }
                        else
                        {
                            TryPut(hotbarslot, blockSel, ctrlMod);
                        }
                    }
                    else
                    {
                        //Add items to slot
                        TryPut(hotbarslot, blockSel, ctrlMod);
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
                if (shiftMod)
                {
                    //Purchase the items.
                    RequestPurchaseItem(blockSel.SelectionBoxIndex, ctrlMod ? BulkPurchaseAmount : 1);
                }
                else
                {
                    // Open the shop inventory for that block selection
                    OpenShopForPlayer(byPlayer, blockSel.SelectionBoxIndex);
                }
            }
            return true;
        }

        private void TryPurchaseItem(IPlayer player, byte[] data)
        {

            int stallSlot;
            int desiredAmount;
            using (MemoryStream memoryStream = new MemoryStream(data))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                stallSlot = binaryReader.ReadInt32();
                desiredAmount = binaryReader.ReadInt32();
            }

            if (desiredAmount <= 0)
            {
                ViconomyCoreSystem.PrintClientMessage(player, TradingConstants.PURCHASED_ZERO, null);
                return;
            }

            //Console.WriteLine(Api.Side + ": We tried to purchase item!");
            //PrintClientMessage(player, Api.Side + ": We tried to purchase item!");

            ItemSlot currency = this.inventory.GetCurrencyForSelection(stallSlot);


            if (currency.Itemstack == null)
            {
                //PrintClientMessage(player, "vinconomy:item-cost", new Object[] { currency.Itemstack.StackSize, currency.Itemstack.GetName() });
                ViconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_PRICE, null);
                return;
            }

            // Does the shop have a register ID set?
            if (this.RegisterID == -1 && !this.IsAdminShop)
            {
                ViconomyCoreSystem.PrintClientMessage(player, TradingConstants.NOT_REGISTERED);
                return;
            }


            // Is there a shop with the given Register ID?
            BEVinconRegister register = modSystem.GetShopRegister(this.Owner, this.RegisterID);
            if (register == null && !this.IsAdminShop)
            {
                ViconomyCoreSystem.PrintClientMessage(player, TradingConstants.COULDNT_GET_REGISTER);
                return;
            }

            ItemSlot[] stockSlots = this.inventory.GetSlotsForSelection(stallSlot);
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
                ViconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_PRODUCT);
                return;
            }

            if (modSystem.CanPurchaseItem(player, this, register, stallSlot, desiredAmount))
            {
                PurchaseItem(player, stallSlot, desiredAmount, register);
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

                if (byPlayer.PlayerUID == this.Owner)
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
                this.invDialog = new GuiViconStallOwner(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
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
            //Console.WriteLine(Api.Side + ": Attempted to close GUI");
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


        protected void SetItemPrice(IPlayer byPlayer, int stallSlot, int price)
        {
            if (byPlayer.PlayerUID != this.Owner)
            {
                ViconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
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
            if (byPlayer.PlayerUID != this.Owner)
            {
                ViconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
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

        #endregion

        protected override void updateMesh(int index)
        {
            ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(index);
            if (Api != null && Api.Side != EnumAppSide.Server && slot != null && !slot.Empty)
            {
                getOrCreateMesh(slot.Itemstack, index);
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (mesher == null)
                return false;

            if (shouldRenderInventory)
            {
                for (int i = 0; i < StallSlotCount; i++)
                {
                    try
                    {
                        ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(i);
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


            return base.OnTesselation(mesher, tesselator);
        }


        protected override float[][] genTransformationMatrices()
        {

            float[][] tfMatrices = new float[StallSlotCount][];
            for (int index = 0; index < StallSlotCount; index++)
            {
                float scale = 0.35f;
                ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(index);
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



            if (movedItems)
            {
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


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.invDialog != null)
            {
                if (invDialog.IsOpened())
                {
                    this.invDialog.TryClose();
                }

                this.invDialog.Dispose();
                this.invDialog = null;
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
            int i = 0;
            foreach (StallSlot slot in inventory.StallSlots)
            {
                i++;
                ItemSlot stock = slot.FindFirstNonEmptyStockSlot();
                ItemSlot currency = slot.currency;
                if (stock != null && stock.Itemstack != null && currency.Itemstack != null)
                {
                    dsc.AppendLine(Lang.Get("vinconomy:for-sale", new Object[] { i, slot.itemsPerPurchase, stock.Itemstack.GetName(), currency.Itemstack.StackSize, currency.Itemstack.GetName() }));
                }
                else
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

            IItemRenderer renderer = modSystem.GetRenderer(stack);
            if (renderer != null)
            {
                modeldata = renderer.createMesh(this, stack, index);

                //Bypass the Display and Shelvable transforms for Armor Stands, where we want the model coordinates to match the character, not the zero'd positions.
                if (!bypassShelvableAttributes)
                {
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
            return inventory.StallSlots[stallSlot].slots;
        }

        public override ItemSlot GetCurrencyForStall(int stallSlot)
        {
            return inventory.StallSlots[stallSlot].currency;
        }

        public override int GetNumItemsPerPurchaseForStall(int stallSlot)
        {
            return inventory.StallSlots[stallSlot].itemsPerPurchase;
        }
    }
}

