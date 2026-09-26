
using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.ModSystems;
using Commercially.Common.Registry;
using Commercially.Common.Util;
using Commercially.Vinconomy.BlockEntityBehaviors;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Util;
using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Commercially.Vinconomy.GUI.Tabs
{
    public class PurchaseStallOwnerTab : ModularTab
    {

        public const string CODE = "Vinconomy.PurchaseStallOwner";
        public override string Code => CODE;

        public override string TabName => Lang.Get("vinconomy:tabname-generic-owner");

        PurchaseStallShopInventory Inventory;
        IStallInventoryProvider StallProvider;
        BEStallBehavior Stall;
        IOwnableRegistry Registry;
        IOwnableChild Ownable;
        int StallSlot;
        int SelectedIndex;

        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            if (Inventory != null && StallProvider != null && Ownable != null)
            {
                CairoFont hoverText = CairoFont.WhiteDetailText();
                CairoFont smallText = CairoFont.WhiteSmallText();
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                string labelText = $"Page {StallSlot + 1} of {StallProvider.StallCount}"; //Lang.Get("vinconomy:gui-slot", new object[] { StallSlot + 1, stall.StallSlotCount });

                OwnableRegistryKeys shopKeys = GUIUtils.GetOwnableDropdownListForOwner(Registry, Ownable);
                SelectedIndex = shopKeys.CurrentSelectedIndex;

                int currencySlotId = GUIUtils.GetCurrencySlotIdForStall(StallProvider, StallSlot);
                int productSlotId = GUIUtils.GetProductSlotIdForStall(StallProvider, StallSlot);
                int internalSlotId = GUIUtils.GetInternalOffsetForStall(StallProvider, StallSlot);

                PurchaseStallSlot stall = StallProvider.GetStallSlot<PurchaseStallSlot>(StallSlot);

                int stockSlotLength = stall.Stock.Length;
                int[] stockSlotIds = GUIUtils.GetSlotIDsForStall(StallProvider, StallSlot);

                int internalSlotLength = stall.GetInternalSlots().Length;
                int[] internalSlotIds = new int[internalSlotLength];
                for (int i = 0; i < internalSlotLength; i++)
                {
                    internalSlotIds[i] = internalSlotId + i;
                }

                int numColumns = 10;
                int slotGridWidth = (int)(10 * (GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding));

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 150).WithFixedOffset(10, GuiStyle.TitleBarHeight + 10);
                settingBounds.BothSizing = ElementSizing.FitToChildren;
                rootBounds.WithChild(settingBounds);

                ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 250, 30);
                ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);
                settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-shop"), smallText, shopSelectionLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-shop"), hoverText, 500, shopSelectionLabel);
                composer.AddDropDown(shopKeys.ShopKeys, shopKeys.ShopNames, SelectedIndex, this.OnShopChanged, shopSelectBounds, "shopSelection");

                ElementBounds priceLabel = ElementBounds.FixedSize(250, 30).FixedUnder(shopSelectBounds,5);
                ElementBounds priceSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(priceLabel);
                ElementBounds priceInputBounds = ElementBounds.FixedSize(75, 30).FixedUnder(priceLabel, 10).FixedRightOf(priceSlotBounds, 10);
                settingBounds.WithChildren(priceLabel, priceSlotBounds, priceInputBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-purchased-stock"), smallText, priceLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-purchased-stock"), hoverText, 500, priceLabel);
                composer.AddItemSlotGrid(Inventory, this.SetCurrencySlot, 1, new int[] { currencySlotId }, priceSlotBounds, "currency");
                composer.AddNumberInput(priceInputBounds, this.OnCostQuantityChanged, smallText, "costQuantity");

                ElementBounds productLabel = ElementBounds.FixedSize(250, 30).FixedUnder(priceSlotBounds, 5);
                ElementBounds productSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(productLabel);
                ElementBounds productInputBounds = ElementBounds.FixedSize(75, 30).FixedUnder(productLabel).FixedRightOf(productSlotBounds).WithFixedOffset(10, 10);
                settingBounds.WithChildren(productLabel, productSlotBounds, productInputBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-currency"), smallText, productLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-returned-currency"), hoverText, 500, productLabel);
                composer.AddItemSlotGrid(Inventory, this.SetProductSlot, 1, new int[] { productSlotId }, productSlotBounds, "product");
                composer.AddNumberInput(productInputBounds, this.OnSellQuantityChanged, smallText, "sellQuantity");


                /*
                ElementBounds chiselLabel = ElementBounds.FixedSize(250, 25).FixedUnder(productSlotBounds, 5);
                ElementBounds chiselSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(chiselLabel);
                settingBounds.WithChildren(chiselLabel, chiselSlotBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-decoration-block"), smallText, chiselLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-decoration-block"), hoverText, 500, chiselLabel);
                composer.AddItemSlotGrid(Inventory, new Action<object>(this.SetCurrencySlot), 1, new int[] { 0 }, chiselSlotBounds, "chisel");
                */

                if (GUIUtils.IsCreativePlayer(ClientApi.World.Player)) {

                    ElementBounds adminShopLabel = ElementBounds.FixedSize(200, 25).FixedUnder(productSlotBounds, 13);
                    ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(productSlotBounds, 10).FixedRightOf(adminShopLabel);
                    settingBounds.WithChildren(adminShopLabel, adminShopBounds);
                    composer.AddSwitch(this.OnToggleAdminShop, adminShopBounds, "admin");
                    composer.AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), smallText, adminShopLabel);
                    composer.AddHoverText(Lang.Get("vinconomy:tooltip-admin-shop"), hoverText, 500, adminShopLabel);
                    composer.GetSwitch("admin").SetValue(Ownable.IsAdminOwned);


                    ElementBounds discardProductLabel = ElementBounds.FixedSize(200, 25).FixedUnder(adminShopLabel,13);
                    ElementBounds discardProductBounds = ElementBounds.FixedSize(40, 40).FixedUnder(adminShopLabel,10).FixedRightOf(discardProductLabel);
                    settingBounds.WithChildren(discardProductLabel, discardProductBounds);
                    composer.AddSwitch(this.OnToggleDiscardProduct, discardProductBounds, "discardProduct");
                    composer.AddStaticText(Lang.Get("vinconomy:gui-discard-product"), smallText, discardProductLabel);
                    composer.AddHoverText(Lang.Get("vinconomy:tooltip-discard-product"), hoverText, 500, discardProductLabel);
                    composer.GetSwitch("discardProduct").SetValue(Inventory.DiscardCurrency);
                }

                ElementBounds pageBounds = ElementBounds.FixedSize(400, 30).FixedRightOf(settingBounds, 15).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                pageBounds.BothSizing = ElementSizing.FitToChildren;
                rootBounds.WithChild(pageBounds);
                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.LeftTop);
                ElementBounds pageLabel = ElementBounds.FixedSize(slotGridWidth - 70, 25).WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0,5);//.FixedRightOf(pagePrev, 10);
                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.RightTop);//.FixedRightOf(pageLabel, 10);
                pageBounds.WithChildren(pagePrev, pageLabel, pageNext);
                composer.AddButton("<", PreviousPage, pagePrev, EnumButtonStyle.Small, "prevPage");
                composer.AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel");
                composer.AddButton(">", NextPage, pageNext, EnumButtonStyle.Small, "nextPage");


                //ElementBounds stallBounds = ElementBounds.FixedSize(250, 200).FixedRightOf(settingBounds, 15).FixedUnder(pageBounds);
                //stallBounds.BothSizing = ElementSizing.FitToChildren;
                //rootBounds.WithChild(stallBounds);

              


                ElementBounds stockLabel = ElementBounds.FixedSize(slotGridWidth, 25).FixedUnder(pagePrev,10);
                pageBounds.WithChildren(stockLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-currency"), labelTextFont, stockLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-returned-currency"), hoverText, 500, stockLabel);

                ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 20, numColumns, (int) Math.Ceiling((float)stockSlotLength /  numColumns)).FixedUnder(stockLabel,-20);
                pageBounds.WithChild(slotGrid);
                composer.AddItemSlotGrid(Inventory, (Gui as GUIModularBlockEntity).SendPacket, 10, stockSlotIds, slotGrid, "currencyInventory");

                ElementBounds purchasedProductLabel = ElementBounds.FixedSize(slotGridWidth, 25).FixedUnder(slotGrid, 10);
                pageBounds.WithChildren(purchasedProductLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-purchased-stock"), labelTextFont, purchasedProductLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-purchased-stock"), hoverText, 500, purchasedProductLabel);

                ElementBounds purchasedSlotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 20, numColumns, (int)Math.Ceiling((float)internalSlotLength / numColumns)).FixedUnder(purchasedProductLabel, -20);
                pageBounds.WithChild(purchasedSlotGrid);
                composer.AddItemSlotGrid(Inventory, (Gui as GUIModularBlockEntity).SendPacket, 10, internalSlotIds, purchasedSlotGrid, "productInventory");
                
                ElementBounds registerFallbackLabel = ElementBounds.FixedSize(210, 25).FixedUnder(purchasedSlotGrid,10);
                pageBounds.WithChild(registerFallbackLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-register-fallback"), smallText, registerFallbackLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-register-fallback"), hoverText, 500, registerFallbackLabel);
                ElementBounds registerFallbackBounds = ElementBounds.FixedSize(40, 40).FixedUnder(purchasedSlotGrid,10).FixedRightOf(registerFallbackLabel);
                pageBounds.WithChild(registerFallbackBounds);
                composer.AddSwitch(new Action<bool>(this.OnToggleRegisterFallback), registerFallbackBounds, "registerFallback");

                ElementBounds limitPurchaseLabel = ElementBounds.FixedSize(210, 25).FixedUnder(purchasedSlotGrid,10).FixedRightOf(registerFallbackBounds,25);
                pageBounds.WithChild(limitPurchaseLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-limit-purchases"), smallText, limitPurchaseLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-limit-purchases"), hoverText, 500, limitPurchaseLabel);
                ElementBounds limitPurchaseBounds = ElementBounds.FixedSize(40, 40).FixedUnder(purchasedSlotGrid,10).FixedRightOf(limitPurchaseLabel,5);
                pageBounds.WithChild(limitPurchaseBounds);
                composer.AddSwitch(new Action<bool>(this.OnToggleLimitPurchases), limitPurchaseBounds, "limitPurchases");

                ElementBounds fuzzyMatchingLabel = ElementBounds.FixedSize(210, 25).FixedUnder(registerFallbackLabel,15);
                pageBounds.WithChild(fuzzyMatchingLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-fuzzy-matching"), smallText, fuzzyMatchingLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-fuzzy-matching"), hoverText, 500, fuzzyMatchingLabel);
                ElementBounds fuzzyMatchingBounds = ElementBounds.FixedSize(40, 40).FixedUnder(registerFallbackLabel,10).FixedRightOf(fuzzyMatchingLabel);
                pageBounds.WithChild(fuzzyMatchingBounds);
                composer.AddSwitch(new Action<bool>(this.OnToggleFuzzyMatching), fuzzyMatchingBounds, "fuzzyMatching");

                ElementBounds numPurchasesSelectionLabel = ElementBounds.FixedSize(170, 30).FixedUnder(limitPurchaseLabel, 15).FixedRightOf(registerFallbackBounds,25);
                pageBounds.WithChild(numPurchasesSelectionLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-purchases-left"), smallText, numPurchasesSelectionLabel);
                ElementBounds numPurchasesSelectionBounds = ElementBounds.FixedSize(75, 30).FixedUnder(limitPurchaseLabel,15).FixedRightOf(numPurchasesSelectionLabel);
                pageBounds.WithChild(numPurchasesSelectionBounds);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-purchases-left"), hoverText, 500, numPurchasesSelectionLabel);
                composer.AddNumberInput(numPurchasesSelectionBounds, new Action<string>(this.OnRemainingPurchasesQuantityChanged), smallText, "numPurchases");

                composer.GetButton("prevPage").Enabled = StallSlot != 0;
                composer.GetButton("nextPage").Enabled = StallSlot != StallProvider.StallCount - 1;

                composer.GetNumberInput("costQuantity").SetValue(Math.Max(1,Inventory.GetStall(StallSlot).Currency.StackSize));
                composer.GetNumberInput("sellQuantity").SetValue(Math.Max(1, Inventory.GetStall(StallSlot).Product.StackSize));

                composer.GetSwitch("registerFallback").SetValue(stall.RegisterFallback);
                composer.GetSwitch("limitPurchases").SetValue(stall.IsLimited);
                composer.GetSwitch("fuzzyMatching").SetValue(stall.IsFuzzyMatching);
                composer.GetSwitch("fuzzyMatching").SetValue(stall.Inventory.DiscardCurrency);
                composer.GetTextInput("numPurchases").SetValue(stall.NumPurchasesRemaining);

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

        private void OnToggleDiscardProduct(bool isToggled)
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
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.SET_SHOULD_DISCARD_CURRENCY, data);
        }

        private void OnRemainingPurchasesQuantityChanged(string amount)
        {
            if (!Gui.Composer.Composed)
                return;

            Int32.TryParse(amount, out int val);

            PurchaseStallSlot stall = StallProvider.GetStallSlot<PurchaseStallSlot>(StallSlot);

            if (val >= 0 && val <= 1024 && val != stall.NumPurchasesRemaining)
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
                ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_PURCHASES_REMAINING, data);
            }
        }

        private void OnToggleFuzzyMatching(bool isToggled)
        {
            if (!Gui.Composer.Composed)
                return;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_FUZZY_MATCHING, data);
        }

        private void OnToggleLimitPurchases(bool isToggled)
        {
            if (!Gui.Composer.Composed)
                return;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_LIMITED_PURCHASES, data);
        }

        private void OnToggleRegisterFallback(bool isToggled)
        {
            if (!Gui.Composer.Composed)
                return;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_REGISTER_FALLBACK, data);
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
            Inventory = entity?.GetBehavior<IInventoryProvider>()?.Inventory as PurchaseStallShopInventory;
            StallProvider = entity?.GetBehavior<IStallInventoryProvider>();
            Stall = entity?.GetBehavior<IStallComponent>() as BEStallBehavior;
            Ownable = entity?.GetBehavior<IOwnableChild>();
            //NumColumns = GetConfiguration()?["NumColumns"].AsInt(10) ?? 10;

            //TODO: Figure out a better way to pass this in - will need it for all of the tabbed GUIs for shops
            GUIModularBlockEntity guiBE = gui as GUIModularBlockEntity;
            if (guiBE != null)
            {
                StallSlot = entity?.GetBehavior<IStallComponent>()?.GetStallIndexFromSelection(guiBE.BlockSelectionIndex) ?? 0;
            }
        }

        public override bool IsVisible()
        {
            return CommUtils.IsLocalPlayerOwner(this.BlockEntity, ClientApi);
        }

        public override void OnGuiClosed()
        {
 
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
