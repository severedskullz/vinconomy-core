using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.GUI
{
    public class GuiViconLedger : GuiDialogGeneric
    {
        ViconomyLedgerSystem modSystem;
        private GuiElementRichtext textElem;
        private GuiElementNumberInput monthElem;
        private GuiElementNumberInput yearElem;
        private ElementBounds clipBounds;

        int shopId;
        int year;
        int month;

        public GuiViconLedger(string DialogTitle, ICoreClientAPI capi)
            : base(DialogTitle, capi)
        {
            shopId = 1; // TODO: get from item.
            modSystem = capi.ModLoader.GetModSystem<ViconomyLedgerSystem>();
            modSystem.OnLedgerData += ModSystem_OnLedgerData;
            this.OnClosed += GuiViconLedger_OnClosed;
            Compose();
        }

        private void ModSystem_OnLedgerData(System.Collections.Generic.Dictionary<string, Network.LedgerEntry> data)
        {
            textElem.SetNewText("Data has loaded!", CairoFont.WhiteSmallText());
        }

        private void GuiViconLedger_OnClosed()
        {
           modSystem.OnLedgerData -= ModSystem_OnLedgerData;
        }

        private void Compose()
        {

            try
            {
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);


                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;


                ElementBounds settingBounds = ElementBounds.FixedSize(400, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                settingBounds.BothSizing = ElementSizing.FitToChildren;

                // Auto-sized dialog at the center of the screen
                //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
                ElementBounds monthLabelBounds = ElementBounds.FixedSize(60, 25).WithFixedOffset(0, 10);
                ElementBounds monthSelectBounds = ElementBounds.FixedSize(75, 30).FixedRightOf(monthLabelBounds).WithFixedOffset(10,5);

                ElementBounds yearLabelBounds = ElementBounds.FixedSize(60, 25).FixedRightOf(monthSelectBounds).WithFixedOffset(10,10);
                ElementBounds yearSelectionBounds = ElementBounds.FixedSize(75, 30).FixedRightOf(yearLabelBounds).WithFixedOffset(10,5);

                ElementBounds buttonBounds = ElementBounds.FixedSize(90, 25).FixedRightOf(yearSelectionBounds).WithFixedOffset(15, 0);



                int w = 380;
                int h = 350;
                ElementBounds textBounds = ElementBounds.Fixed(0, 0, w, h).FixedUnder(monthLabelBounds).WithFixedOffset(0,10);
                clipBounds = textBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0);
                ElementBounds insetBounds = textBounds.FlatCopy().FixedGrow(3.0).WithFixedOffset(-2.0, -2.0);

                ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(textBounds).FixedUnder(monthLabelBounds).WithFixedOffset(0, 10);

                settingBounds.WithChildren(monthSelectBounds, monthLabelBounds, yearSelectionBounds, yearLabelBounds, scrollbarBounds);
                //settingBounds.verticalSizing = ElementSizing.FitToChildren;

                
                bgBounds.WithChildren(settingBounds);

                CairoFont font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("StallOwner", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                SingleComposer.BeginChildElements(settingBounds)
                    .AddStaticText("Month:", font, monthLabelBounds)
                    .AddNumberInput(monthSelectBounds, new Action<string>(onMonthChanged), CairoFont.WhiteSmallText(), "month")
                    .AddStaticText("Year:", font, yearLabelBounds)
                    .AddNumberInput(yearSelectionBounds, new Action<string>(onYearChanged), CairoFont.WhiteSmallText(), "year")
                    .AddButton("View", ViewLedger, buttonBounds)
                    .BeginClip(clipBounds)
                    .AddInset(insetBounds)
                    .AddRichtext("", CairoFont.WhiteSmallText(), textBounds.WithFixedPadding(5.0).WithFixedSize((double)(w - 10), (double)(h - 10)), "ledgerText")

                    .EndClip()
                    .AddVerticalScrollbar(onScrollbarValueChanged, scrollbarBounds, "scrollbar")
                .EndChildElements();

              

                //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
                SingleComposer.Compose();

                textElem = SingleComposer.GetRichtext("ledgerText");
                monthElem = SingleComposer.GetNumberInput("month");
                yearElem = SingleComposer.GetNumberInput("year");

                monthElem.SetValue(capi.World.Calendar.Month);
                yearElem.SetValue(capi.World.Calendar.Year);

                updateScrollbarBounds();

            }
            catch (Exception e) { 
                Console.WriteLine(e.ToString());
            }
            

        }

        private bool ViewLedger()
        {
            textElem.SetNewText("Loading...", CairoFont.WhiteSmallText());
            modSystem.RequestLedgerData(shopId, month, year);
            return true;
        }

        private void updateScrollbarBounds()
        {
            if (textElem == null)
            {
                return;
            }
            GuiElementScrollbar scrollbar = SingleComposer.GetScrollbar("scrollbar");
            scrollbar.Bounds.CalcWorldBounds();
            scrollbar.SetHeights((float)clipBounds.fixedHeight, (float)textElem.Bounds.fixedHeight);
            scrollbar.SetScrollbarPosition(0);
        }


        private void onScrollbarValueChanged(float value)
        {
            textElem.Bounds.fixedY = (double)(0f - value);
            textElem.Bounds.CalcWorldBounds();
        }

        private void onMonthChanged(string amount)
        {

            if (amount == month.ToString())
            {
                return;
            }
            Int32.TryParse(amount, out month);
            if (month <= 0 && year > 0) {
                month = 12;
                year--;
                yearElem.SetValue(year);
            } else if (month <= 0) {
                month = 1;
            } else if (month > 12) {
                month = 1;
                year++;
                yearElem.SetValue(year);
            } 

            monthElem.SetValue(month);

        }

        private void onYearChanged(string amount)
        {
            if (amount == year.ToString())
            {
                return;
            }
            Int32.TryParse(amount, out year);
            if (year < 0)
                year = 0;
            yearElem.SetValue(year);
        }


        private void SendInvPacket(object p)
        {
            //capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

      
    }
}
