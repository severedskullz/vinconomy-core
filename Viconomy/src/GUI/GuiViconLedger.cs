using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Viconomy.Network;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Viconomy.GUI
{
    public class GuiViconLedger : GuiDialogGeneric
    {
        VinconomyLedgerSystem modSystem;
        private GuiElementRichtext textElem;
        private GuiElementNumberInput monthElem;
        private GuiElementNumberInput yearElem;
        private ElementBounds clipBounds;

        int shopId;
        int year;
        int month;

        public GuiViconLedger(string DialogTitle, int shopID, ICoreClientAPI capi)
            : base(DialogTitle, capi)
        {
            shopId = shopID;
            modSystem = capi.ModLoader.GetModSystem<VinconomyLedgerSystem>();
            modSystem.OnLedgerData += ModSystem_OnLedgerData;
            this.OnClosed += GuiViconLedger_OnClosed;
            Compose();
            ViewLedger();
        }

        private ItemStack ResolveBlockOrItem(string code, int size)
        {
            AssetLocation location = new AssetLocation(code);
            Item item = capi.World.GetItem(location);
            if (item != null)
            {
                return new ItemStack(item, size);
            }

            Block block = capi.World.GetBlock(location);
            if (block != null)
            {
                return new ItemStack(block, size);
            }

            return null;
        }

        private void ModSystem_OnLedgerData(Dictionary<string, List<LedgerEntry>> data)
        {
            CairoFont font = CairoFont.WhiteSmallText();
            List<RichTextComponent> list = new List<RichTextComponent>();
            try
            {

                if (data != null)
                {
                    foreach (var key in data.Keys)
                    {
                        list.Add(new RichTextComponent(capi, key + ":\r\n", font));
                        foreach (var sale in data[key])
                        {
                            ItemStack product = ResolveBlockOrItem(sale.ProductCode, sale.ProductQuantity);
                            if (sale.ProductAttributes != null)
                            {
                                JsonObject productAttr = JsonObject.FromJson(sale.ProductAttributes);
                                product.Attributes = (ITreeAttribute)productAttr.ToAttribute();
                            }
                            ItemStack currency = ResolveBlockOrItem(sale.CurrencyCode, sale.CurrencyQuantity);
                            if (sale.CurrencyAttributes != null)
                            {
                                JsonObject currencyAttr = JsonObject.FromJson(sale.CurrencyAttributes);
                                currency.Attributes = (ITreeAttribute)currencyAttr.ToAttribute();
                            }
                            list.Add(new RichTextComponent(capi, "\t" + product.GetName() + " x" + product.StackSize + " sold for " + currency.GetName() + " x" + currency.StackSize + "\r\n", font));
                        }
                        list.Add(new RichTextComponent(capi, "\r\n", font));
                    }
                    list.Add(new RichTextComponent(capi, "\r\n", font));
                }
                else
                {
                    list.Add(new RichTextComponent(capi, "There are no sales this month.", font));
                }
            } catch(Exception e) {
                list.Add(new RichTextComponent(capi, "There was a problem loading the ledger info - Please forward this to the developer, so he can fix it: " + e.Message, font));
            }
           
            textElem.SetNewText(list.ToArray());
            updateScrollbarBounds();
        }

        private void GuiViconLedger_OnClosed()
        {
           modSystem.OnLedgerData -= ModSystem_OnLedgerData;
        }

        private void Compose()
        {

            try
            {
                int w = 500;
                int h = 350;

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);


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

                ElementBounds buttonBounds = ElementBounds.FixedSize(90, 25).WithFixedOffset(w-65, 0);




                ElementBounds textBounds = ElementBounds.Fixed(0, 0, w, h).FixedUnder(monthLabelBounds).WithFixedOffset(0,10);
                clipBounds = textBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0);
                ElementBounds insetBounds = textBounds.FlatCopy().FixedGrow(3.0).WithFixedOffset(-2.0, -2.0);

                ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(textBounds).FixedUnder(monthLabelBounds).WithFixedOffset(0, 10);

                settingBounds.WithChildren(monthSelectBounds, monthLabelBounds, yearSelectionBounds, yearLabelBounds, scrollbarBounds);
                //settingBounds.verticalSizing = ElementSizing.FitToChildren;

                
                bgBounds.WithChildren(settingBounds);

                CairoFont font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconLedger", dialogBounds)
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

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

      
    }
}
