using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vinconomy.GUI
{
    public class GuiVinconTenretniWritten : GuiDialogGeneric
    {
        private ElementBounds clipBounds;
        private GuiElementRichtext textElem;
        private string BookText;

        public GuiVinconTenretniWritten(string DialogTitle, string bookText, ICoreClientAPI capi) : base(DialogTitle, capi)
        {
            BookText = bookText;
            //this.OnClosed += GuiViconLedger_OnClosed;
            Compose();
        }

        private void Compose()
        {

            try
            {
                int w = 800;
                int h = 600;

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                ElementBounds settingBounds = ElementBounds.FixedSize(400, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                settingBounds.BothSizing = ElementSizing.FitToChildren;

                ElementBounds textBounds = ElementBounds.Fixed(0, 0, w, h).WithFixedOffset(0, 2);
                clipBounds = textBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0);

                ElementBounds insetBounds = textBounds.FlatCopy().FixedGrow(3.0).WithFixedOffset(-2.0, -2.0);
                ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(textBounds).WithFixedOffset(0, 2);

                settingBounds.WithChildren(scrollbarBounds);
                bgBounds.WithChildren(settingBounds);

                SingleComposer = capi.Gui.CreateCompo("ViconLedger", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                SingleComposer.BeginChildElements(settingBounds)
                    .BeginClip(clipBounds)
                        .AddInset(insetBounds)
                        .AddRichtext(BookText, CairoFont.WhiteSmallText(), textBounds.WithFixedPadding(5.0).WithFixedSize(w - 10,h - 10), "tenretni")
                    .EndClip()
                    .AddVerticalScrollbar(onScrollbarValueChanged, scrollbarBounds, "scrollbar")
                .EndChildElements();


                textElem = SingleComposer.GetRichtext("tenretni");
                SingleComposer.Compose();

                ViewText();
                updateScrollbarBounds();
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private bool ViewText()
        {
            try {
                textElem.SetNewText(BookText, CairoFont.WhiteSmallText());
            } catch {
                textElem.SetNewText(Lang.Get("vinconomy:gui-error-parsing"), CairoFont.WhiteSmallText());
            }
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


        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }


    }
}
