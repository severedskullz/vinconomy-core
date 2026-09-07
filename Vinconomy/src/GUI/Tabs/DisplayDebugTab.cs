using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.Util;
using Commercially.Vinconomy.BlockEntityBehaviors.DisplayProviders;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Commercially.Vinconomy.GUI.Tabs
{
    public class DisplayDebugTab : ModularTab
    {
        public const string CODE = "Vinconomy.DisplayDebug";
        public override string Code => CODE;

        BaseDisplayContentsBehavior Display;
        private int StallSlot;
        private bool isUpdating;

        public override string TabName => Lang.Get("vinconomy:tabname-displaydebug");

        public override void Initialize(IModularGui gui, BlockEntity entity = null)
        {
            base.Initialize(gui, entity);
            Display = entity?.GetBehavior<BaseDisplayContentsBehavior>();
        }

        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            if (Display != null)
            {
                CairoFont hoverText = CairoFont.WhiteDetailText();
                CairoFont smallText = CairoFont.WhiteSmallText();
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                string labelText = Lang.Get("vinconomy:gui-slot", [StallSlot + 1, Display.TfData.Length]);

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.LeftTop);
                ElementBounds pageLabel = ElementBounds.FixedSize(50, 25).WithFixedAlignmentOffset(0, 10).WithAlignment(EnumDialogArea.CenterTop);

                //string labelText = Lang.Get("vinconomy:gui-slot", new object[] { StallSlot + 1, StallProvider.StallCount });
                labelTextFont.AutoBoxSize(labelText, pageLabel, true);

                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).WithAlignment(EnumDialogArea.RightTop);

                ElementBounds labelX = ElementBounds.FixedSize(60, 25).FixedUnder(pagePrev);
                ElementBounds inputX = ElementBounds.FixedSize(60, 25).FixedUnder(labelX);

                ElementBounds labelY = ElementBounds.FixedSize(60, 25).FixedUnder(pagePrev).FixedRightOf(labelX);
                ElementBounds inputY = ElementBounds.FixedSize(60, 25).FixedUnder(labelY).FixedRightOf(labelX);

                ElementBounds labelZ = ElementBounds.FixedSize(60, 25).FixedUnder(pagePrev).FixedRightOf(labelY);
                ElementBounds inputZ = ElementBounds.FixedSize(60, 25).FixedUnder(labelZ).FixedRightOf(labelY);
                settingBounds.WithChildren(labelX, inputX, labelY, inputY, labelZ, inputZ);

                ElementBounds labelRotX = ElementBounds.FixedSize(60, 25).FixedUnder(inputX);
                ElementBounds inputRotX = ElementBounds.FixedSize(60, 25).FixedUnder(labelRotX);

                ElementBounds labelRotY = ElementBounds.FixedSize(60, 25).FixedUnder(inputX).FixedRightOf(labelRotX);
                ElementBounds inputRotY = ElementBounds.FixedSize(60, 25).FixedUnder(labelRotY).FixedRightOf(labelRotX);

                ElementBounds labelRotZ = ElementBounds.FixedSize(60, 25).FixedUnder(inputX).FixedRightOf(labelRotY);
                ElementBounds inputRotZ = ElementBounds.FixedSize(60, 25).FixedUnder(labelRotZ).FixedRightOf(labelRotY);
                settingBounds.WithChildren(labelRotX, inputRotX, labelRotY, inputRotY, labelRotZ, inputRotZ);

                ElementBounds labelScaleX = ElementBounds.FixedSize(60, 25).FixedUnder(inputRotX);
                ElementBounds inputScaleX = ElementBounds.FixedSize(60, 25).FixedUnder(labelScaleX);

                ElementBounds labelScaleY = ElementBounds.FixedSize(60, 25).FixedUnder(inputRotX).FixedRightOf(labelScaleX);
                ElementBounds inputScaleY = ElementBounds.FixedSize(60, 25).FixedUnder(labelScaleY).FixedRightOf(labelScaleX);

                ElementBounds labelScaleZ = ElementBounds.FixedSize(60, 25).FixedUnder(inputRotX).FixedRightOf(labelScaleY);
                ElementBounds inputScaleZ = ElementBounds.FixedSize(60, 25).FixedUnder(labelScaleZ).FixedRightOf(labelScaleY);
                settingBounds.WithChildren(labelScaleX, inputScaleX, labelScaleY, inputScaleY, labelScaleZ, inputScaleZ);

                settingBounds.verticalSizing = ElementSizing.FitToChildren;

                rootBounds.WithChildren(settingBounds);


                composer.BeginChildElements(settingBounds)
                    .AddButton("<", new ActionConsumable(this.PreviousPage), pagePrev, EnumButtonStyle.Small, "prevPage")
                    .AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel")
                    .AddButton(">", new ActionConsumable(this.NextPage), pageNext, EnumButtonStyle.Small, "nextPage")

                    .AddStaticText("X", CairoFont.WhiteSmallText(), labelX)
                    .AddNumberInput(inputX, UpdateTfData, CairoFont.WhiteSmallText(), "inputX")
                    .AddStaticText("Y", CairoFont.WhiteSmallText(), labelY)
                    .AddNumberInput(inputY, UpdateTfData, CairoFont.WhiteSmallText(), "inputY")
                    .AddStaticText("Z", CairoFont.WhiteSmallText(), labelZ)
                    .AddNumberInput(inputZ, UpdateTfData, CairoFont.WhiteSmallText(), "inputZ")

                    .AddStaticText("Rot X", CairoFont.WhiteSmallText(), labelRotX)
                    .AddNumberInput(inputRotX, UpdateTfData, CairoFont.WhiteSmallText(), "inputRotX")
                    .AddStaticText("Rot Y", CairoFont.WhiteSmallText(), labelRotY)
                    .AddNumberInput(inputRotY, UpdateTfData, CairoFont.WhiteSmallText(), "inputRotY")
                    .AddStaticText("Rot Z", CairoFont.WhiteSmallText(), labelRotZ)
                    .AddNumberInput(inputRotZ, UpdateTfData, CairoFont.WhiteSmallText(), "inputRotZ")

                    .AddStaticText("Scale X", CairoFont.WhiteSmallText(), labelScaleX)
                    .AddNumberInput(inputScaleX, UpdateTfData, CairoFont.WhiteSmallText(), "inputScaleX")
                    .AddStaticText("Scale Y", CairoFont.WhiteSmallText(), labelScaleY)
                    .AddNumberInput(inputScaleY, UpdateTfData, CairoFont.WhiteSmallText(), "inputScaleY")
                    .AddStaticText("Scale Z", CairoFont.WhiteSmallText(), labelScaleZ)
                    .AddNumberInput(inputScaleZ, UpdateTfData, CairoFont.WhiteSmallText(), "inputScaleZ")
                .EndChildElements();

                UpdatePageValues();

            }
            else
            {
                ElementBounds textBounds = ElementBounds.FixedSize(400, 300).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(textBounds);
                composer.AddStaticText(Lang.Get("vinconomy:not-a-shop"), CairoFont.WhiteSmallText(), textBounds);
            }
        }

        public void UpdatePageValues()
        {
            GuiComposer composer = Gui.Composer;
            isUpdating = true;

            composer.GetDynamicText("pageLabel").SetNewText(Lang.Get("vinconomy:gui-slot", [StallSlot + 1, Display.TfData.Length]));
            composer.GetNumberInput("inputX").SetValue(Display.TfData[StallSlot].X);
            composer.GetNumberInput("inputY").SetValue(Display.TfData[StallSlot].Y);
            composer.GetNumberInput("inputZ").SetValue(Display.TfData[StallSlot].Z);

            composer.GetNumberInput("inputRotX").SetValue(Display.TfData[StallSlot].RotX);
            composer.GetNumberInput("inputRotY").SetValue(Display.TfData[StallSlot].RotY);
            composer.GetNumberInput("inputRotZ").SetValue(Display.TfData[StallSlot].RotZ);

            composer.GetNumberInput("inputScaleX").SetValue(Display.TfData[StallSlot].ScaleX);
            composer.GetNumberInput("inputScaleY").SetValue(Display.TfData[StallSlot].ScaleY);
            composer.GetNumberInput("inputScaleZ").SetValue(Display.TfData[StallSlot].ScaleZ);
            isUpdating = false;

        }

        private void UpdateTfData(string obj)
        {
            if (!Gui.Composer.Composed || isUpdating) return;
            float result;
            if (!float.TryParse(obj, out result)) return;

            Display.TfData[StallSlot].X = Gui.Composer.GetNumberInput("inputX").GetValue();
            Display.TfData[StallSlot].Y = Gui.Composer.GetNumberInput("inputY").GetValue();
            Display.TfData[StallSlot].Z = Gui.Composer.GetNumberInput("inputZ").GetValue();

            Display.TfData[StallSlot].RotX = Gui.Composer.GetNumberInput("inputRotX").GetValue();
            Display.TfData[StallSlot].RotY = Gui.Composer.GetNumberInput("inputRotY").GetValue();
            Display.TfData[StallSlot].RotZ = Gui.Composer.GetNumberInput("inputRotZ").GetValue();

            Display.TfData[StallSlot].ScaleX = Gui.Composer.GetNumberInput("inputScaleX").GetValue();
            Display.TfData[StallSlot].ScaleY = Gui.Composer.GetNumberInput("inputScaleY").GetValue();
            Display.TfData[StallSlot].ScaleZ = Gui.Composer.GetNumberInput("inputScaleZ").GetValue();

            Display.Blockentity.MarkDirty(true);

        }

        private bool PreviousPage()
        {
            StallSlot -= 1;
            StallSlot = Math.Max(0, StallSlot);
            UpdatePageValues();
            return true;
        }

        private bool NextPage()
        {
            StallSlot += 1;
            StallSlot = Math.Min(Display.TfData.Length - 1, StallSlot);
            UpdatePageValues();
            return true;
        }


        public override bool IsVisible()
        {
            return CommUtils.IsCreativePlayer(ClientApi.World.Player);
        }

        public override void OnGuiClosed()
        {
           
        }

        public override void OnGuiOpened()
        {
           
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
