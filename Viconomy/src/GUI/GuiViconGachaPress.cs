using System;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiViconGachaPress : GuiDialogBlockEntity
    {
        BEVinconGachaLoader stall;
        InventoryBase vinInv;

        public GuiViconGachaPress(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconGachaLoader>(BlockEntityPosition);
            vinInv = Inventory;
           
            
            
            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;
            this.Compose();
        }

        private void Compose()
        {

            try { 
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);


                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;



                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0,GuiStyle.TitleBarHeight);
            
                //settingBounds.BothSizing = ElementSizing.FitToChildren;

                // Auto-sized dialog at the center of the screen
                //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
                ElementBounds nameSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
                ElementBounds nameSelectionBounds = nameSelectionLabel.BelowCopy().WithFixedWidth(250);      


                ElementBounds inputLabel = ElementBounds.FixedSize(100, 25).FixedUnder(nameSelectionBounds).WithFixedOffset(40, 15);
                ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(inputLabel).WithFixedOffset(40, 00);

                ElementBounds outputLabel = ElementBounds.FixedSize(100, 25).FixedUnder(nameSelectionBounds).FixedRightOf(inputLabel).WithFixedOffset(20, 15);
                ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(outputLabel).FixedRightOf(inputLabel).WithFixedOffset(20, 00);


                ElementBounds bundleBounds = ElementBounds.FixedSize(100, 25).FixedUnder(outputSlotBounds).WithFixedOffset(10, 15);
                ElementBounds bundleAllBounds = ElementBounds.FixedSize(100, 25).FixedRightOf(bundleBounds).FixedUnder(outputSlotBounds).WithFixedOffset(10, 15);

                ElementBounds itemPage = ElementBounds.FixedSize(200, 50).FixedRightOf(settingBounds).WithFixedOffset(15, GuiStyle.TitleBarHeight);
                itemPage.BothSizing = ElementSizing.FitToChildren;



                ElementBounds itemsPerLabel = ElementBounds.FixedSize(220, 25);
                ElementBounds gachaBounds = ElementBounds.FixedSize(100, 300).FixedUnder(itemsPerLabel).WithFixedOffset(0,5);
                gachaBounds.BothSizing = ElementSizing.FitToChildren;
                int slotCount = 6;
                int slotsPerRow = 3;
                int rows = (int)Math.Ceiling(((double)slotCount) / slotsPerRow);
                //Console.WriteLine("We need " + rows + " rows...");

                //This is a lot more complicated than it needs to be. Basically, keep track of the last Row/Column element and put it either
                // Under the last row if its not null, or Right of the last column if its not null. At the end of the row loop, update our last
                // Row object to the first element in the logical "column". This way I can have an arbitrary layout. 
                ElementBounds[] slotBounds = new ElementBounds[slotCount];
                ElementBounds[] amountSelectionBounds = new ElementBounds[slotCount];
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
                        ElementBounds curSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).WithParent(gachaBounds);//.WithFixedPadding(10)
                        if (curRow != null)
                        {
                            curSlotBounds.FixedUnder(curRow).WithFixedOffset(0, 45);
                        }
                        ElementBounds curLabelBounds = ElementBounds.FixedSize(60, 30).FixedUnder(curSlotBounds).WithFixedOffset(0,5).WithParent(gachaBounds);
                        
                        if (curColumn != null)
                        {
                            curSlotBounds.FixedRightOf(curColumn).WithFixedOffset(25, 0);
                            curLabelBounds.FixedRightOf(curColumn).WithFixedOffset(20, 0);
                        } else
                        {
                            curSlotBounds.WithFixedOffset(5, 0);
                        }
                        curColumn = curLabelBounds;

                        slotBounds[curIn] = curSlotBounds;
                        amountSelectionBounds[curIn] = curLabelBounds;
                    }
                    int index = (row * slotsPerRow) + slotsPerRow;
                    if (index < slotCount)
                        curRow = slotBounds[index - 1];

                }

                itemPage.WithChildren(itemsPerLabel, gachaBounds);
                settingBounds.WithChildren(nameSelectionBounds, nameSelectionLabel, inputLabel, inputSlotBounds, outputLabel, outputSlotBounds);
                settingBounds.BothSizing = ElementSizing.FitToChildren;
                bgBounds.WithChildren(settingBounds, itemPage);

                SingleComposer = capi.Gui.CreateCompo("ViconStallOwner", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                CairoFont hoverText = CairoFont.WhiteDetailText();
                CairoFont smallText = CairoFont.WhiteSmallText();

                SingleComposer.BeginChildElements(settingBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-name"), smallText, nameSelectionLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-gacha-name"), hoverText, 500, nameSelectionLabel)
                    .AddTextInput(nameSelectionBounds, OnTextChanged, smallText, "bundleName")
                    .AddStaticText(Lang.Get("vinconomy:gui-gacha"), smallText, inputLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-gacha-input"), hoverText, 500, inputLabel)
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { 1 }, inputSlotBounds, "input")
                    .AddStaticText(Lang.Get("vinconomy:gui-output"), smallText, outputLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-gacha-output"), hoverText, 500, outputLabel)
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { 0 }, outputSlotBounds, "output")
                    .AddButton(Lang.Get("vinconomy:gui-bundle"), new ActionConsumable(this.BundleItems), bundleBounds, EnumButtonStyle.Small, "bundle")
                    .AddButton(Lang.Get("vinconomy:gui-bundle-all"), new ActionConsumable(this.BundleAllItems), bundleAllBounds, EnumButtonStyle.Small, "bundleAll")
                .EndChildElements();

                CairoFont font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                SingleComposer.BeginChildElements(gachaBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-items-per-gacha"), font, itemsPerLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-items-per-gacha"), hoverText, 500, itemsPerLabel);

                for (int i = 0; i < slotBounds.Length; i++)
                {
                    int index = i;
                    Action<string> lambda = (string obj) => { OnAmountChanged(index, obj); };
                    //Action<string> lambda = new Action<string>(string obj) {  };
                    SingleComposer.AddNumberInput(amountSelectionBounds[i], lambda , font, "amount" + i);
                    SingleComposer.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 5, new int[] { i + 2}, slotBounds[i], "inventory" + i);
                }

                SingleComposer.EndChildElements();

                SingleComposer.GetTextInput("bundleName").SetValue(stall.GachaName);
                for (int i = 0; i < slotBounds.Length; i++)
                {
                   
                    GuiElementNumberInput input = SingleComposer.GetNumberInput("amount" + i);
                    input.SetValue(stall.ItemsPerSlot[i]);
                }

                SingleComposer.Compose();


            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private bool BundleAllItems()
        {
            RequestItemBundle(999);
            return true;
        }

        private bool BundleItems()
        {
            RequestItemBundle(1);
            return true;
        }

        private void RequestItemBundle(int amount)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(amount);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.PURCHASE_ITEMS, data);
        }

        private void OnAmountChanged(int i, string amountStr)
        {
            if (!SingleComposer.Composed)
            {
                return;
            }

            int amount = 0;

            try
            {
                amount = Int32.Parse(amountStr);
            } catch
            {
            }
            byte[] data;
            
            if (amount <= 0)
            {
                amount = 0;
                GuiElementNumberInput input = SingleComposer.GetNumberInput("amount" + i);
                Action<string> dele = input.OnTextChanged;
                input.OnTextChanged -= dele; // Stupid fucking hack to prevent stack overflow and change a value manually...
                input.SetValue(amount);
                input.OnTextChanged += dele;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(i);
                writer.Write(amount);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.SET_ITEMS_PER_PURCHASE, data);

        }

        private void OnTextChanged(string obj)
        {
            if (!SingleComposer.Composed)
            {
                return;
            }

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(obj);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.SET_ITEM_NAME, data);
        }

        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
        }

    }
}
