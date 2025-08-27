using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Viconomy.GUI;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.Slots;
using Viconomy.Inventory.StallSlots;
using Viconomy.Trading;
using Viconomy.Trading.TradeHandlers;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockEntities
{
    public class BEVinconPurchaseContainer : BEVinconContainer
    {
        public bool RegisterFallback;

        //Amount of purchased product from users
        private int PurchasedProductStacksPerSlot = 30;

        //Amount of Money available to pay to players
        public override int ProductStacksPerSlot => 20;

        public override int StallSlotCount => 2;
        protected override bool OverrideBaseShape => inventory.ChiselDecoSlot.Itemstack == null;
        public BEVinconPurchaseContainer() 
        {
            ConfigureInventory();
            Inventory.SlotModified += Inventory_SlotModified;
        }

        public override void ConfigureInventory()
        {
            inventory = new ViconItemPurchaseInventory(this, null, Api, StallSlotCount, ProductStacksPerSlot, PurchasedProductStacksPerSlot);
        }


 
        protected override void TryPurchaseItem(IPlayer player, int stallSlot, int numPurchases)
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

        public override void PurchaseItem(IPlayer player, int stallSlot, int numPurchases, BEVinconRegister shopRegister)
        {
            GenericTradeRequest request = new GenericTradeRequest(Api, player);

            ItemStack returnedCurrency = GetCurrencyForStall(stallSlot).Itemstack;
            ItemStack desiredProduct = GetDesiredProductForStall(stallSlot).Itemstack;
            request.WithShop(shopRegister, this, stallSlot, IsAdminShop);
            request.WithPurchases(numPurchases);
            request.WithCurrency(desiredProduct, TradingUtil.GetAllValidSlotsFor(player, desiredProduct), desiredProduct.StackSize);
            request.WithProduct(returnedCurrency, GetAggregateProductSlots(stallSlot), returnedCurrency.StackSize);

            AggregatedSlots coupons = TradingUtil.GetCouponsSlotsFor(player, request.ProductStackNeeded, shopRegister);
            if (coupons.Slots.Count > 0)
            {
                request.WithCoupons(coupons.Slots[0]);
            }

            //request.WithTools(null, 1);
            if (shopRegister != null)
            {
                ItemStack tradePass = shopRegister.Inventory[0].Itemstack;
                if (tradePass != null)
                {
                    request.WithTradePass(tradePass, TradingUtil.GetAllValidSlotsFor(player, tradePass));
                }
            }


            request.Build();

            GenericTradeResult result = PurchaseStallTradeHandler.TryPurchaseItem(request);
            if (result.Error != null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, result.Error);
            }
            else
            {
                MarkDirty(true, null);
                UpdateMeshes();
            }
        }

        private ItemSlot GetDesiredProductForStall(int stallSlot)
        {
            return ((PurchaseStallSlot)inventory.StallSlots[stallSlot]).DesiredProduct;
        }


        #region GUI

        protected override void OpenShopForPlayer(IPlayer byPlayer, int selectedStall)
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

        protected override void OpenShopGui(byte[] data)
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
                    dialogTitle = Lang.Get("vinconomy:gui-stall-owner",  [name] );
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
                this.invDialog = new GuiVinconPurchaseStallOwner(dialogTitle, this.Inventory, isOwner, this.Pos, this.Api as ICoreClientAPI, stallSelection);
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
        #endregion


        #region Networking

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


        #endregion

        protected override void UpdateMesh(int index)
        {
            ItemSlot slot = ((PurchaseStallSlot)inventory.StallSlots[index]).DesiredProduct;
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
                        PurchaseStallSlot stall = (PurchaseStallSlot)inventory.StallSlots[i];
                        ItemSlot slot = stall.DesiredProduct;

                        bool isNotLimited = !stall.LimitedPurchases || stall.NumTradesLeft > 0;
                        bool isNotEmpty = slot != null && !slot.Empty;

                        if (isNotLimited && isNotEmpty && tfMatrices != null)
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
                PurchaseStallSlot stall = (PurchaseStallSlot)inventory.StallSlots[index];
                ItemSlot slot = stall.DesiredProduct;
                if (slot?.Itemstack != null)
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
                float left = .265f - (scale / 2);
                float right = left + .47f;

                float x = (index % 2 == 0) ? left : right;
                float y = 1;
                float z = (index / 2 == 0) ? left : right;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f).Scale(scale, scale, scale);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            int i = 0;
            foreach (StallSlotBase slot in inventory.StallSlots)
            {
                i++;
                PurchaseStallSlot ps = (PurchaseStallSlot)slot;
                ItemSlot currency = ps.Currency;
                ItemSlot product = ps.DesiredProduct;
                if (currency != null && currency.Itemstack != null && product != null && product.Itemstack != null)
                {
                    dsc.AppendLine(Lang.Get("vinconomy:for-purchase", [ i,  product.Itemstack.StackSize, slot.GetCurrencyName(Api), currency.Itemstack.StackSize, slot.GetProductName(Api) ]));
                }
                else
                {
                    dsc.AppendLine(Lang.Get("vinconomy:not-for-sale", [ i ]));
                }

            }

        }

        protected override void SetItemPrice(IPlayer byPlayer, int stallSlot, int price)
        {
            if (!CanAccess(byPlayer))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN);
                return;
            }

            ItemSlot slot = ((PurchaseStallSlot) inventory.StallSlots[stallSlot]).DesiredProduct;
            if (slot.Itemstack != null)
            {
                slot.Itemstack.StackSize = price;
                slot.MarkDirty();
            }
            this.MarkDirty();
        }

        protected override void SetStallItemsPerPurchase(IPlayer byPlayer, int stallSlot, int numItems)
        {
            if (!CanAccess(byPlayer))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN);
                return;
            }

            ItemSlot slot = ((PurchaseStallSlot)inventory.StallSlots[stallSlot]).Currency;
            if (slot.Itemstack != null)
            {
                slot.Itemstack.StackSize = numItems;
                slot.MarkDirty();
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("RegisterFallback", RegisterFallback);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            RegisterFallback = tree.GetBool("RegisterFallback");
        }

         public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedClientPacket " + packetid);
            //PrintClientMessage(player, Api.Side + ": OnRecievedClientPacket");
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            int stallSlot;
            int amount;
            bool value;
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
                case VinConstants.SET_PURCHASES_REMAINING:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        stallSlot = reader.ReadInt32();
                        amount = reader.ReadInt32();
                    }
                    SetPurchasesRemaining(stallSlot, amount);
                    break;
                case VinConstants.SET_REGISTER_FALLBACK:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        value = reader.ReadBoolean();
                    }
                    RegisterFallback = value;
                    this.MarkDirty();
                    break;
                case VinConstants.SET_LIMITED_PURCHASES:
                    using (MemoryStream ms = new MemoryStream(data))
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        stallSlot = reader.ReadInt32();
                        value = reader.ReadBoolean();
                    }
                    SetLimitedPurchases(stallSlot, value);
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

        private void SetLimitedPurchases(int stallSlot, bool value)
        {
            PurchaseStallSlot slot = (PurchaseStallSlot)((ViconItemPurchaseInventory)inventory).StallSlots[stallSlot];
            slot.LimitedPurchases = value;
            this.MarkDirty();
        }

        private void SetPurchasesRemaining(int stallSlot, int amount)
        {
            PurchaseStallSlot slot = (PurchaseStallSlot)((ViconItemPurchaseInventory)inventory).StallSlots[stallSlot];
            slot.NumTradesLeft = amount;
            this.MarkDirty();
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(BlockSelection selection, IPlayer forPlayer)
        {
            List<WorldInteraction> interactions = new List<WorldInteraction>();

            int index = GetStallSlotForSelectionIndex(selection.SelectionBoxIndex);
            PurchaseStallSlot slot = ((PurchaseStallSlot)inventory.StallSlots[index]);


            ItemStack currency = slot.DesiredProduct.Itemstack;
            ItemStack product = slot.Currency.Itemstack;

            //StallSlot slot = slots[selection.SelectionBoxIndex];

            if (Owner != forPlayer.PlayerUID || VinconomyCoreSystem.ShouldForceCustomerScreen)
            {
                if (currency != null && product != null)
                {
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = [currency]

                    });

                    ItemStack fiveStack = currency.Clone();
                    fiveStack.StackSize = 5 * fiveStack.StackSize;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase-bulk",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sneak", "sprint"],
                        Itemstacks = [fiveStack]
                    });
                }
            }
            else
            {
                if (product != null)
                {
                    ItemStack helpSlot = product.Clone();
                    helpSlot.StackSize = 1;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-add",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = [helpSlot]
                    });

                    ItemStack helpSlotStack = helpSlot.Clone();
                    helpSlotStack.StackSize = helpSlotStack.Collectible.MaxStackSize;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-add",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sneak", "sprint"],
                        Itemstacks = [helpSlotStack]
                    });

                    if (currency != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = [currency]

                        });

                        ItemStack fiveStack = currency.Clone();
                        fiveStack.StackSize = 5 * fiveStack.StackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase-bulk",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = ["sneak", "sprint"],
                            Itemstacks = [fiveStack]
                        });
                    }
                }
                else
                {
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-add",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak"
                    });
                }
            }

            interactions.Add(new WorldInteraction
            {
                ActionLangCode = "vinconomy:stall-open-menu",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = null
            });

            return interactions.ToArray();
        }

        protected override bool TryAddItemToStall(ItemSlot activeSlot, int stallSlot, bool bulk)
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
    }
}

