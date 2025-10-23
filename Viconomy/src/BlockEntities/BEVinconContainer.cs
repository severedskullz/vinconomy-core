using System;
using System.IO;
using System.Numerics;
using System.Text;
using Viconomy.Filters;
using Viconomy.GUI;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.StallSlots;
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
            ViconItemInventory inv = new ViconItemInventory(this, null, Api, StallSlotCount, ProductStacksPerSlot);
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

        protected virtual void TryPurchaseItem(IPlayer player, int stallSlot, int numPurchases)
        {

            if (numPurchases <= 0)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.PURCHASED_ZERO);
                return;
            }

            ItemSlot currency = inventory.GetCurrencyForStallSlot(stallSlot);

            if (currency.Itemstack == null)
            {
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

        protected virtual GuiDialogBlockEntity GetCustomerGui(string dialogTitle, int stallSelection)
        {
            return new GuiViconStallCustomer(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
        }

        protected virtual GuiDialogBlockEntity GetOwnerGui(string dialogTitle, bool isOwner, int stallSelection)
        {
            return new GuiViconStallOwner(dialogTitle, this.Inventory, isOwner, this.Pos, this.Api as ICoreClientAPI, stallSelection);
        }

        protected virtual void OpenShopForPlayer(IPlayer byPlayer, int selectedStall)
        {

            if (Api.Side == EnumAppSide.Server)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(OwnerName == null ? "" : OwnerName);
                    writer.Write((byte)StallSlotCount);
                    writer.Write((byte)ProductStacksPerSlot);
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
                    dialogTitle = Lang.Get("vinconomy:gui-stall-owner", [name]);
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
                this.invDialog = GetOwnerGui(dialogTitle, isOwner, stallSelection);
            else
                this.invDialog = GetCustomerGui(dialogTitle, stallSelection);
            //this.invDialog.OpenSound = this.OpenSound;
            //this.invDialog.CloseSound = this.CloseSound;
            this.invDialog.TryOpen();
            this.invDialog.OnClosed += delegate ()
            {
                this.invDialog = null;
                capi.Network.SendBlockEntityPacket(this.Pos, VinConstants.CLOSE_GUI, null);
            };
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
            int stallSlot;
            int amount;
            switch (packetid)
            {
                case VinConstants.CLOSE_GUI:
                        inventoryManager?.CloseInventory(this.Inventory);
                    break;

                case VinConstants.PURCHASE_ITEMS:

                    using (MemoryStream memoryStream = new MemoryStream(data))
                    {
                        BinaryReader binaryReader = new BinaryReader(memoryStream);
                        stallSlot = binaryReader.ReadInt32();
                        amount = binaryReader.ReadInt32();
                    }
                    TryPurchaseItem(player, stallSlot, amount);
                    break;

                case VinConstants.SET_ITEMS_PER_PURCHASE:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        stallSlot = (int)reader.ReadInt32();
                        amount = (int)reader.ReadInt32();
                    }
                    SetStallItemsPerPurchase(player, stallSlot, amount);
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
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        stallSlot = reader.ReadInt32();
                        amount = reader.ReadInt32();
                    }
                    SetItemPrice(player, stallSlot, amount);
                    break;

                case VinConstants.SET_ADMIN_DISCARD_CURRENCY:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        isAdmin = reader.ReadBoolean();
                    }
                    SetDiscardProduct(player, isAdmin);
                    break;

                default:
                    if (packetid < 1000)
                    {
                        if (!CanAccess(player))
                        {
                            if (!((ICoreServerAPI)Api).Server.IsDedicated)
                            {
                                VinconomyCoreSystem.PrintClientMessage(player, "Nice Try, but that isn't yours... If this wasn't singleplayer, you would have been kicked.");
                            }
                            else
                            {
                                ((IServerPlayer)player).Disconnect("Nice try, but that wasn't yours. (Tried to access Stall they didn't own)");
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

        public virtual void SetDiscardProduct(IPlayer byPlayer, bool discard)
        {
            if (byPlayer.PlayerUID != this.Owner)
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN);
                return;
            }

            if (!byPlayer.HasPrivilege("gamemode"))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.NO_PRIVLEGE);
                return;
            }

            this.DiscardProduct = discard;
            this.MarkDirty();

        }

        protected virtual void SetItemPrice(IPlayer byPlayer, int stallSlot, int price)
        {
            if (!CanAccess(byPlayer))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN);
                return;
            }

            ItemSlot slot = this.inventory.StallSlots[stallSlot].Currency;
            if (slot.Itemstack != null)
            {
                slot.Itemstack.StackSize = price;
                slot.MarkDirty();
            }
            
        }

        protected virtual void SetStallItemsPerPurchase(IPlayer byPlayer, int stallSlot, int numItems)
        {
            if (!CanAccess(byPlayer))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN);
                return;
            }

            this.inventory.StallSlots[stallSlot].ItemsPerPurchase = numItems;
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
                        modSystem.Mod.Logger.Error($"Had some trouble rendering  mesh in a stall @ {Pos.X} {Pos.Y} {Pos.Z} for slot {i}");
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
                Cuboidf sb = Block.SelectionBoxes[index];
                float left = -.25f;
                float right = left + .5f;

                float x = (index % 2 == 0) ? left : right;
                float y = sb.YSize <= .45f ? sb.MaxY - 0.39f + (.45f - sb.YSize) : sb.MaxY - 0.39f;
                float z = (index / 2 == 0) ? left : right;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }

        protected virtual bool TryAddItemToStall(ItemSlot activeSlot, int stallSlot, bool bulk)
        {
            ItemSlot[] slots = inventory.GetSlotsForStallSlot(stallSlot);
            int amountItem = bulk ? activeSlot.Itemstack.StackSize : 1;
            bool movedItems = false;

            for (int i = 0; i < ProductStacksPerSlot; i++)
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
                ItemSlot currency = slot.Currency;
                if (stock != null && stock.Itemstack != null && currency.Itemstack != null)
                {
                    dsc.AppendLine(Lang.Get("vinconomy:for-sale", new Object[] { i, slot.ItemsPerPurchase, slot.GetProductName(Api), currency.Itemstack.StackSize, slot.GetCurrencyName(Api) }));
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

                if (modeldata == null)
                {
                    // Dont crash, please!
                    return modeldata;
                }

                //Bypass the Display and Shelvable transforms for Armor Stands, where we want the model coordinates to match the character, not the zero'd positions.
                if (!bypassShelvableAttributes)
                {
                    ModelTransform modelTransform = null;
                    // pick our preselected Attribute Transform Code
                    if (stack.Collectible.Attributes?[AttributeTransformCode].Exists ?? false)
                    {
                        modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                    }
                    else if (stack.Block is IShelvable)
                    {
                        modelTransform = (stack.Block as IShelvable).GetOnShelfTransform(stack);
                    }
                    else if (stack.Collectible.Attributes?["onDisplayTransform"].Exists ?? false)
                    {
                        modelTransform = stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();

                    } else if (stack.Collectible.Attributes?["groundStorageTransform"].Exists ?? false)
                    {
                        modelTransform = stack.Collectible.Attributes?["groundStorageTransform"].AsObject<ModelTransform>();

                    }

                    if (modelTransform != null)
                    {
                        modelTransform.EnsureDefaultValues();
                        modeldata.ModelTransform(modelTransform);
                    }


                    // Should be handled by IShelvable, but I still see it in the JSON
                    else if (stack.Collectible.Attributes?["shelvable"].Exists ?? false)
                    {
                        modeldata.Scale(new Vec3f(0.5f, 0.0f, 0.5f), 0.85f, 0.85f, 0.85f);
                    } else
                    {
                        modeldata.Scale(new Vec3f(0.5f, 0.0f, 0.5f), 0.35f, 0.35f, 0.35f);
                    }

                }

                if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
                {
                    modeldata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), MathF.PI / 2f, 0f, 0f);
                    modeldata.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.35f, 0.35f, 0.35f);
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
            return inventory.StallSlots[stallSlot].Currency;
        }

        public override int GetNumItemsPerPurchaseForStall(int stallSlot)
        {
            return inventory.StallSlots[stallSlot].ItemsPerPurchase;
        }

        public override ItemStack FindFirstNonEmptyStockStack(int stallSlot)
        {
            return inventory.StallSlots[stallSlot].FindFirstNonEmptyStockSlot()?.Itemstack;
        }
    }
}

