using System;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory.StallSlots;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.Slots;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiDialogViconStallCustomer<E> : GuiDialogBlockEntity where E:ItemSlot
    {
        BEVinconContainer stall;

        int curTab;
        int quantity = 1;
        DummyInventory inv;
        ViconPurchaseSlot purchaseSlot;
        ViconCurrencySlot currancySlot;
        StallSlotBase<E> stallSlot;

        public GuiDialogViconStallCustomer(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {

            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconContainer>(BlockEntityPosition);
            curTab = stallSelection;
            VinconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();

            if (base.IsDuplicate)
            {
                return;
            }
            //capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;

            this.inv = new DummyInventory(capi, 2);

            purchaseSlot = new ViconPurchaseSlot(inv, 0);
            this.inv[0] = purchaseSlot;

            currancySlot = new ViconCurrencySlot(inv);
            this.inv[1] = currancySlot;

            this.Compose();
        }

        private void Compose()
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            ViconomyBaseInventory<E> vinInv = Inventory as ViconomyBaseInventory<E>;
            stallSlot = vinInv.StallSlots[curTab];
            ItemSlot purchaseItem = stallSlot.FindFirstNonEmptyStockSlot();
            
            if (purchaseItem != null)
            {
                this.purchaseSlot.Itemstack = purchaseItem.Itemstack.Clone();
                this.purchaseSlot.Itemstack.StackSize = stallSlot.itemsPerPurchase * quantity;
            } else
            {
                this.purchaseSlot.Itemstack = null;
            }

            ItemSlot currencyItem = stallSlot.currency;

            if (currencyItem != null && currencyItem.Itemstack != null)
            {
                this.currancySlot.Itemstack = currencyItem.Itemstack.Clone();
                this.currancySlot.Itemstack.StackSize = currencyItem.Itemstack.StackSize * quantity;
            }
            else
            {
                this.currancySlot.Itemstack = null;
            }

            ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0,GuiStyle.TitleBarHeight);
            
            ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.LeftTop);
            ElementBounds pageLabel = ElementBounds.FixedSize(50, 25).WithFixedAlignmentOffset(0, 10).WithAlignment(EnumDialogArea.CenterTop);
            CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
            string labelText = Lang.Get("vinconomy:gui-slot", new object[] { curTab + 1, stall.StallSlotCount });
            labelTextFont.AutoBoxSize(labelText, pageLabel, true);
            ElementBounds pageNext = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.RightTop);

            ElementBounds currencyLabel = ElementBounds.FixedSize(60, 25).FixedUnder(pagePrev).WithFixedOffset(35, 15);
            ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel).WithFixedOffset(35, 0);

            ElementBounds purchaseLabel = currencyLabel.RightCopy().WithFixedSize(60, 25).WithFixedOffset(60, 0);
            ElementBounds purchaseSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(purchaseLabel).WithFixedOffset(155, 0);

            ElementBounds quantitySelectionLabel = ElementBounds.FixedSize(75, 30).FixedUnder(currencyLabel).WithFixedOffset(0, 75);
            ElementBounds quantitySelectionBounds = quantitySelectionLabel.RightCopy().WithFixedSize(75, 30).WithFixedOffset(0,-5);
            ElementBounds purchaseButtonBounds = ElementBounds.FixedSize(60, 40).FixedUnder(quantitySelectionLabel).WithFixedOffset(100, 0);


            settingBounds.WithChildren(quantitySelectionBounds, quantitySelectionLabel, currencyLabel, currencySlotBounds, purchaseSlotBounds, purchaseButtonBounds);
            settingBounds.verticalSizing = ElementSizing.FitToChildren;

            bgBounds.WithChildren( settingBounds);

            //IconUtil.DrawArrowRight

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("ViconStallCustomer", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

            
                SingleComposer.BeginChildElements(settingBounds)
                    .AddButton("<", new ActionConsumable(this.PreviousPage), pagePrev, EnumButtonStyle.Small, "prevPage")
                    .AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel")
                    .AddButton(">", new ActionConsumable(this.NextPage), pageNext, EnumButtonStyle.Small, "nextPage")
  
                    .AddStaticText(Lang.Get("vinconomy:gui-quantity"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                    .AddNumberInput(quantitySelectionBounds, null, CairoFont.WhiteSmallText(), "quantity")
                    .AddButton(Lang.Get("vinconomy:gui-deal"), new ActionConsumable(this.OnPurchase),purchaseButtonBounds, EnumButtonStyle.Small, "save")
                    .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                    .AddPassiveItemSlot(currencySlotBounds, inv, currancySlot, true)
                    
                    .AddStaticText(Lang.Get("vinconomy:gui-product"), CairoFont.WhiteSmallText(), purchaseLabel)
                    .AddItemSlotGrid(inv, null, 1, new int[] { 0 }, purchaseSlotBounds)

                .EndChildElements();
            
            if (curTab == 0)
                SingleComposer.GetButton("prevPage").Enabled = false;

            if (curTab == stall.StallSlotCount-1)
                SingleComposer.GetButton("nextPage").Enabled = false;

            //Prevent stack overflow from OnQuantityChanged from getting fired when we call SetValue().
            GuiElementNumberInput quantityInput = SingleComposer.GetNumberInput("quantity");
            quantityInput.SetValue(quantity);
            quantityInput.OnTextChanged = new Action<string>(this.onQuantityChanged); // NOW we can update the value without stack overflow.

            SingleComposer.Compose();
        }

        private bool OnPurchase()
        {
            //capi.Logger.Chat("Attempting to purchase item from slot " + curTab);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(curTab);
                writer.Write(quantity);
                writer.Write(true);
                data = ms.ToArray();
                
                capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.PURCHASE_ITEMS, data);
            }
            return true;
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

        private void onQuantityChanged(string amount)
        {
            int val = 1;
            Int32.TryParse(amount, out val);
            quantity = Math.Max(0,val);

            Compose();

            
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

    }
}
