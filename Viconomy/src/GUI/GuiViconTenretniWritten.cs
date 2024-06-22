using System;
using Vintagestory.API.Client;

namespace Viconomy.GUI
{
    public class GuiViconTenretniWritten : GuiDialogGeneric
    {
        private ElementBounds clipBounds;
        private GuiElementRichtext textElem;
        CairoFont font;
        private string BookText;

        public GuiViconTenretniWritten(string DialogTitle, string bookText, ICoreClientAPI capi) : base(DialogTitle, capi)
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

                ElementBounds buttonBounds = ElementBounds.FixedSize(120, 25).WithFixedOffset(w - 95, GuiStyle.TitleBarHeight);

                ElementBounds textBounds = ElementBounds.Fixed(0, 0, w, h)
                    //.FixedUnder(buttonBounds)
                    .WithFixedOffset(0, 2);
                clipBounds = textBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0);
                ElementBounds insetBounds = textBounds.FlatCopy().FixedGrow(3.0).WithFixedOffset(-2.0, -2.0);

                ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(textBounds)
                    //.FixedUnder(buttonBounds)
                    .WithFixedOffset(0, 2);

                settingBounds.WithChildren(scrollbarBounds);
                //settingBounds.verticalSizing = ElementSizing.FitToChildren;


                bgBounds.WithChildren(buttonBounds, settingBounds);

                font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconLedger", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                SingleComposer.BeginChildElements(settingBounds)
                    //.AddButton("Refresh", RefreshText, buttonBounds)
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

        private bool RefreshText()
        {
            throw new NotImplementedException();
        }

        private bool ViewText()
        {
            try {
                textElem.SetNewText(BookText, CairoFont.WhiteSmallText());
                //textElem.RecomposeText();
                Console.WriteLine("It worked?");
            } catch {
                textElem.SetNewText("There was a problem loading RichText. Perhaps there is a problem with the source document", CairoFont.WhiteSmallText());
                Console.WriteLine("It didnt work");
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
