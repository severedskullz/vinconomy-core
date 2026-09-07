
using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.ModSystems;
using Commercially.Common.Registry;
using Commercially.Common.Util;
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
    public class GachaStallOwnerTab : ModularTab
    {
        public const string CODE = "Vinconomy.GachaStallOwner";
        public override string Code => CODE;
        public override string TabName => Lang.Get("vinconomy:tabname-generic-owner");


        GachaShopInventory Inventory;
        IStallInventoryProvider StallProvider;
        IOwnableRegistry Registry;
        IOwnableChild Ownable;
        int StallSlot;
        int SelectedIndex;
        private bool IsUpdating;

        public override void Initialize(IModularGui gui, BlockEntity entity = null)
        {
            base.Initialize(gui, entity);
            Registry = ClientApi.ModLoader.GetModSystem<CommerciallyModSystem>().OwnableRegistry;
            Inventory = entity?.GetBehavior<IInventoryProvider>()?.Inventory as GachaShopInventory;
            StallProvider = entity?.GetBehavior<IStallInventoryProvider>();
            Ownable = entity?.GetBehavior<IOwnableChild>();

            //TODO: Figure out a better way to pass this in - will need it for all of the tabbed GUIs for shops
            GUIModularBlockEntity guiBE = gui as GUIModularBlockEntity;
            if (guiBE != null)
            {
                StallSlot = entity?.GetBehavior<IStallComponent>()?.GetStallIndexFromSelection(guiBE.BlockSelectionIndex) ?? 0;
            }
        }

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

                GachaStallSlot stall = StallProvider.GetStallSlot<GachaStallSlot>(StallSlot);

                int stallOffset = GUIUtils.GetOffsetForStall(StallProvider, StallSlot);
                int productOffset = stallOffset + 1 + stall.GachaContents.Length;
                int currencySlotId = 0;
                int productSlotId = GUIUtils.GetInternalOffsetForStall(StallProvider, StallSlot);

                int productSlotLength = stall.GetStockSlots().Length;
                int[] productSlotsIds = new int[productSlotLength];
                for (int i = 0; i < productSlotLength; i++)
                {
                    productSlotsIds[i] = productOffset + i;
                }

                int numColumns = 10;
                int slotGridWidth = (int)(numColumns * (GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding));

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 150).WithFixedOffset(10, GuiStyle.TitleBarHeight + 10);
                settingBounds.BothSizing = ElementSizing.FitToChildren;
                rootBounds.WithChild(settingBounds);

                ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 250, 30);
                ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);
                settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-shop"), smallText, shopSelectionLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-shop"), hoverText, 500, shopSelectionLabel);
                composer.AddDropDown(shopKeys.ShopKeys, shopKeys.ShopNames, SelectedIndex, this.OnShopChanged, shopSelectBounds, "shopSelection");

                ElementBounds priceLabel = ElementBounds.FixedSize(250, 30).FixedUnder(shopSelectBounds, 5);
                ElementBounds priceSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(priceLabel);
                ElementBounds priceInputBounds = ElementBounds.FixedSize(75, 30).FixedUnder(priceLabel, 10).FixedRightOf(priceSlotBounds, 10);
                settingBounds.WithChildren(priceLabel, priceSlotBounds, priceInputBounds);
                composer.AddStaticText(Lang.Get("vinconomy:gui-price"), smallText, priceLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-price"), hoverText, 500, priceLabel);
                composer.AddItemSlotGrid(Inventory, this.SetCurrencySlot, 1, new int[] { currencySlotId }, priceSlotBounds, "currency");
                composer.AddNumberInput(priceInputBounds, this.OnCostQuantityChanged, smallText, "costQuantity");

                ElementBounds totalCountLabel = ElementBounds.FixedSize(200, 25).FixedUnder(priceSlotBounds, 13);
                ElementBounds totalCountBounds = ElementBounds.FixedSize(40, 40).FixedUnder(priceSlotBounds, 10).FixedRightOf(totalCountLabel);
                settingBounds.WithChildren(totalCountLabel, totalCountBounds);
                composer.AddSwitch(this.OnToggleItemWeight, totalCountBounds, "weightCounds");
                composer.AddStaticText(Lang.Get("vinconomy:gui-item-weight-counts"), smallText, totalCountLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-item-weight-counts"), hoverText, 500, totalCountLabel);


                if (GUIUtils.IsCreativePlayer(ClientApi.World.Player))
                {

                    ElementBounds adminShopLabel = ElementBounds.FixedSize(200, 25).FixedUnder(totalCountLabel, 13);
                    ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(totalCountLabel, 10).FixedRightOf(adminShopLabel);
                    settingBounds.WithChildren(adminShopLabel, adminShopBounds);
                    composer.AddSwitch(this.OnToggleAdminShop, adminShopBounds, "admin");
                    composer.AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), smallText, adminShopLabel);
                    composer.AddHoverText(Lang.Get("vinconomy:tooltip-admin-shop"), hoverText, 500, adminShopLabel);
                    composer.GetSwitch("admin").SetValue(Ownable.IsAdminOwned);
                }

                ElementBounds pageBounds = ElementBounds.FixedSize(400, 30).FixedRightOf(settingBounds, 15).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                pageBounds.BothSizing = ElementSizing.FitToChildren;
                rootBounds.WithChild(pageBounds);
                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.LeftTop);
                ElementBounds pageLabel = ElementBounds.FixedSize(slotGridWidth - 70, 25).WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0, 5);//.FixedRightOf(pagePrev, 10);
                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.RightTop);//.FixedRightOf(pageLabel, 10);
                pageBounds.WithChildren(pagePrev, pageLabel, pageNext);
                composer.AddButton("<", PreviousPage, pagePrev, EnumButtonStyle.Small, "prevPage");
                composer.AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel");
                composer.AddButton(">", NextPage, pageNext, EnumButtonStyle.Small, "nextPage");


                //ElementBounds stallBounds = ElementBounds.FixedSize(250, 200).FixedRightOf(settingBounds, 15).FixedUnder(pageBounds);
                //stallBounds.BothSizing = ElementSizing.FitToChildren;
                //rootBounds.WithChild(stallBounds);
                

                ElementBounds contentsLabel = ElementBounds.FixedSize(slotGridWidth, 25).FixedUnder(pagePrev, 10);
                pageBounds.WithChildren(contentsLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-contents"), labelTextFont, contentsLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-contents"), hoverText, 500, contentsLabel);

                ElementBounds last = null;
                for (int i = 0; i < stall.GachaContents.Length; i++)
                {
                    int index = i; // For the lambda
                    ElementBounds contentsGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 20, 1, 1).FixedUnder(contentsLabel, -20);
                    if (last != null)
                    {
                        contentsGrid.FixedRightOf(last, 10);
                    }
                    pageBounds.WithChild(contentsGrid);
                    composer.AddItemSlotGrid(Inventory, (obj) => {UpdateContentsSlot(index, obj); }, 1, [stallOffset + 1 +i], contentsGrid, "contentsInventory" + i);

                    ElementBounds contentsNumBounds = ElementBounds.FixedSize(50, 30).FixedUnder(contentsGrid, 2);
                    if (last != null)
                    {
                        contentsNumBounds.FixedRightOf(last, 10);
                    }
                    pageBounds.WithChildren(contentsNumBounds);
                    composer.AddNumberInput(contentsNumBounds, (obj) => { OnContentsQuantityChanged(index, obj); }, smallText, "contentsQuantity" + i);

                    last = contentsGrid;
                }

                ElementBounds weightLabel = ElementBounds.FixedSize(150, 25).FixedUnder(last, 45);
                pageBounds.WithChildren(weightLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-weight"), labelTextFont, weightLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-weight"), hoverText, 500, weightLabel);

                ElementBounds weightBounds = ElementBounds.FixedSize(75, 25).FixedUnder(last, 45).FixedRightOf(weightLabel,10);
                pageBounds.WithChildren(weightBounds);
                composer.AddNumberInput(weightBounds, this.OnWeightChanged, smallText, "weight");


                ElementBounds stockLabel = ElementBounds.FixedSize(slotGridWidth, 25).FixedUnder(weightLabel, 10);
                pageBounds.WithChildren(stockLabel);
                composer.AddStaticText(Lang.Get("vinconomy:gui-stock"), labelTextFont, stockLabel);
                composer.AddHoverText(Lang.Get("vinconomy:tooltip-stock"), hoverText, 500, stockLabel);

                int columns = 10;
                int rows = (int)Math.Ceiling(stall.Stock.Length / 10.0f);

                int[] columnIDs = new int[stall.Stock.Length];
                for (int i = 0; i < stall.Stock.Length; i++)
                {
                    columnIDs[i] = productOffset + i;
                }


                ElementBounds stockGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 20, columns, rows).FixedUnder(stockLabel, -20);
                pageBounds.WithChild(stockGrid);
                composer.AddItemSlotGrid(Inventory, (Gui as GUIModularBlockEntity).SendPacket, columns, columnIDs, stockGrid, "contentsInventory");



                composer.GetButton("prevPage").Enabled = StallSlot != 0;
                composer.GetButton("nextPage").Enabled = StallSlot != StallProvider.StallCount - 1;

                composer.GetNumberInput("weight").SetValue(stall.Weight);

                UpdateContentsQuantity();
                composer.GetNumberInput("costQuantity").SetValue(Math.Max(1, Inventory.GetStall(StallSlot).Currency.StackSize));
                composer.GetSwitch("weightCounds").SetValue(Inventory.IsCountBasedRandomizer);
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

        public void UpdateContentsQuantity()
        {
            GachaStallSlot stall = StallProvider.GetStallSlot<GachaStallSlot>(StallSlot);

            for (int i = 0; i < stall.GachaContents.Length; i++)
            {
                
                int stacksize = stall.GachaContents[i].StackSize;
                Gui.Composer.GetNumberInput("contentsQuantity" + i).SetValue(stacksize);
            }

        }

        private void UpdateContentsSlot(int index, object obj)
        {
            GachaStallSlot stall = StallProvider.GetStallSlot<GachaStallSlot>(StallSlot);
            int stacksize = stall.GachaContents[index].StackSize;
            (Gui as GUIModularBlockEntity).SendPacket(obj);
            IsUpdating = true;
                Gui.Composer.GetNumberInput("contentsQuantity" + index).SetValue(stacksize);
            IsUpdating = false;
        }

        private void OnWeightChanged(string amount)
        {
            if (!Gui.Composer.Composed)
                return;

            Int32.TryParse(amount, out int value);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(value);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_WEIGHT, data);
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

        private void OnContentsQuantityChanged(int slot, string amount)
        {
            if (!Gui.Composer.Composed || IsUpdating)
                return;

            Int32.TryParse(amount, out int val);
            val = Math.Max(1, val);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(slot);
                writer.Write(val);
                data = ms.ToArray();
            }
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_CONTENTS_QUANTITY, data);
            
        }

        private void OnCostQuantityChanged(string amount)
        {
            if (!Gui.Composer.Composed || IsUpdating)
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

        private void SetCurrencySlot(object obj)
        {
            BaseStallSlot stall = StallProvider.GetStallSlot(StallSlot);
            Gui.Composer.GetTextInput("costQuantity").SetValue(stall.CurrencyPerPurchase);
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos.X, BlockEntity.Pos.Y, BlockEntity.Pos.Z, obj);
        }

        private bool PreviousPage()
        {
            StallSlot -= 1;
            StallSlot = Math.Max(0, StallSlot);
            Gui.FullRecompose();
            return true;
        }

        private bool NextPage()
        {
            StallSlot += 1;
            StallSlot = Math.Min(StallProvider.StallCount - 1, StallSlot);
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
        private void OnToggleItemWeight(bool isToggled)
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
            ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, VinConstants.SET_TOTAL_RANDOMIZER, data);
        }
    }
}
