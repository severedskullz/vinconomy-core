using System;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiDialogViconGachaCustomer : GuiDialogBlockEntity
    {
        BEVinconGacha stall;
        ViconomyGachaInventory vinInv;
        DummyInventory inv;

        public GuiDialogViconGachaCustomer(string DialogTitle, ViconomyGachaInventory Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {

            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconGacha>(BlockEntityPosition);
            vinInv = Inventory;
            vinInv.SlotModified += VinInv_SlotModified;

            if (base.IsDuplicate)
            {
                return;
            }
            //capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;

            this.inv = new DummyInventory(capi, Inventory.Count);
            this.inv.TakeLocked = true;
            this.inv.PutLocked = true;
            this.inv[0] = new ViconCurrencySlot(inv);


            for (int i = 1; i < Inventory.Count; i++)
            {
                this.inv[i] = new ViconPurchaseSlot(inv, i);
            }
            
            Compose();
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            vinInv.SlotModified -= VinInv_SlotModified;
        }
        private void VinInv_SlotModified(int obj)
        {
            UpdateSlots();
        }

        private void UpdateSlots()
        {
            ItemSlot currencyItem = vinInv[0];
            if (currencyItem.Itemstack != null)
            {
                this.inv[0].Itemstack = currencyItem.Itemstack.Clone();
            }
            else
            {
                this.inv[0].Itemstack = null;
            }

            for (int i = 1; i < Inventory.Count; i++)
            {
                ItemStack orig = Inventory[i].Itemstack;
                ItemStack clone = null;
                if (orig != null)
                {
                    clone = orig.Clone();
                    clone.StackSize = 1;
                }

                this.inv[i].Itemstack = clone;
            }
            UpdateSlotWinningChances();
        }

        private void UpdateSlotWinningChances()
        {
            int totalItems = vinInv.GetTotalItems();
            int totalSlots = vinInv.GetNonEmptySlotCount();

            for (int i = 0; i < vinInv.Slots.Length - 1; i++)
            {
                ItemSlot slot = inv.Slots[i + 1];
                double percent = vinInv.GetChanceForSlot(i + 1, stall.useTotalRandomizer);
                string percentage = percent + "%";
                GuiElementDynamicText text = SingleComposer.GetDynamicText("percentage" + i);
                text.SetNewText(percentage);

            }
        }

        private void Compose()
        {
            try { 
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0,GuiStyle.TitleBarHeight);

                ElementBounds currencyLabel = ElementBounds.FixedSize(60, 25).WithFixedOffset(35, 15);
                ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel).WithFixedOffset(35, 0);
                ElementBounds purchaseButtonBounds = ElementBounds.FixedSize(60, 40).FixedUnder(currencyLabel).FixedRightOf(currencySlotBounds).WithFixedOffset(50, 0);

                ElementBounds chanceLabel = ElementBounds.FixedSize(215, 25).FixedUnder(currencySlotBounds).WithFixedOffset(15, 15);
                ElementBounds gachaBounds = ElementBounds.FixedSize(100, 300).FixedUnder(chanceLabel).WithFixedOffset(0, 5);
                gachaBounds.BothSizing = ElementSizing.FitToChildren;
                int slotCount = Inventory.Count - 1;
                int slotsPerRow = 3;
                int rows = (int)Math.Ceiling(((double)slotCount) / slotsPerRow);
                //Console.WriteLine("We need " + rows + " rows...");

                //This is a lot more complicated than it needs to be. Basically, keep track of the last Row/Column element and put it either
                // Under the last row if its not null, or Right of the last column if its not null. At the end of the row loop, update our last
                // Row object to the first element in the logical "column". This way I can have an arbitrary layout. 
                ElementBounds[] slotBounds = new ElementBounds[slotCount];
                ElementBounds[] labelBounds = new ElementBounds[slotCount];
                ElementBounds curRow = null;
                for (int row = 0; row < rows; row++)
                {
                    ElementBounds curColumn = null;
                    for (int i = 0; i < slotsPerRow; i++)
                    {
                        int curIn = (row * slotsPerRow) + i;
                        //Console.WriteLine("Currently on index" + curIn);
                        if (curIn >= slotCount)
                        {
                            //TODO: Figure out a LESS fucking complicated way to get out of this loop and avoid IndexOutOfRangeException.
                            break;
                        }


                        ElementBounds curLabelBounds = ElementBounds.FixedSize(75, 20).WithParent(gachaBounds);
                        if (curRow != null)
                        {
                            curLabelBounds.FixedUnder(curRow).WithFixedOffset(0, 10);
                        }
                        ElementBounds curSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(curLabelBounds).WithParent(gachaBounds);//.WithFixedPadding(10)
                        if (curColumn != null)
                        {
                            curLabelBounds.FixedRightOf(curColumn).WithFixedOffset(10, 0);
                            curSlotBounds.FixedRightOf(curColumn).WithFixedOffset(20, 0);
                        }
                        else
                        {
                            curSlotBounds.WithFixedOffset(10, 0);
                        }
                        curColumn = curLabelBounds;

                        slotBounds[curIn] = curSlotBounds;
                        labelBounds[curIn] = curLabelBounds;

                    }
                    int index = (row * slotsPerRow) + slotsPerRow;
                    if (index < slotCount)
                        curRow = slotBounds[index - 1];

                }
                settingBounds.WithChildren(currencyLabel, currencySlotBounds, purchaseButtonBounds, chanceLabel, gachaBounds);
                settingBounds.verticalSizing = ElementSizing.FitToChildren;
                bgBounds.WithChildren( settingBounds);

                //IconUtil.DrawArrowRight

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconStallCustomer", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);


                SingleComposer.BeginChildElements(gachaBounds)
                   .AddStaticText(Lang.Get("vinconomy:gui-deal"), CairoFont.WhiteSmallText(), chanceLabel);

                CairoFont font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);


                for (int i = 0; i < slotBounds.Length; i++)
                {
                    SingleComposer.AddDynamicText("0%", font, labelBounds[i], "percentage" + i);
                    SingleComposer.AddItemSlotGrid(inv, new Action<object>(this.SendInvPacket), 5, new int[] { i + 1 }, slotBounds[i], "inventory" + i);
                    //SingleComposer.AddPassiveItemSlot(slotBounds[i], Inventory, Inventory[i + 1], true);
                }

                SingleComposer.EndChildElements();

                SingleComposer.BeginChildElements(settingBounds)
                   
                        .AddButton(Lang.Get("vinconomy:gui-deal"), new ActionConsumable(this.OnPurchase),purchaseButtonBounds, EnumButtonStyle.Small, "save")
                        .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                        .AddPassiveItemSlot(currencySlotBounds, inv, inv[0], true)

                    .EndChildElements();
 
                SingleComposer.Compose();

                UpdateSlots();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void SendInvPacket(object obj)
        {
            // Dont do shit.
        }

        private bool OnPurchase()
        {
            //capi.Logger.Chat("Attempting to purchase item");
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.PURCHASE_ITEMS, null);
            return true;
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

    }
}
