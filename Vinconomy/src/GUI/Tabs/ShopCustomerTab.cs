
using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.Inventory.Slots;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory;
using System;
using System.IO;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Commercially.Vinconomy.GUI.Tabs
{
    public class ShopCustomerTab : ModularTab
    {

        public const string CODE = "Vinconomy.ShopCustomer";
        public override string Code => CODE;
        public override string TabName => Lang.Get("vinconomy:tabname-generic-customer");

        DummyInventory DInv;
        VinconBaseInventory Inventory;
        IStallInventoryProvider StallProvider;
        int StallSlot;

        private int Quantity = 1;

        public override void Initialize(IModularGui gui, BlockEntity entity = null)
        {
            base.Initialize(gui, entity);
            StallProvider = entity?.GetBehavior<IStallInventoryProvider>();
            Inventory = StallProvider?.Inventory as VinconBaseInventory;
            DInv = new DummyInventory(ClientApi, 2);
            DInv.PutLocked = true;
            DInv.TakeLocked = true;
            DInv[0] = new ItemLockedSlot(DInv);
            DInv[1] = new ItemLockedSlot(DInv);

            //TODO: Figure out a better way to pass this in - will need it for all of the tabbed GUIs for shops
            GUIModularBlockEntity guiBE = gui as GUIModularBlockEntity;
            if (guiBE != null)
            {
                StallSlot = entity?.GetBehavior<IStallComponent>()?.GetStallIndexFromSelection(guiBE.BlockSelectionIndex) ?? 0;
            }

        }

        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            if (Inventory != null && StallProvider != null)
            {
                CairoFont hoverText = CairoFont.WhiteDetailText();
                CairoFont smallText = CairoFont.WhiteSmallText();
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                string labelText = Lang.Get("vinconomy:gui-slot", [StallSlot + 1, StallProvider.StallCount]);

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.LeftTop);
                ElementBounds pageLabel = ElementBounds.FixedSize(50, 25).WithFixedAlignmentOffset(0, 10).WithAlignment(EnumDialogArea.CenterTop);

                //string labelText = Lang.Get("vinconomy:gui-slot", new object[] { StallSlot + 1, StallProvider.StallCount });
                labelTextFont.AutoBoxSize(labelText, pageLabel, true);
                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.RightTop);

                ElementBounds currencyLabel = ElementBounds.FixedSize(60, 25).FixedUnder(pagePrev).WithFixedOffset(35, 15);
                ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel).WithFixedOffset(35, 0);

                ElementBounds purchaseLabel = currencyLabel.RightCopy().WithFixedSize(60, 25).WithFixedOffset(60, 0);
                ElementBounds purchaseSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(purchaseLabel).WithFixedOffset(155, 0);

                ElementBounds quantitySelectionLabel = ElementBounds.FixedSize(75, 30).FixedUnder(currencyLabel).WithFixedOffset(0, 75);
                ElementBounds quantitySelectionBounds = quantitySelectionLabel.RightCopy().WithFixedSize(75, 30).WithFixedOffset(0, -5);
                ElementBounds purchaseButtonBounds = ElementBounds.FixedSize(60, 40).FixedUnder(quantitySelectionLabel).WithFixedOffset(100, 0);


                settingBounds.WithChildren(quantitySelectionBounds, quantitySelectionLabel, currencyLabel, currencySlotBounds, purchaseSlotBounds, purchaseButtonBounds);
                settingBounds.verticalSizing = ElementSizing.FitToChildren;

                rootBounds.WithChildren(settingBounds);


                composer.BeginChildElements(settingBounds)
                    .AddButton("<", new ActionConsumable(this.PreviousPage), pagePrev, EnumButtonStyle.Small, "prevPage")
                    .AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel")
                    .AddButton(">", new ActionConsumable(this.NextPage), pageNext, EnumButtonStyle.Small, "nextPage")

                    .AddStaticText(Lang.Get("vinconomy:gui-quantity"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                    .AddNumberInput(quantitySelectionBounds, OnQuantityChanged, CairoFont.WhiteSmallText(), "quantity")
                    .AddButton(Lang.Get("vinconomy:gui-deal"), new ActionConsumable(this.OnPurchase), purchaseButtonBounds, EnumButtonStyle.Small, "save")
                    .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                    //.AddPassiveItemSlot(currencySlotBounds, inv, currancySlot, true)
                    .AddItemSlotGrid(DInv, null, 1, new int[] { 0 }, currencySlotBounds)

                    .AddStaticText(Lang.Get("vinconomy:gui-product"), CairoFont.WhiteSmallText(), purchaseLabel)
                    .AddItemSlotGrid(DInv, null, 1, new int[] { 1 }, purchaseSlotBounds)

                .EndChildElements();
                UpdatePurchaseInfo();

                composer.GetNumberInput("quantity").SetValue(Quantity);

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

        private void OnQuantityChanged(string amount)
        {
            if (!Gui.Composer.Composed)
                return;

            Int32.TryParse(amount, out int val);
            Quantity = Math.Max(1, val);

            DInv[0].Itemstack?.StackSize = Inventory.GetStall(StallSlot)?.Currency?.StackSize * Quantity ?? 0;
            DInv[1].Itemstack?.StackSize = Inventory.GetStall(StallSlot)?.Product?.StackSize * Quantity ?? 0;
        }

        private bool PreviousPage()
        {
            Quantity = 1;
            GuiElementNumberInput quantityInput = Gui.Composer.GetNumberInput("quantity");
            quantityInput.SetValue(Quantity);

            StallSlot -= 1;
            StallSlot = Math.Max(0, StallSlot);
            UpdatePurchaseInfo();
            return true;
        }

        private bool NextPage()
        {
            Quantity = 1;
            GuiElementNumberInput quantityInput = Gui.Composer.GetNumberInput("quantity");
            quantityInput.SetValue(Quantity);

            StallSlot += 1;
            StallSlot = Math.Min(StallProvider.StallCount - 1, StallSlot);
            UpdatePurchaseInfo();
            return true;
        }

        private void UpdatePurchaseInfo()
        {
            ItemStack currency = Inventory.GetStall(StallSlot).Currency.Itemstack?.Clone();
            if (currency != null)
                currency.StackSize = currency.StackSize * Quantity;

            ItemStack product = Inventory.GetStall(StallSlot).Product.Itemstack?.Clone();
            if (product != null)
                product.StackSize = product.StackSize * Quantity;

            DInv[0].Itemstack = currency;
            DInv[1].Itemstack = product;


            Gui.Composer.GetButton("prevPage").Enabled = StallSlot != 0;
            Gui.Composer.GetButton("nextPage").Enabled = StallSlot != StallProvider.StallCount - 1;
        }

        public override bool IsVisible()
        {
            return true;
        }

        public override void OnGuiClosed()
        {
 
        }

        public override void OnGuiOpened()
        {

        }

        private bool OnPurchase()
        {
            //capi.Logger.Chat("Attempting to purchase item from slot " + curTab);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(StallSlot);
                writer.Write(Quantity);
                data = ms.ToArray();

                ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.PURCHASE_ITEMS, data);
            }
            return true;
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
