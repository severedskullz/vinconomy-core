using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.GUI
{
    public class GuiViconStallOwner : GuiDialogBlockEntity
    {
        BEVinconContainer stall;
        ShopRegistration[] registers;
        ICoreClientAPI api;
        int stallSlotCount;
        private int stacksPerSlot;
        int curTab;
        DummyInventory inv;
        ViconPurchaseSlot purchaseSlot;
        StallSlot stallSlot;

        public GuiViconStallOwner(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconContainer>(BlockEntityPosition);
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
            stacksPerSlot = stall.StacksPerSlot;


            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;

            inv = new DummyInventory(capi);
            purchaseSlot = new ViconPurchaseSlot(inv, 0);
            inv.TakeLocked = true;
            inv.PutLocked = true;
            inv[0] = purchaseSlot;

            Compose();
        }

        private void Compose()
        {
            try
            {
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                ViconomyInventory vinInv = Inventory as ViconomyInventory;
                int[] uiSlots = new int[stacksPerSlot];
                int offset = curTab * (stacksPerSlot + 1) + 1;
                if (vinInv != null)
                {
                    for (int i = 0; i < stacksPerSlot; i++)
                    {
                        uiSlots[i] = offset + i;
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


                stallSlot = vinInv.StallSlots[curTab];
                UpdatePurchaseSlot();

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                //settingBounds.BothSizing = ElementSizing.FitToChildren;

                // Auto-sized dialog at the center of the screen
                //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
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

                // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
                //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

                ElementBounds itemPage = ElementBounds.FixedSize(200, 10).FixedRightOf(settingBounds).WithFixedOffset(25, GuiStyle.TitleBarHeight);
                itemPage.BothSizing = ElementSizing.FitToChildren;


                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithFixedPosition(0, 0);
                //ElementBounds pageLabel = ElementBounds.FixedSize(50, 20).WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0,15).WithFixedPadding(75,0);
                ElementBounds pageLabel = ElementBounds.FixedSize(50, 25).WithFixedAlignmentOffset(0, 10).FixedRightOf(pagePrev, 10);
                //ElementBounds pageNext = ElementBounds.FixedSize(50, 50).WithAlignment(EnumDialogArea.RightTop);
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                string labelText = Lang.Get("vinconomy:gui-slot", new object[] { curTab + 1, stall.StallSlotCount });
                labelTextFont.AutoBoxSize(labelText, pageLabel, true);

                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).FixedRightOf(pageLabel, 10);
                ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.CenterBottom, 0, 20, (int)Math.Ceiling(Math.Sqrt(uiSlots.Length)), (int)Math.Ceiling(Math.Sqrt(uiSlots.Length)));//.WithParent(itemPage);


                itemPage.WithChildren(pagePrev, pageLabel, pageNext, slotGrid);



                bgBounds.WithChildren(itemPage, settingBounds);

                //IconUtil.DrawArrowRight

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconStallOwner", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                SingleComposer.BeginChildElements(settingBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-shop"), CairoFont.WhiteSmallText(), shopSelectionLabel)
                    .AddDropDown(shopsKeys, shopsNames, selectedIndex, new SelectionChangedDelegate(this.onSelectionChanged), shopSelectBounds)
                    .AddIf(api.World.Player.HasPrivilege("gamemode"))
                        .AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), CairoFont.WhiteSmallText(), adminShopLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleAdminShop), adminShopBounds, "admin")
                    .EndIf()
                    .AddStaticText(Lang.Get("vinconomy:gui-cost-per-purchase"), CairoFont.WhiteSmallText(), costSelectionLabel)
                    .AddNumberInput(costSelectionBounds, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "costQuantity")

                    .AddStaticText(Lang.Get("vinconomy:gui-items-per-purchase"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                    .AddNumberInput(quantitySelectionBounds, new Action<string>(this.onSellQuantityChanged), CairoFont.WhiteSmallText(), "sellQuantity")
                    //.AddButton("Save", new ActionConsumable(this.onSave),saveButtonBounds, EnumButtonStyle.Small, "save")
                    .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SetCurrencySlot), 1, new int[] { offset + stacksPerSlot }, currencySlotBounds, "currency")

                    .AddStaticText(Lang.Get("vinconomy:gui-product"), CairoFont.WhiteSmallText(), purchaseLabel)
                    .AddItemSlotGrid(inv,null,1,purchaseSlotBounds)
                    //.AddPassiveItemSlot(purchaseSlotBounds, inv, purchaseSlot, true)

                    .AddStaticText(Lang.Get("vinconomy:gui-decoration-block"), CairoFont.WhiteSmallText(), chiselLabel )
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SetCurrencySlot), 1, new int[] { 0 }, chiselSlotBounds, "chisel")

                //.AddItemSlotGrid(inv, null, 1, new int[] { 0 }, purchaseSlotBounds, "purchase")
                //.AddPassiveItemSlot(outputSlotBounds, Inventory, )
                .EndChildElements();

                SingleComposer.BeginChildElements(itemPage)
                    .AddButton("<", new ActionConsumable(this.PreviousPage), pagePrev, EnumButtonStyle.Small, "prevPage")
                    .AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel")
                    .AddButton(">", new ActionConsumable(this.NextPage), pageNext, EnumButtonStyle.Small, "nextPage")
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), (int)Math.Ceiling(Math.Sqrt(uiSlots.Length)), uiSlots, slotGrid, "inventory")
                .EndChildElements();


                if (curTab == 0)
                    SingleComposer.GetButton("prevPage").Enabled = false;

                if (curTab == stallSlotCount - 1)
                    SingleComposer.GetButton("nextPage").Enabled = false;

                if (capi.World.Player.HasPrivilege("gamemode"))
                    SingleComposer.GetSwitch("admin").SetValue(stall.IsAdminShop);


                SingleComposer.GetTextInput("costQuantity").SetValue(stallSlot.currency.StackSize);
                SingleComposer.GetTextInput("sellQuantity").SetValue(stallSlot.itemsPerPurchase);


                //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
                SingleComposer.Compose();

            }
            catch (Exception e) {
                Console.WriteLine(e.ToString());
            }

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
                this.purchaseSlot.Itemstack.StackSize = Math.Min(Math.Max(1, stallSlot.itemsPerPurchase), item.MaxSlotStackSize);
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
            curTab = Math.Min(stallSlotCount-1, curTab);
            this.Compose();
            return true;
        }

        private void onSellQuantityChanged(string amount)
        {
            int val = 1;
            Int32.TryParse(amount, out val);

            if (val > 0 && val <= 1024 && val != stallSlot.itemsPerPurchase)
            {
                stallSlot.itemsPerPurchase = val;
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
            UpdatePurchaseSlot();
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void SetCurrencySlot(object p)
        {
            SingleComposer.GetTextInput("costQuantity").SetValue(stallSlot.currency.StackSize);
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

    }
}
