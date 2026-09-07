
using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.ModSystems;
using Commercially.Common.Registry;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory;
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
    public class SculptureShopOwnerTab : ModularTab
    {

        public const string CODE = "Vinconomy.SculptureOwner";
        public override string Code => CODE;

        public override string TabName => Lang.Get("vinconomy:tabname-generic-owner");

        VinconBaseInventory Inventory;
        IStallInventoryProvider StallProvider;
        IOwnableRegistry Registry;
        private IOwnableChild Ownable;

        int Layer;
        int SelectedIndex;
        int StallSlot; // Should always be 0, but just in case;

        string SculptureName;
        int SculptureXZ;
        int SculptureY;

        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            if (Inventory != null && StallProvider != null && Ownable != null)
            {
                CairoFont hoverText = CairoFont.WhiteDetailText();
                CairoFont smallText = CairoFont.WhiteSmallText();

                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                string labelText = $"Layer {Layer + 1} of {SculptureStallSlot.MaxSculptureSize}"; //Lang.Get("vinconomy:gui-slot", new object[] { StallSlot + 1, stall.StallSlotCount });

                OwnableRegistryKeys shopKeys = GUIUtils.GetOwnableDropdownListForOwner(Registry, Ownable);
                SelectedIndex = shopKeys.CurrentSelectedIndex;

                SculptureStallSlot stall = StallProvider.GetStallSlot<SculptureStallSlot>(StallSlot);
                SculptureXZ = stall.SculptureHorizontalSize;
                SculptureY = stall.SculptureVerticalSize;
                SculptureName = stall.SculptureName;

                int layerSlots = SculptureStallSlot.MaxSculptureSize * SculptureStallSlot.MaxSculptureSize;

                int[] slotIDs = new int[layerSlots];
                int offset = GUIUtils.GetProductOffsetForStall(StallProvider, StallSlot);
                for (int i = 0; i < slotIDs.Length; i++)
                {
                    slotIDs[i] = offset+ i + (Layer * layerSlots);
                }
                int currencySlotId = GUIUtils.GetCurrencySlotIdForStall(StallProvider, StallSlot);
                int productSlotId = GUIUtils.GetInternalOffsetForStall(StallProvider, StallSlot);
                int numColumns = SculptureStallSlot.MaxSculptureSize;
                int slotGridWidth = (int)(numColumns * (GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding));


                ElementBounds settingBounds = ElementBounds.FixedSize(250, 150).WithFixedOffset(10, GuiStyle.TitleBarHeight + 10);
                settingBounds.BothSizing = ElementSizing.FitToChildren;
                rootBounds.WithChild(settingBounds);

                ElementBounds shopSelectionLabel = ElementBounds.FixedSize(250, 30);
                ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);
                settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-shop"), smallText, shopSelectionLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-shop"), hoverText, 500, shopSelectionLabel);
                composer.AddDropDown(shopKeys.ShopKeys, shopKeys.ShopNames, SelectedIndex, this.OnShopChanged, shopSelectBounds, "shopSelection");

                ElementBounds nameLabel = ElementBounds.FixedSize(250, 30).FixedUnder(shopSelectBounds, 15);
                ElementBounds nameBounds = ElementBounds.FixedSize(250, 30).FixedUnder(nameLabel);
                settingBounds.WithChildren(nameLabel, nameBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-sculpture-name"), smallText, nameLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-output-name"), hoverText, 500, nameLabel);
                composer.AddTextInput(nameBounds, OnNameChanged, smallText, "nameInput");

                ElementBounds widthSelectionLabel = ElementBounds.FixedSize(200, 30).FixedUnder(nameBounds,15);
                ElementBounds widthSelectionBounds = ElementBounds.FixedSize(50, 30).FixedUnder(nameBounds, 15).FixedRightOf(widthSelectionLabel);
                settingBounds.WithChildren(widthSelectionLabel, widthSelectionBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-sculpture-width"), smallText, widthSelectionLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-sculpture-width"), hoverText, 500, widthSelectionLabel);
                composer.AddNumberInput(widthSelectionBounds, OnWidthChanged, smallText, "widthInput");

                ElementBounds heightSelectionLabel = ElementBounds.FixedSize(200, 30).FixedUnder(widthSelectionLabel, 15);
                ElementBounds heightSelectionBounds = ElementBounds.FixedSize(50, 30).FixedUnder(widthSelectionLabel, 15).FixedRightOf(heightSelectionLabel);
                settingBounds.WithChildren(heightSelectionLabel, heightSelectionBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-sculpture-height"), smallText, heightSelectionLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-sculpture-height"), hoverText, 500, heightSelectionLabel);
                composer.AddNumberInput(heightSelectionBounds, OnHeightChanged, smallText, "heightInput");

                ElementBounds priceLabel = ElementBounds.FixedSize(100, 30).FixedUnder(heightSelectionLabel, 15);
                ElementBounds priceSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).WithFixedOffset(5, 0).FixedUnder(priceLabel);
                ElementBounds priceInputBounds = ElementBounds.FixedSize(75, 30).FixedUnder(priceLabel).FixedRightOf(priceSlotBounds).WithFixedOffset(10, 10);
                settingBounds.WithChildren(priceLabel, priceSlotBounds, priceInputBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-price"), smallText, priceLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-price"), hoverText, 500, priceLabel);
                composer.AddItemSlotGrid(Inventory, this.SetCurrencySlot, 1, new int[] { currencySlotId }, priceSlotBounds, "currency");
                composer.AddNumberInput(priceInputBounds, this.OnCostQuantityChanged, smallText, "costQuantity");

                if (GUIUtils.IsCreativePlayer(ClientApi.World.Player))
                {
                    ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(priceSlotBounds).WithFixedOffset(0, 10);
                    ElementBounds adminShopLabel = ElementBounds.FixedSize(100, 25).FixedUnder(priceSlotBounds).FixedRightOf(adminShopBounds).WithFixedOffset(0, 10);

                    settingBounds.WithChildren(adminShopLabel, adminShopBounds);
                    composer.AddSwitch(this.OnToggleAdminShop, adminShopBounds, "admin");
                    composer.AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), smallText, adminShopLabel);
                    composer.AddHoverText(Lang.Get("vinconomy:tooltip-admin-shop"), hoverText, 500, adminShopLabel);
                    composer.GetSwitch("admin").SetValue(Ownable.IsAdminOwned);
                }

                ElementBounds pageBounds = ElementBounds.FixedSize(250, 30).FixedRightOf(settingBounds, 15).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(pageBounds);
                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30);
                ElementBounds pageLabel = ElementBounds.FixedSize(Math.Max(slotGridWidth, 200), 25).WithFixedAlignmentOffset(0, 5).FixedRightOf(pagePrev, 10);
                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).FixedRightOf(pageLabel, 10);
                pageBounds.WithChildren(pagePrev, pageLabel, pageNext);
                composer.AddButton("<", PreviousPage, pagePrev, EnumButtonStyle.Small, "prevPage");
                composer.AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel");
                composer.AddButton(">", NextPage, pageNext, EnumButtonStyle.Small, "nextPage");


                ElementBounds stallBounds = ElementBounds.FixedSize(250, 200).FixedRightOf(settingBounds, 15).FixedUnder(pageBounds, 10);
                stallBounds.BothSizing = ElementSizing.FitToChildren;
                rootBounds.WithChild(stallBounds);

                ElementBounds stockLabel = ElementBounds.FixedSize(Math.Max(slotGridWidth, 200) + 80, 25);//.FixedUnder(productSlotBounds, 15);
                stallBounds.WithChildren(stockLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-product"), labelTextFont, stockLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-product"), hoverText, 500, stockLabel);

                ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.CenterTop, 0, 20, numColumns, numColumns).FixedUnder(stockLabel, -20);
                stallBounds.WithChild(slotGrid);
                composer.AddItemSlotGrid(Inventory, (Gui as GUIModularBlockEntity).SendPacket, SculptureStallSlot.MaxSculptureSize, slotIDs, slotGrid, "inventory");

                composer.GetButton("prevPage").Enabled = Layer > 0;
                composer.GetButton("nextPage").Enabled = Layer < SculptureStallSlot.MaxSculptureSize - 1;

                composer.GetNumberInput("costQuantity").SetValue(Math.Max(1, Inventory.GetStall(StallSlot).Currency.StackSize));

                composer.GetNumberInput("widthInput").SetValue(Math.Clamp(SculptureXZ, 1, 5));
                composer.GetNumberInput("heightInput").SetValue(Math.Clamp(SculptureY, 1, 5));
                composer.GetTextInput("nameInput").SetValue(SculptureName);

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

        private void OnNameChanged(string obj)
        {
            if (!Gui.Composer.Composed)
                return;

            SculptureName = obj;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(SculptureName);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_ITEM_NAME, data);

        }

        private void OnWidthChanged(string txt)
        {
            if (!Gui.Composer.Composed)
                return;

            Int32.TryParse(txt, out int val);
            SculptureXZ = Math.Clamp(val, 1, SculptureStallSlot.MaxSculptureSize);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(SculptureXZ);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_SCULPTURE_XZ, data);
        }

        private void OnHeightChanged(string txt)
        {
            if (!Gui.Composer.Composed)
                return;

            Int32.TryParse(txt, out int val);
            SculptureY = Math.Clamp(val, 1, SculptureStallSlot.MaxSculptureSize);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(SculptureY);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_SCULPTURE_Y, data);
        }

        private bool PreviousPage()
        {
            Layer -= 1;
            Layer = Math.Max(0, Layer);
            //Gui.Composer.GetButton("prevPage").Enabled = StallSlot != 0;
            //Gui.Composer.GetButton("nextPage").Enabled = StallSlot != StallProvider.StallCount - 1;
            Gui.FullRecompose();
            return true;
        }

        private bool NextPage()
        {
            Layer += 1;
            Layer = Math.Min(SculptureStallSlot.MaxSculptureSize - 1, Layer);
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
            Inventory = entity?.GetBehavior<IInventoryProvider>()?.Inventory as VinconBaseInventory;
            StallProvider = entity?.GetBehavior<IStallInventoryProvider>();
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
