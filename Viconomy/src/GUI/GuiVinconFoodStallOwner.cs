using System;
using System.Collections.Generic;
using System.IO;
using Vinconomy.BlockEntities;
using Vinconomy.Inventory.Impl;
using Vinconomy.Inventory.Slots;
using Vinconomy.Inventory.StallSlots;
using Vinconomy.Registry;
using Vinconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vinconomy.GUI
{
    public class GuiVinconFoodStallOwner : GuiDialogBlockEntity
    {
        BEVinconBase stall;
        ShopRegistration[] registers;
        ICoreClientAPI api;
        int curTab;
        DummyInventory inv;
        VinconLockedSlot purchaseSlot;
        MealStallSlot stallSlot;

        public GuiVinconFoodStallOwner(string DialogTitle, InventoryBase Inventory, bool isOwner, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconBase>(BlockEntityPosition);
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


            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;

            inv = new DummyInventory(capi,7);
            inv.TakeLocked = true;
            inv.PutLocked = true;
            inv[0] = purchaseSlot = new VinconLockedSlot(inv, 0);
            for (int i = 1; i < 7; i++)
            {
                inv[i] = new VinconLockedSlot(inv, 0);
            }

            Compose();
        }

        public void UpdateIngredients()
        {
            ItemStack[] contents = stallSlot.GetMealContents();

            int[] uiSlots = [1, 2, 3, 4, 5, 6];
            int offset = curTab * (stall.ProductStacksPerSlot + 1) + 2;

            for (int i = 0; i < 6; i++)
            {
                if (i < contents.Length)
                    inv[i + 1].Itemstack = contents[i];
                else
                    inv[i + 1].Itemstack = null;
            }
        }

        private void Compose()
        {
            try
            {
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                VinconMealInventory vinInv = Inventory as VinconMealInventory;
                vinInv.SlotModified += UpdateIngEvent;
                stallSlot = vinInv.GetStall<MealStallSlot>(curTab);
                int offset = curTab * (stall.ProductStacksPerSlot + 1) + 2;


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

                    if (stall.ShopId == registers[i].ID)
                    {
                        selectedIndex = i + 1;
                    }
                }
                UpdateIngredients();
                UpdatePurchaseSlot();

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
                ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);

                ElementBounds currencyLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(0, 15);
                ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel);

                ElementBounds purchaseLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(125, 15);
                ElementBounds purchaseSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(purchaseLabel).WithFixedOffset(125, 00);

                ElementBounds costSelectionLabel = ElementBounds.FixedSize(150, 30).FixedUnder(currencySlotBounds).WithFixedOffset(0, 15);
                ElementBounds costSelectionBounds = ElementBounds.FixedSize(75, 30).FixedUnder(currencySlotBounds).FixedRightOf(costSelectionLabel).WithFixedOffset(25, 10);

                ElementBounds quantitySelectionLabel = ElementBounds.FixedSize(150, 30).FixedUnder(costSelectionLabel).WithFixedOffset(0, 15);
                ElementBounds quantitySelectionBounds = ElementBounds.FixedSize(75, 30).FixedUnder(costSelectionLabel).FixedRightOf(quantitySelectionLabel).WithFixedOffset(25, 10);

                ElementBounds chiselLabel = ElementBounds.FixedSize(200, 25).FixedUnder(quantitySelectionLabel).WithFixedOffset(0, 15);
                ElementBounds chiselSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(chiselLabel);

                ElementBounds adminShopLabel = ElementBounds.FixedSize(100, 25).FixedUnder(chiselSlotBounds).WithFixedOffset(0, 15);
                ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(chiselSlotBounds).FixedRightOf(adminShopLabel).WithFixedOffset(120, 10);

                settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel, quantitySelectionBounds, quantitySelectionLabel, currencyLabel, currencySlotBounds, purchaseSlotBounds, adminShopBounds, adminShopLabel);
                settingBounds.verticalSizing = ElementSizing.FitToChildren;

                ElementBounds itemPage = ElementBounds.FixedSize(200, 10).FixedRightOf(settingBounds).WithFixedOffset(25, GuiStyle.TitleBarHeight);
                itemPage.BothSizing = ElementSizing.FitToChildren;


                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithFixedPosition(0, 0);
                ElementBounds pageLabel = ElementBounds.FixedSize(225, 25).WithFixedAlignmentOffset(0, 10).FixedRightOf(pagePrev, 10);

                string labelText = Lang.Get("vinconomy:gui-slot",[curTab + 1, stall.StallSlotCount ]);

                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).FixedRightOf(pageLabel, 10);
                ElementBounds servingsLabel = ElementBounds.FixedSize(300, 25).FixedUnder(pagePrev).WithFixedOffset(0,15);

                ElementBounds ingredientsLabel = ElementBounds.FixedSize(300, 25).FixedUnder(servingsLabel).WithFixedOffset(0, 15);
                ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 6, 1).FixedUnder(ingredientsLabel);
                ElementBounds slotGrid1 = ElementBounds.FixedSize(51, 50).FixedUnder(ingredientsLabel).WithParent(itemPage);
                ElementBounds slotGrid2 = slotGrid1.RightCopy().WithParent(itemPage);
                ElementBounds slotGrid3 = slotGrid2.RightCopy().WithParent(itemPage);
                ElementBounds slotGrid4 = slotGrid3.RightCopy().WithParent(itemPage);
                ElementBounds slotGrid5 = slotGrid4.RightCopy().WithParent(itemPage);
                ElementBounds slotGrid6 = slotGrid5.RightCopy().WithParent(itemPage);

                ElementBounds servingModLabel = ElementBounds.FixedSize(300, 25).FixedUnder(slotGrid).WithFixedOffset(0, 15);

                ElementBounds transferTo1Bounds = ElementBounds.FixedSize(45, 45).FixedUnder(servingModLabel).WithFixedOffset(10, 0);
                ElementBounds transferTo5Bounds = ElementBounds.FixedSize(45, 45).FixedUnder(servingModLabel).FixedRightOf(transferTo1Bounds).WithFixedOffset(10, 0);
                ElementBounds transferSlot = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(servingModLabel).FixedRightOf(transferTo5Bounds).WithFixedOffset(18,-2);
                ElementBounds transferFrom1Bounds = ElementBounds.FixedSize(45, 45).FixedUnder(servingModLabel).FixedRightOf(transferSlot).WithFixedOffset(15,0);
                ElementBounds transferFrom5Bounds = ElementBounds.FixedSize(45, 45).FixedUnder(servingModLabel).FixedRightOf(transferFrom1Bounds).WithFixedOffset(10, 0);
                //ElementBounds debugBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 5, 3).FixedUnder(transferFrom5Bounds);

                itemPage.WithChildren(pagePrev, pageLabel, pageNext, servingsLabel,ingredientsLabel, slotGrid, servingModLabel, transferSlot, transferFrom1Bounds, transferFrom5Bounds, transferTo1Bounds, transferTo5Bounds);
                bgBounds.WithChildren(itemPage, settingBounds);

                CairoFont labelTextCenteredFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                CairoFont labelTextLeftFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Left);
                CairoFont labelLargeTextLeftFont = CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Left);
                CairoFont hoverText = CairoFont.WhiteDetailText();

                SingleComposer = capi.Gui.CreateCompo("ViconMealStallOwner" + Inventory.InventoryID, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);


                SingleComposer.BeginChildElements(settingBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-shop"), CairoFont.WhiteSmallText(), shopSelectionLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-shop"), hoverText, 500, shopSelectionLabel)
                    .AddDropDown(shopsKeys, shopsNames, selectedIndex, new SelectionChangedDelegate(this.onSelectionChanged), shopSelectBounds)
                    .AddIf(stall.IsAdminShop || VinUtils.IsCreativePlayer(api.World.Player))
                        .AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), CairoFont.WhiteSmallText(), adminShopLabel)
                        .AddHoverText(Lang.Get("vinconomy:tooltip-admin-shop"), hoverText, 500, adminShopLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleAdminShop), adminShopBounds, "admin")
                    .EndIf()
                    .AddStaticText(Lang.Get("vinconomy:gui-cost-per-purchase"), CairoFont.WhiteSmallText(), costSelectionLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-cost-per-purchase"), hoverText, 500, costSelectionLabel)
                    .AddNumberInput(costSelectionBounds, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "costQuantity")

                    .AddStaticText(Lang.Get("vinconomy:gui-items-per-purchase"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-items-per-purchase"), hoverText, 500, quantitySelectionLabel)
                    .AddNumberInput(quantitySelectionBounds, new Action<string>(this.onSellQuantityChanged), CairoFont.WhiteSmallText(), "sellQuantity")

                    .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-price"), hoverText, 500, currencyLabel)
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SetCurrencySlot), 1, [offset + stall.ProductStacksPerSlot], currencySlotBounds, "currency")

                    .AddStaticText(Lang.Get("vinconomy:gui-product"), CairoFont.WhiteSmallText(), purchaseLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-product"), hoverText, 500, purchaseLabel)
                    .AddItemSlotGrid(inv, null, 1, [0], purchaseSlotBounds)

                    .AddStaticText(Lang.Get("vinconomy:gui-decoration-block"), CairoFont.WhiteSmallText(), chiselLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-decoration-block"), hoverText, 500, chiselLabel)
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, [0], chiselSlotBounds, "chisel")
                .EndChildElements()

                .AddInset(itemPage)
                .BeginChildElements(itemPage)

                    .AddButton("<", new ActionConsumable(this.PreviousPage), pagePrev, EnumButtonStyle.Small, "prevPage")
                    .AddDynamicText(labelText, labelTextCenteredFont, pageLabel, "pageLabel")
                    .AddButton(">", new ActionConsumable(this.NextPage), pageNext, EnumButtonStyle.Small, "nextPage")
                    .AddDynamicText(Lang.Get("vinconomy:gui-meal-servings", [stallSlot.Meal.StackSize, stallSlot.Capacity]), labelLargeTextLeftFont, servingsLabel, "servings")
                    .AddDynamicText(Lang.Get("vinconomy:gui-ingredients"), labelTextLeftFont, ingredientsLabel, "ingredients")
                    .AddPassiveItemSlot(slotGrid1, inv, inv[1], false)
                    .AddPassiveItemSlot(slotGrid2, inv, inv[2], false)
                    .AddPassiveItemSlot(slotGrid3, inv, inv[3], false)
                    .AddPassiveItemSlot(slotGrid4, inv, inv[4], false)
                    .AddPassiveItemSlot(slotGrid5, inv, inv[5], false)
                    .AddPassiveItemSlot(slotGrid6, inv, inv[6], false)
                    .AddStaticText(Lang.Get("vinconomy:gui-serving-transfer"), labelTextLeftFont, servingModLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-serving-transfer"), hoverText, 500, servingModLabel)
                    .AddItemSlotGrid(vinInv, SendInvPacket, 1, [1], transferSlot, "transferSlot")
                    .AddButton("+1", () => { TransferMeal(1); return true; }, transferFrom1Bounds, EnumButtonStyle.Small)
                    .AddButton("+5", () => { TransferMeal(5); return true; }, transferFrom5Bounds, EnumButtonStyle.Small)
                    .AddButton("-1", () => { TransferMeal(-1); return true; }, transferTo1Bounds, EnumButtonStyle.Small)
                    .AddButton("-5", () => { TransferMeal(-5); return true; }, transferTo5Bounds, EnumButtonStyle.Small)
                .EndChildElements();
                //SingleComposer.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 5, debugBounds, "debugslots");

                if (curTab == 0)
                    SingleComposer.GetButton("prevPage").Enabled = false;

                if (curTab == stall.StallSlotCount - 1)
                    SingleComposer.GetButton("nextPage").Enabled = false;

                if (stall.IsAdminShop || VinUtils.IsCreativePlayer(api.World.Player))
                    SingleComposer.GetSwitch("admin").SetValue(stall.IsAdminShop);


                SingleComposer.GetTextInput("costQuantity").SetValue(stallSlot.Currency.StackSize);
                SingleComposer.GetTextInput("sellQuantity").SetValue(stallSlot.ItemsPerPurchase);

                SingleComposer.Compose();


            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

        }

        private void UpdateIngEvent(int obj)
        {
            UpdatePurchaseSlot();
            UpdateIngredients();
            SingleComposer.GetDynamicText("servings").SetNewText(Lang.Get("vinconomy:gui-meal-servings", [stallSlot.Meal.StackSize, stallSlot.Capacity]));
        }

        private void TransferMeal(int v)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(curTab);
                writer.Write(v);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.ACTIVATE_BLOCK, data);
            UpdatePurchaseSlot();
        }

        private void onCostQuantityChanged(string txt)
        {
            if (txt.Length == 0)
                return;

            Int32.TryParse(txt, out int val);
            val = Math.Max(val, 1);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(curTab);
                writer.Write(val);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_ITEM_PRICE, data);

        }

        private void UpdatePurchaseSlot()
        {
            ItemSlot item = stallSlot.FindFirstNonEmptyStockSlot();


            if (item != null)
            {
                this.purchaseSlot.Itemstack = item.Itemstack.Clone();
                this.purchaseSlot.Itemstack.StackSize = Math.Min(Math.Max(1, stallSlot.ItemsPerPurchase), item.MaxSlotStackSize);
            }
            else
            {
                this.purchaseSlot.Itemstack = null;
            }
        }

        private void OnToggleAdminShop(bool isToggled)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_ADMIN_SHOP, data);

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
            curTab = Math.Min(stall.StallSlotCount-1, curTab);
            this.Compose();
            return true;
        }

        private void onSellQuantityChanged(string amount)
        {
            int val = 1;
            Int32.TryParse(amount, out val);

            if (val > 0 && val <= 1024 && val != stallSlot.ItemsPerPurchase)
            {
                stallSlot.ItemsPerPurchase = val;
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(curTab);
                    writer.Write(val);
                    data = ms.ToArray();
                }
                capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_ITEMS_PER_PURCHASE, data);

                if (purchaseSlot.Itemstack != null)
                {
                    purchaseSlot.Itemstack.StackSize = val;
                }

            }
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

        private void SetCurrencySlot(object p)
        {
            SingleComposer.GetTextInput("costQuantity").SetValue(stallSlot.Currency.StackSize);
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarCloseClicked()
        {
            Inventory.SlotModified -= UpdateIngEvent;
            TryClose();
        }

    }
}
