using System;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.Slots;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiDialogViconSculpturePadCustomer : GuiDialogBlockEntity
    {
        BEVinconSculpturePad stall;

        int curTab;
        int quantity = 1;
        DummyInventory inv;
        ViconPurchaseSlot purchaseSlot;
        ViconCurrencySlot currancySlot;

        public GuiDialogViconSculpturePadCustomer(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {

            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconSculpturePad>(BlockEntityPosition);
            VinconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();

            if (base.IsDuplicate)
            {
                return;
            }
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

            ViconSculptureInventory vinInv = Inventory as ViconSculptureInventory;
            ItemStack purchaseItem = stall.GenNewSculptureBundle();
            
            if (purchaseItem != null)
            {
                this.purchaseSlot.Itemstack = purchaseItem;
                this.purchaseSlot.Itemstack.StackSize = 1;
            } else
            {
                this.purchaseSlot.Itemstack = null;
            }

            ItemSlot currencyItem = stall.GetCurrencyForStall(0);

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
            
            ElementBounds currencyLabel = ElementBounds.FixedSize(60, 25).WithFixedOffset(35, 15);
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
                    .AddStaticText(Lang.Get("vinconomy:gui-quantity"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                    .AddNumberInput(quantitySelectionBounds, null, CairoFont.WhiteSmallText(), "quantity")
                    .AddButton(Lang.Get("vinconomy:gui-deal"), new ActionConsumable(this.OnPurchase),purchaseButtonBounds, EnumButtonStyle.Small, "save")
                    .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                    .AddPassiveItemSlot(currencySlotBounds, inv, currancySlot, true)
                    
                    .AddStaticText(Lang.Get("vinconomy:gui-product"), CairoFont.WhiteSmallText(), purchaseLabel)
                    .AddItemSlotGrid(inv, null, 1, new int[] { 0 }, purchaseSlotBounds)

                .EndChildElements();
            

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
                
                capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.PURCHASE_ITEMS, data);
            }
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
