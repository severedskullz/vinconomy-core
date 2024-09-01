using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Viconomy.Config;
using Viconomy.Network;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Viconomy.GUI
{
    public class GuiViconTenretniUnwritten : GuiDialogGeneric
    {
        private ElementBounds clipBounds;
        private GuiElementRichtext textElem;
        CairoFont font;
        private VinconomyCoreSystem modSystem;
        private string BookText;
        private int endpointSelectionIndex;
        private Task task;
        private string[] endpointNames;

        public GuiViconTenretniUnwritten(string DialogTitle, ICoreClientAPI capi) : base(DialogTitle, capi)
        {
            modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();
            //this.OnClosed += GuiViconLedger_OnClosed;
            BookText = string.Empty;
            Compose();
        }

        private void Compose()
        {

            try
            {
                int w = 800;
                int h = 600;

                List<string> baseURLs = new List<string>();
                List<string> names = new List<string>();
                Config.ViconTenretniWhitelist[] whitelist = modSystem.Config.ViconTenretniWhitelists;

                if (whitelist != null)
                {
                    for (int i = 0; i < whitelist.Length; i++)
                    {
                        baseURLs.Add(whitelist[i].baseURL);
                        names.Add(whitelist[i].name);
                    }
                } else
                {
                    baseURLs.Add("");
                    names.Add("No Configured Archives");
                }



                string[] endpointKeys = baseURLs.ToArray();
                endpointNames = names.ToArray();

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);


                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;


                ElementBounds settingBounds = ElementBounds.FixedSize(400, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                settingBounds.BothSizing = ElementSizing.FitToChildren;

                ElementBounds nameLabelBounds = ElementBounds.FixedSize(60, 25).WithFixedOffset(0, 10);
                ElementBounds nameBounds = ElementBounds.FixedSize(300, 30).FixedRightOf(nameLabelBounds).WithFixedOffset(10, 5);


                // Auto-sized dialog at the center of the screen
                //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
                ElementBounds baseURLLabelBounds = ElementBounds.FixedSize(60, 25).FixedUnder(nameLabelBounds).WithFixedOffset(0, 10);
                ElementBounds baseURLSelectBounds = ElementBounds.FixedSize(200, 30).FixedUnder(nameLabelBounds).FixedRightOf(baseURLLabelBounds).WithFixedOffset(10, 5);

                ElementBounds idLabelBounds = ElementBounds.FixedSize(30, 25).FixedUnder(nameLabelBounds).FixedRightOf(baseURLSelectBounds).WithFixedOffset(10, 10);
                ElementBounds idSelectionBounds = ElementBounds.FixedSize(300, 30).FixedUnder(nameLabelBounds).FixedRightOf(idLabelBounds).WithFixedOffset(10, 5);

                ElementBounds buttonBounds = ElementBounds.FixedSize(120, 25).FixedUnder(nameLabelBounds).WithFixedOffset(w - 95, 0);


                ElementBounds textBounds = ElementBounds.Fixed(0, 0, w, h).FixedUnder(buttonBounds).WithFixedOffset(0, 20);
                clipBounds = textBounds.ForkBoundingParent(0.0, 0.0, 0.0, 0.0);
                ElementBounds insetBounds = textBounds.FlatCopy().FixedGrow(3.0).WithFixedOffset(-2.0, -2.0);

                ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(textBounds).FixedUnder(buttonBounds).WithFixedOffset(0, 20);

                settingBounds.WithChildren(scrollbarBounds);
                //settingBounds.verticalSizing = ElementSizing.FitToChildren;

                ElementBounds saveButtonBounds = ElementBounds.FixedSize(120, 25).FixedUnder(scrollbarBounds).WithFixedOffset((w /2) - 60, 12);

                bgBounds.WithChildren( settingBounds);

                CairoFont defaultFont = CairoFont.WhiteSmallText();
                //CairoFont centeredFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconTenretni", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                SingleComposer.BeginChildElements(settingBounds)
                    .AddStaticText("Name:", defaultFont, nameLabelBounds)
                    .AddTextInput(nameBounds, new Action<string>(onInputChanged), defaultFont, "name")

                    .AddButton("Test", RefreshText, buttonBounds,EnumButtonStyle.Normal, "testHTTP")
                    .AddStaticText("Archive:", defaultFont, baseURLLabelBounds)
                    .AddDropDown(endpointKeys, endpointNames, endpointSelectionIndex, new SelectionChangedDelegate(this.onEndpointSelectionChanged), baseURLSelectBounds, "archive")
                    .AddStaticText("ID:", defaultFont, idLabelBounds)
                    .AddTextInput(idSelectionBounds, new Action<string>(onInputChanged), defaultFont, "id")
                    
                    .BeginClip(clipBounds)
                        .AddInset(insetBounds)
                        .AddRichtext(BookText, CairoFont.WhiteSmallText(), textBounds.WithFixedPadding(5.0).WithFixedSize(w - 10,h - 10), "tenretni")
                    .EndClip()
                    .AddVerticalScrollbar(onScrollbarValueChanged, scrollbarBounds, "scrollbar")
                    .AddButton("Save", SaveText, saveButtonBounds)
                .EndChildElements();


                textElem = SingleComposer.GetRichtext("tenretni");
                SingleComposer.Compose();

                updateScrollbarBounds();
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }


        }

        private bool SaveText()
        {


            //Thanks Tyron for this convoluted as fuck implementation to get the Text instead of setting Text!
            // SingleComposer.GetDropDown("archive").Text or GetText() is always blank.
            GuiElementDropDown archiveDropDown = SingleComposer.GetDropDown("archive");
            int index = archiveDropDown.SelectedIndices[0];
            string archive = endpointNames[index];

            TenretniPacket packet = new TenretniPacket()
            {
                Name = SingleComposer.GetTextInput("name").GetText(),
                BaseURL = archiveDropDown.SelectedValue,
                Archive = archive,
                ID = SingleComposer.GetTextInput("id").GetText()
            };
            capi.Network.GetChannel("Vinconomy").SendPacket(packet);
            TryClose();
            return true;
        }

        private void onInputChanged(string obj)
        {
            //
        }

        private void onEndpointSelectionChanged(string code, bool selected)
        {
            //
        }

        private bool RefreshText()
        {
            string baseURL = SingleComposer.GetDropDown("archive").SelectedValue;
            string id = SingleComposer.GetTextInput("id").GetText();
            SingleComposer.GetButton("testHTTP").Enabled = false;
            task = VinUtils.GetAsync(baseURL + id, postHandlder);
            return true;
        }

        private void postHandlder(CompletedArgs args)
        {
           
            capi.Event.EnqueueMainThreadTask(() => { //Console.WriteLine(args.Response.ToString());
                SingleComposer.GetButton("testHTTP").Enabled = true;

                if (args.StatusCode == 404 || args.ErrorMessage != "OK") {
                    BookText = "The requested Archive Address could not be read.";
                } else
                {
                    BookText = args.Response;
                }

                
                UpdateText();
                task = null;
            }, "HttpRequestCallback");
        }

        private bool UpdateText()
        {
            try {
                textElem.SetNewText(BookText, CairoFont.WhiteSmallText());
                //textElem.RecomposeText();
                //Console.WriteLine("It worked?");
            } catch {
                textElem.SetNewText("There was a problem loading RichText. Perhaps there is a problem with the source document", CairoFont.WhiteSmallText());
                //Console.WriteLine("It didnt work");
            }
            updateScrollbarBounds();
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
