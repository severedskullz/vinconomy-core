using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.StallSlots;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.GUI
{
    public class GuiVinconPurchaseStallOwner : GuiDialogBlockEntity
    {
        BEVinconPurchaseContainer stall;
        ShopRegistration[] registers;
        ICoreClientAPI api;
        int stallSlotCount;
        int curTab;
        bool isOwner;
        PurchaseStallSlot stallSlot;
        ViconItemPurchaseInventory vInventory;

        public GuiVinconPurchaseStallOwner(string DialogTitle, InventoryBase Inventory, bool isOwner,  BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconPurchaseContainer>(BlockEntityPosition);
            curTab = stallSelection;
            VinconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();
            ShopRegistration[] allRegisters = modSystem.GetRegistry().GetShopsForOwner(stall.Owner);
            List<ShopRegistration> filteredRegisters = new List<ShopRegistration>();
            foreach (ShopRegistration register in allRegisters)
            {
                if (register.Position != null)
                {
                    filteredRegisters.Add(register);
                }
            }

            registers = filteredRegisters.ToArray();
            stallSlotCount = stall.StallSlotCount;

            this.isOwner = isOwner;

            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;

            Compose();
        }

        private void Compose()
        {
            try
            {
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                vInventory = Inventory as ViconItemPurchaseInventory;
                int[] productSlots = new int[vInventory.ProductStacksPerStall];
                int pOffset = (curTab * vInventory.StallSlotSize)+1;
                if (vInventory != null)
                {
                    for (int i = 0; i < vInventory.ProductStacksPerStall; i++)
                    {
                        productSlots[i] = pOffset + i;
                    }
                }

                int[] purchasedSlots = new int[vInventory.PurchasedItemStacksPerStall];
                int cOffset = pOffset + vInventory.ProductStacksPerStall;
                if (vInventory != null)
                {
                    for (int i = 0; i < vInventory.PurchasedItemStacksPerStall; i++)
                    {
                        purchasedSlots[i] = cOffset + i;
                    }
                }

                int selectedIndex = 0;
                int shopLength = registers.Length + 1;
                string[] shopsNames = new string[shopLength];
                string[] shopsKeys = new string[shopLength];

                shopsNames[0] = Lang.Get("vinconomy:gui-none");
                shopsKeys[0] = "None";
                for (int i = 0; i < registers.Length; i++)
                {
                    shopsNames[i + 1] = registers[i].Name;
                    shopsKeys[i + 1] = registers[i].ID.ToString();

                    if (stall.RegisterID == registers[i].ID)
                    {
                        selectedIndex = i + 1;
                    }
                }

                stallSlot = (PurchaseStallSlot)vInventory.StallSlots[curTab];

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                //settingBounds.BothSizing = ElementSizing.FitToChildren;

                // Auto-sized dialog at the center of the screen
                //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
                ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
                ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);

                ElementBounds desiredProductLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(0, 15);
                ElementBounds desiredProductSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(desiredProductLabel);

                ElementBounds purchaseLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(125, 15);
                ElementBounds purchaseSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(purchaseLabel).WithFixedOffset(125, 00);

                ElementBounds costSelectionLabel = ElementBounds.FixedSize(170, 30).FixedUnder(desiredProductSlotBounds).WithFixedOffset(0, 15);
                ElementBounds costSelectionBounds = ElementBounds.FixedSize(75, 30).FixedUnder(desiredProductSlotBounds).FixedRightOf(costSelectionLabel).WithFixedOffset(5, 10);

                ElementBounds quantitySelectionLabel = ElementBounds.FixedSize(170, 30).FixedUnder(costSelectionLabel).WithFixedOffset(0, 15);
                ElementBounds quantitySelectionBounds = ElementBounds.FixedSize(75, 30).FixedUnder(costSelectionLabel).FixedRightOf(quantitySelectionLabel).WithFixedOffset(5, 10);

                ElementBounds registerFallbackLabel = ElementBounds.FixedSize(210, 25).FixedUnder(quantitySelectionLabel).WithFixedOffset(0, 15);
                ElementBounds registerFallbackBounds = ElementBounds.FixedSize(40, 40).FixedUnder(quantitySelectionLabel).FixedRightOf(registerFallbackLabel).WithFixedOffset(10, 10);

                ElementBounds fuzzyMatchingLabel = ElementBounds.FixedSize(210, 25).FixedUnder(registerFallbackLabel).WithFixedOffset(0, 15);
                ElementBounds fuzzyMatchingBounds = ElementBounds.FixedSize(40, 40).FixedUnder(registerFallbackLabel).FixedRightOf(fuzzyMatchingLabel).WithFixedOffset(10, 10);

                ElementBounds limitPurchaseLabel = ElementBounds.FixedSize(210, 25).FixedUnder(fuzzyMatchingLabel).WithFixedOffset(0, 15);
                ElementBounds limitPurchaseBounds = ElementBounds.FixedSize(40, 40).FixedUnder(fuzzyMatchingLabel).FixedRightOf(limitPurchaseLabel).WithFixedOffset(10, 10);
               
                ElementBounds numPurchasesSelectionLabel = ElementBounds.FixedSize(170, 30).FixedUnder(limitPurchaseLabel).WithFixedOffset(0, 15);
                ElementBounds numPurchasesSelectionBounds = ElementBounds.FixedSize(75, 30).FixedUnder(limitPurchaseLabel).FixedRightOf(numPurchasesSelectionLabel).WithFixedOffset(5, 10);

                ElementBounds chiselLabel = ElementBounds.FixedSize(200, 25).FixedUnder(numPurchasesSelectionLabel).WithFixedOffset(0, 15);
                ElementBounds chiselSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(chiselLabel);

                ElementBounds adminShopLabel = ElementBounds.FixedSize(210, 25).FixedUnder(chiselSlotBounds).WithFixedOffset(0, 15);
                ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(chiselSlotBounds).FixedRightOf(adminShopLabel).WithFixedOffset(10, 10);

                ElementBounds discardProductLabel = ElementBounds.FixedSize(210, 25).FixedUnder(adminShopLabel).WithFixedOffset(0, 15);
                ElementBounds discardProductBounds = ElementBounds.FixedSize(40, 40).FixedUnder(adminShopLabel).FixedRightOf(discardProductLabel).WithFixedOffset(10, 10);

                settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel, quantitySelectionBounds, quantitySelectionLabel, desiredProductLabel, desiredProductSlotBounds, purchaseSlotBounds, adminShopBounds, adminShopLabel);
                settingBounds.verticalSizing = ElementSizing.FitToChildren;

                // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
                //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

                ElementBounds itemPage = ElementBounds.FixedSize(200, 10).FixedRightOf(settingBounds).WithFixedOffset(25, GuiStyle.TitleBarHeight);
                itemPage.BothSizing = ElementSizing.FitToChildren;


                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithFixedPosition(0, 0);
                //ElementBounds pageLabel = ElementBounds.FixedSize(50, 20).WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0,15).WithFixedPadding(75,0);
                ElementBounds pageLabel = ElementBounds.FixedSize(430, 25).WithFixedAlignmentOffset(0, 10).FixedRightOf(pagePrev, 10);
                //ElementBounds pageNext = ElementBounds.FixedSize(50, 50).WithAlignment(EnumDialogArea.RightTop);
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                string labelText = Lang.Get("vinconomy:gui-slot", new object[] { curTab + 1, stall.StallSlotCount });
                labelTextFont.AutoBoxSize(labelText, pageLabel, true);

                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).FixedRightOf(pageLabel, 10);
                ElementBounds productSlotLabel = ElementBounds.FixedSize(500, 25).FixedUnder(pagePrev).WithFixedOffset(5, 15);
                ElementBounds productSlotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 20, 10, (int)Math.Ceiling(productSlots.Length / 10.0)).FixedUnder(productSlotLabel).WithFixedOffset(0, -15);

                ElementBounds currencySlotLabel = ElementBounds.FixedSize(500, 25).FixedUnder(productSlotGrid).WithFixedOffset(5, 15);
                ElementBounds currencySlotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 20, 10,(int)Math.Ceiling(purchasedSlots.Length / 10.0)).FixedUnder(currencySlotLabel).WithFixedOffset(0,-15);
               
                itemPage.WithChildren(pagePrev, pageLabel, pageNext, currencySlotLabel, currencySlotGrid, productSlotLabel, productSlotGrid);



                bgBounds.WithChildren(itemPage, settingBounds);

                //IconUtil.DrawArrowRight

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconStallOwner", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                SingleComposer.BeginChildElements(settingBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-shop"), CairoFont.WhiteSmallText(), shopSelectionLabel)
                    .AddDropDown(shopsKeys, shopsNames, selectedIndex, new SelectionChangedDelegate(this.onSelectionChanged), shopSelectBounds, "shopSelection")

                    .AddStaticText(Lang.Get("vinconomy:gui-product"), CairoFont.WhiteSmallText(), desiredProductLabel)
                    .AddItemSlotGrid(vInventory, new Action<object>(this.SetDesiredProductSlot), 1, new int[] { cOffset + vInventory.PurchasedItemStacksPerStall }, desiredProductSlotBounds, "desiredProduct")

                    .AddStaticText(Lang.Get("vinconomy:gui-currency"), CairoFont.WhiteSmallText(), purchaseLabel)
                    .AddItemSlotGrid(vInventory, new Action<object>(this.SetReturnedCurrencySlot), 1, new int[] { pOffset + vInventory.StallSlotSize - 1 }, purchaseSlotBounds, "returnedCurrency")


                    .AddStaticText(Lang.Get("vinconomy:gui-items-per-purchase"), CairoFont.WhiteSmallText(), costSelectionLabel)
                    .AddNumberInput(costSelectionBounds, new Action<string>(this.onProductQuantityChanged), CairoFont.WhiteSmallText(), "desiredProductQuantity")

                    .AddStaticText(Lang.Get("vinconomy:gui-cost-per-purchase"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                    .AddNumberInput(quantitySelectionBounds, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "returnedCurrencyQuantity")

                    .AddStaticText(Lang.Get("vinconomy:gui-register-fallback"), CairoFont.WhiteSmallText(), registerFallbackLabel)
                    .AddSwitch(new Action<bool>(this.OnToggleRegisterFallback), registerFallbackBounds, "registerFallback")

                    .AddStaticText(Lang.Get("vinconomy:gui-limit-purchases"), CairoFont.WhiteSmallText(), limitPurchaseLabel)
                    .AddSwitch(new Action<bool>(this.OnToggleLimitPurchases), limitPurchaseBounds, "limitPurchases")

                    .AddStaticText(Lang.Get("vinconomy:gui-fuzzy-matching"), CairoFont.WhiteSmallText(), fuzzyMatchingLabel)
                    .AddSwitch(new Action<bool>(this.OnToggleFuzzyMatching), fuzzyMatchingBounds, "fuzzyMatching")

                    .AddStaticText(Lang.Get("vinconomy:gui-purchases-left"), CairoFont.WhiteSmallText(), numPurchasesSelectionLabel)
                    .AddNumberInput(numPurchasesSelectionBounds, new Action<string>(this.onRemainingPurchasesQuantityChanged), CairoFont.WhiteSmallText(), "numPurchases")

                    //.AddButton("Save", new ActionConsumable(this.onSave),saveButtonBounds, EnumButtonStyle.Small, "save")
                  
                    .AddStaticText(Lang.Get("vinconomy:gui-decoration-block"), CairoFont.WhiteSmallText(), chiselLabel )
                    .AddItemSlotGrid(vInventory, new Action<object>(this.SendInvPacket), 1, new int[] {0}, chiselSlotBounds, "chisel")
                    .AddIf(stall.IsAdminShop || VinUtils.IsCreativePlayer(api.World.Player))
                        .AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), CairoFont.WhiteSmallText(), adminShopLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleAdminShop), adminShopBounds, "admin")
                        .AddStaticText(Lang.Get("vinconomy:gui-discard-product"), CairoFont.WhiteSmallText(), discardProductLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleDiscardCurrency), discardProductBounds, "discardProduct")
                    .EndIf()

                //.AddItemSlotGrid(inv, null, 1, new int[] { 0 }, purchaseSlotBounds, "purchase")
                //.AddPassiveItemSlot(outputSlotBounds, Inventory, )
                .EndChildElements();

                SingleComposer.BeginChildElements(itemPage)
                    .AddButton("<", new ActionConsumable(this.PreviousPage), pagePrev, EnumButtonStyle.Small, "prevPage")
                    .AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel")
                    .AddButton(">", new ActionConsumable(this.NextPage), pageNext, EnumButtonStyle.Small, "nextPage")
                    .AddStaticText(Lang.Get("vinconomy:gui-currency"), CairoFont.WhiteSmallText(), productSlotLabel)
                    .AddItemSlotGrid(vInventory, new Action<object>(this.SendInvPacket), 10, productSlots, productSlotGrid, "productSlots")
                    .AddStaticText(Lang.Get("vinconomy:gui-purchased-stock"), CairoFont.WhiteSmallText(), currencySlotLabel)
                    .AddItemSlotGrid(vInventory, new Action<object>(this.SendInvPacket), 10, purchasedSlots, currencySlotGrid, "currencySlots")

                .EndChildElements();

                if (curTab == 0)
                    SingleComposer.GetButton("prevPage").Enabled = false;

                if (curTab == stallSlotCount - 1)
                    SingleComposer.GetButton("nextPage").Enabled = false;

                if (stall.IsAdminShop || VinUtils.IsCreativePlayer(api.World.Player))
                {
                    SingleComposer.GetSwitch("admin").SetValue(stall.IsAdminShop);
                    SingleComposer.GetSwitch("discardProduct").SetValue(stall.DiscardProduct);
                }
                    

                SingleComposer.GetDropDown("shopSelection").Enabled = isOwner;


                SingleComposer.GetTextInput("desiredProductQuantity").SetValue(stallSlot.DesiredProduct.StackSize);
                SingleComposer.GetTextInput("returnedCurrencyQuantity").SetValue(stallSlot.Currency.StackSize);
                SingleComposer.GetSwitch("registerFallback").SetValue(stall.RegisterFallback);
                SingleComposer.GetSwitch("limitPurchases").SetValue(stallSlot.LimitedPurchases);
                SingleComposer.GetSwitch("fuzzyMatching").SetValue(stallSlot.FuzzyMatching);
                SingleComposer.GetTextInput("numPurchases").SetValue(stallSlot.NumTradesLeft);


                //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
                SingleComposer.Compose();

            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }

        private void onRemainingPurchasesQuantityChanged(string amount)
        {
            int val = 1;
            Int32.TryParse(amount, out val);
            val = Math.Max(0, val);
            val = Math.Min(1024, val);

           
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(curTab);
                    writer.Write(val);
                    data = ms.ToArray();
                }
                capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_PURCHASES_REMAINING, data);
        }

        private void onProductQuantityChanged(string txt)
        {
            if (txt.Length == 0)
                return;

            int val = 1;
            Int32.TryParse(txt, out  val);

            if (val > 0 && val <= 1024 && val != stallSlot.DesiredProduct.StackSize)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(curTab);
                    writer.Write(val);
                    data = ms.ToArray();
                }
                capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_ITEM_PRICE, data);

                if (stallSlot.DesiredProduct.Itemstack != null)
                {
                    stallSlot.DesiredProduct.Itemstack.StackSize = val;
                }
            }

        }
        private void onCostQuantityChanged(string amount)
        {
            int val = 1;
            Int32.TryParse(amount, out val);

            if (val > 0 && val <= 1024 && val != stallSlot.Currency.StackSize)
            {
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(curTab);
                    writer.Write(val);
                    data = ms.ToArray();
                }
                capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_ITEMS_PER_PURCHASE, data);

                if (stallSlot.Currency.Itemstack != null)
                {
                    stallSlot.Currency.Itemstack.StackSize = val;
                }

            }
        }

        private void OnToggleFuzzyMatching(bool isToggled)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(curTab);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_FUZZY_MATCHING, data);
        }

        private void OnToggleRegisterFallback(bool isToggled)
        {
            VinUtils.SendSingleBool(capi, BlockEntityPosition, VinConstants.SET_REGISTER_FALLBACK, isToggled);
        }

        private void OnToggleLimitPurchases(bool isToggled)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(curTab);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_LIMITED_PURCHASES, data);
        }

        private void OnToggleDiscardCurrency(bool isToggled)
        {
            VinUtils.SendSingleBool(capi, BlockEntityPosition, VinConstants.SET_ADMIN_DISCARD_CURRENCY, isToggled);
        }

        private void OnToggleAdminShop(bool isToggled)
        {
            VinUtils.SendSingleBool(capi, BlockEntityPosition, VinConstants.SET_ADMIN_SHOP, isToggled);
        }

        private bool PreviousPage()
        {
            curTab -= 1;
            curTab = Math.Max(0, curTab);
            this.Compose();
            return true;
        }

        private bool NextPage()
        {
            curTab += 1;
            curTab = Math.Min(stallSlotCount-1, curTab);
            this.Compose();
            return true;
        }



        private void onSelectionChanged(string code, bool selected)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                int id = code.ToInt(-1);
                writer.Write(id);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_REGISTER_ID, data);

        }

        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void SetReturnedCurrencySlot(object p)
        {
            SingleComposer.GetTextInput("returnedCurrencyQuantity").SetValue(stallSlot.Currency.StackSize);
            SendInvPacket(p);
        }

        private void SetDesiredProductSlot(object p)
        {
            SingleComposer.GetTextInput("desiredProductQuantity").SetValue(stallSlot.DesiredProduct.StackSize);
            SendInvPacket(p);
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

    }
}
