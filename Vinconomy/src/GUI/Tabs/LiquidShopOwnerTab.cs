
using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.Inventory.Slots;
using Commercially.Common.ModSystems;
using Commercially.Common.Registry;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Commercially.Vinconomy.GUI.Tabs
{
    public class LiquidShopOwnerTab : ModularTab
    {

        public const string CODE = "Vinconomy.LiquidOwner";
        public override string Code => CODE;

        public override string TabName => Lang.Get("vinconomy:tabname-generic-owner");

        LiquidShopInventory Inventory;
        DummyInventory ProductInv;
        IStallInventoryProvider StallProvider;
        IOwnableRegistry Registry;
        private IOwnableChild Ownable;
        int StallSlot;

        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            if (Inventory != null && StallProvider != null && Ownable != null)
            {
                CairoFont hoverText = CairoFont.WhiteDetailText();
                CairoFont smallText = CairoFont.WhiteSmallText();
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                string labelText = $"Page {StallSlot + 1} of {StallProvider.StallCount}"; //Lang.Get("vinconomy:gui-slot", new object[] { StallSlot + 1, stall.StallSlotCount });

                OwnableRegistration[] ownables = Registry.GetOwnablesForOwner(Ownable.OwnerUID, Ownable.GetAllowedParentTypes());
                int shopLength = ownables.Length;
                OwnableRegistryKeys shops = GUIUtils.GetOwnableDropdownListForOwner(Registry, Ownable);

                // Figure out the slot indexes for SlotGrid
                LiquidStallSlot stall = StallProvider.GetStallSlot<LiquidStallSlot>(StallSlot);
                int stallSlotOffset = GUIUtils.GetOffsetForStall(StallProvider, StallSlot);
                ProductInv[0].Itemstack = Inventory.GetStall(StallSlot).GetStockSlot(0).Itemstack?.Clone();
                

                int slotGridWidth = (int) (GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding);
                int sectionPageHeaderWidth = Math.Max(slotGridWidth, 250);
                int sectionFullHeaderWidth = sectionPageHeaderWidth + 80; // 2 x (30w buttons + 10w paddings) for < and > buttons is 80

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 150).WithFixedOffset(10, GuiStyle.TitleBarHeight+10);
                settingBounds.BothSizing = ElementSizing.FitToChildren;
                rootBounds.WithChild(settingBounds);

                ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
                ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);
                settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-shop"), smallText, shopSelectionLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-shop"), hoverText, 500, shopSelectionLabel);
                composer.AddDropDown(shops.ShopKeys, shops.ShopNames, shops.CurrentSelectedIndex, this.OnShopChanged, shopSelectBounds, "shopSelection");

                ElementBounds chiselLabel = ElementBounds.FixedSize(200, 25).FixedUnder(shopSelectBounds,15);
                ElementBounds chiselSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(chiselLabel);
                settingBounds.WithChildren(chiselLabel, chiselSlotBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-decoration-block"), smallText, chiselLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-decoration-block"), hoverText, 500, chiselLabel);
                composer.AddItemSlotGrid(Inventory, new Action<object>(this.SetCurrencySlot), 1, new int[] { 0 }, chiselSlotBounds, "chisel");

                if (GUIUtils.IsCreativePlayer(ClientApi.World.Player))
                {
                    ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(chiselSlotBounds).WithFixedOffset(0, 10);
                    ElementBounds adminShopLabel = ElementBounds.FixedSize(100, 25).FixedUnder(chiselSlotBounds).FixedRightOf(adminShopBounds).WithFixedOffset(0, 10);
                    settingBounds.WithChildren(adminShopLabel, adminShopBounds);
                    composer.AddSwitch(this.OnToggleAdminShop, adminShopBounds, "admin");
                    composer.AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), smallText, adminShopLabel);
                    composer.AddHoverText(Lang.Get("vinconomy:tooltip-admin-shop"), hoverText, 500, adminShopLabel);
                    composer.GetSwitch("admin").SetValue(Ownable.IsAdminOwned);
                }

                ElementBounds pageBounds = ElementBounds.FixedSize(250, 30).FixedRightOf(settingBounds, 15).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(pageBounds);
                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30);
                ElementBounds pageLabel = ElementBounds.FixedSize(sectionPageHeaderWidth, 25).WithFixedAlignmentOffset(0, 5).FixedRightOf(pagePrev, 10);
                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).FixedRightOf(pageLabel, 10);
                pageBounds.WithChildren(pagePrev, pageLabel, pageNext);
                composer.AddButton("<", PreviousPage, pagePrev, EnumButtonStyle.Small, "prevPage");
                composer.AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel");
                composer.AddButton(">", NextPage, pageNext, EnumButtonStyle.Small, "nextPage");


                ElementBounds stallBounds = ElementBounds.FixedSize(250, 200).FixedRightOf(settingBounds, 15).FixedUnder(pageBounds,10);
                stallBounds.BothSizing = ElementSizing.FitToChildren;
                rootBounds.WithChild(stallBounds);
                //composer.AddInset(stallBounds);

                ElementBounds priceLabel = ElementBounds.FixedSize(100, 30).WithFixedOffset(5,10);
                ElementBounds priceSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).WithFixedOffset(5, 0).FixedUnder(priceLabel);
                ElementBounds priceInputBounds = ElementBounds.FixedSize(75, 30).FixedUnder(priceLabel).FixedRightOf(priceSlotBounds).WithFixedOffset(10, 10);
                stallBounds.WithChildren(priceLabel, priceSlotBounds, priceInputBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-price"), smallText, priceLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-price"), hoverText, 500, priceLabel);
                composer.AddItemSlotGrid(Inventory, this.SetCurrencySlot, 1, new int[] { stallSlotOffset }, priceSlotBounds, "currency");
                composer.AddNumberInput(priceInputBounds, this.OnCostQuantityChanged, smallText, "costQuantity");

                ElementBounds productLabel = ElementBounds.FixedSize(100, 30).FixedRightOf(priceLabel, 80).WithFixedOffset(0,10);
                ElementBounds productSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(productLabel).FixedRightOf(priceLabel, 80);
                ElementBounds productInputBounds = ElementBounds.FixedSize(75, 30).FixedUnder(productLabel).FixedRightOf(productSlotBounds).WithFixedOffset(10, 10);
                stallBounds.WithChildren(productLabel, productSlotBounds, productInputBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-product"), smallText, productLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-product"), hoverText, 500, productLabel);
                composer.AddItemSlotGrid(Inventory, this.SetProductSlot, 1, new int[] { stallSlotOffset + 1 }, productSlotBounds, "product");
                composer.AddNumberInput(productInputBounds, this.OnSellQuantityChanged, smallText, "sellQuantity");


                ElementBounds stockLabel = ElementBounds.FixedSize(sectionFullHeaderWidth, 25).FixedUnder(productSlotBounds, 15);
                stallBounds.WithChildren(stockLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-product"), labelTextFont, stockLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-product"), hoverText, 500, stockLabel);

                ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.CenterTop, 0, 20, 1, 1).FixedUnder(stockLabel,-20);
                stallBounds.WithChild(slotGrid);
                composer.AddItemSlotGrid(ProductInv, null, 1, new int[] { 0 }, slotGrid, "inventory");

                ElementBounds transferLabel = ElementBounds.FixedSize(sectionFullHeaderWidth, 25).FixedUnder(slotGrid, 15);
                stallBounds.WithChildren(transferLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-transfer-product"), labelTextFont, transferLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-transfer-product"), hoverText, 500, transferLabel);

                ElementBounds tranIn = ElementBounds.FixedSize(40, 40).FixedUnder(transferLabel).WithFixedOffset(60,0);
                stallBounds.WithChild(tranIn);
                composer.AddButton("^", TransferIn, tranIn, EnumButtonStyle.Small, "transferIn");
                ElementBounds tranInBulk = ElementBounds.FixedSize(40, 40).FixedUnder(transferLabel).FixedRightOf(tranIn);
                stallBounds.WithChild(tranInBulk);
                composer.AddButton("^^", TransferInBulk, tranInBulk, EnumButtonStyle.Small, "transferInBulk");

                ElementBounds tranGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 20, 1, 1).FixedUnder(transferLabel, -20).FixedRightOf(tranInBulk);
                stallBounds.WithChild(tranGrid);
                composer.AddItemSlotGrid(Inventory, (Gui as GUIModularBlockEntity).SendPacket, 1, new int[] { 1}, tranGrid, "transferInventory");

                ElementBounds tranOut = ElementBounds.FixedSize(40, 40).FixedUnder(transferLabel).FixedRightOf(tranGrid);
                stallBounds.WithChild(tranOut);
                composer.AddButton("v", TransferOut, tranOut, EnumButtonStyle.Small, "transferOut");
                ElementBounds tranOutBulk = ElementBounds.FixedSize(40, 40).FixedUnder(transferLabel).FixedRightOf(tranOut);
                stallBounds.WithChild(tranOutBulk);
                composer.AddButton("vv", TransferOutBulk, tranOutBulk, EnumButtonStyle.Small, "transferOutBulk");


                stallBounds.WithChildren(tranIn, tranInBulk, tranGrid, tranOut, tranOutBulk);


                composer.GetButton("prevPage").Enabled = StallSlot != 0;
                composer.GetButton("nextPage").Enabled = StallSlot != StallProvider.StallCount - 1;

                composer.GetNumberInput("costQuantity").SetValue(Math.Max(1,Inventory.GetStall(StallSlot).Currency.StackSize));
                composer.GetNumberInput("sellQuantity").SetValue(Math.Max(1, Inventory.GetStall(StallSlot).Product.StackSize));

            }
            else if (StallProvider == null)
            {
                ElementBounds textBounds = ElementBounds.FixedSize(400, 300).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(textBounds);
                composer.AddStaticText(Lang.Get("vinconomy:not-a-shop"), CairoFont.WhiteSmallText(), textBounds);
            }
            else
            {
                ElementBounds textBounds = ElementBounds.FixedSize(400, 300).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(textBounds);
                composer.AddStaticText(Lang.Get("commercially:container-no-inventory"), CairoFont.WhiteSmallText(), textBounds);
            }
        }

        private bool TransferInBulk()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(10);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.TRANSFER_CONTENTS, data);
            return true;
        }

        private bool TransferIn()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(1);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.TRANSFER_CONTENTS, data);
            return true;
        }

        private bool TransferOutBulk()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(-10);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.TRANSFER_CONTENTS, data);
            return true;
        }

        private bool TransferOut()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(-1);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.TRANSFER_CONTENTS, data);
            return true;
        }

        private bool PreviousPage()
        {
            StallSlot -= 1;
            StallSlot = Math.Max(0, StallSlot);
            //Gui.Composer.GetButton("prevPage").Enabled = StallSlot != 0;
            //Gui.Composer.GetButton("nextPage").Enabled = StallSlot != StallProvider.StallCount - 1;
            Gui.FullRecompose();
            return true;
        }

        private bool NextPage()
        {
            StallSlot += 1;
            StallSlot = Math.Min(StallProvider.StallCount - 1, StallSlot);
            //Gui.Composer.GetButton("prevPage").Enabled = StallSlot != 0;
            //Gui.Composer.GetButton("nextPage").Enabled = StallSlot != StallProvider.StallCount - 1;
            Gui.FullRecompose();
            return true;
        }

        private void OnToggleAdminShop(bool isToggled)
        {
            if (!Gui.Composer.Composed)
                return;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.SET_ADMIN_OWNED, data);
        }

        private void SetProductSlot(object obj)
        {
            BaseStallSlot stall = StallProvider.GetStallSlot(StallSlot);
            Gui.Composer.GetTextInput("sellQuantity").SetValue(stall.ProductPerPurchase);
            Gui.SendPacket(obj);
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos.X, BlockEntity.Pos.Y, BlockEntity.Pos.Z, obj);
        }

        private void SetCurrencySlot(object obj)
        {
            BaseStallSlot stall = StallProvider.GetStallSlot(StallSlot);
            Gui.Composer.GetTextInput("costQuantity").SetValue(stall.CurrencyPerPurchase);
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos.X, BlockEntity.Pos.Y, BlockEntity.Pos.Z, obj);
        }

        private void OnSellQuantityChanged(string amount)
        {
            if (!Gui.Composer.Composed)
                return;

            Int32.TryParse(amount, out int val);

            BaseStallSlot stall = StallProvider.GetStallSlot(StallSlot);

            if (val > 0 && val <= 1024 && val != stall.ProductPerPurchase)
            {
                stall.ProductPerPurchase = val;
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(StallSlot);
                    writer.Write(val);
                    data = ms.ToArray();
                }
                ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.SET_ITEMS_PER_PURCHASE, data);
            }
        }

        private void OnCostQuantityChanged(string amount)
        {
            if (!Gui.Composer.Composed)
                return;

            Int32.TryParse(amount, out int val);

            BaseStallSlot stall = StallProvider.GetStallSlot(StallSlot);

            if (val > 0 && val <= 1024 && val != stall.CurrencyPerPurchase)
            {
                stall.CurrencyPerPurchase = val;
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(StallSlot);
                    writer.Write(val);
                    data = ms.ToArray();
                }
                ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.SET_ITEM_PRICE, data);
            }
        }

        private void OnShopChanged(string code, bool selected)
        {
            if (!Gui.Composer.Composed)
                return;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                int id = code.ToInt(-1);
                writer.Write(id);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.SET_PARENT_ID, data);
        }

        public override void Initialize(IModularGui gui, BlockEntity entity = null)
        {
            base.Initialize(gui, entity);
            Registry = ClientApi.ModLoader.GetModSystem<CommerciallyModSystem>().OwnableRegistry;
            Inventory = entity?.GetBehavior<IInventoryProvider>()?.Inventory as LiquidShopInventory;
            Inventory.OnStockUpdated += OnUpdateContents;
            StallProvider = entity?.GetBehavior<IStallInventoryProvider>();
            Ownable = entity?.GetBehavior<IOwnableChild>();
            //NumColumns = GetConfiguration()?["NumColumns"].AsInt(10) ?? 10;

            ProductInv = new DummyInventory(ClientApi,1);
            ProductInv.TakeLocked = true;
            ProductInv.PutLocked = true;
            ProductInv[0] = new ItemLockedSlot(ProductInv);

            //TODO: Figure out a better way to pass this in - will need it for all of the tabbed GUIs for shops
            GUIModularBlockEntity guiBE = gui as GUIModularBlockEntity;
            if (guiBE != null)
            {
                StallSlot = entity?.GetBehavior<IStallComponent>()?.GetStallIndexFromSelection(guiBE.BlockSelectionIndex) ?? 0;
            }
        }

        private void OnUpdateContents(IStallComponent shop, int stallSlot, ItemStack product, int stockCount, ItemStack currency)
        {
            if (stallSlot == StallSlot)
            {
                LiquidStallSlot stall = Inventory.GetStall<LiquidStallSlot>(stallSlot);
                ProductInv[0].Itemstack = stall.Liquid.Itemstack?.Clone();
            }
        }

        public override bool IsVisible()
        {
            return CommUtils.IsLocalPlayerOwner(this.BlockEntity, ClientApi);
        }

        public override void OnGuiClosed()
        {
            Inventory.OnStockUpdated -= OnUpdateContents;
        }

        public override void OnGuiOpened()
        {

        }

        public override void OnRecievedData(byte[] data)
        {

        }

        public override byte[] OnSendData(BlockEntity entity, Caller caller, BlockSelection blockSel, string key)
        {
            return null;
        }
    }
}
