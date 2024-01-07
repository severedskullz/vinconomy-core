using System;
using System.IO;
using System.Linq;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.src.GUI
{
    public class GuiViconWaypoint : GuiDialogBlockEntity
    {
        private ViconomyCoreSystem modsystem;
        private string[] icons;
        private int[] colors;
        private int ID;

        private bool isVisible;
        private string currentIcon = "genericZShop";
        private int currentColor = 0;

        public GuiViconWaypoint(string dialogTitle, BlockPos blockEntityPos, int id, ICoreClientAPI capi) : base(dialogTitle, blockEntityPos, capi)
        {
            modsystem = capi.ModLoader.GetModSystem<ViconomyCoreSystem>();
            icons = modsystem.ShopMapLayer.WaypointIcons.Keys.ToArray<string>();
            colors = modsystem.ShopMapLayer.WaypointColors.ToArray();
            ID = id;
        }

        public override bool TryOpen()
        {
            this.ComposeDialog();
            return base.TryOpen();
        }

        private void ComposeDialog()
        {
            ElementBounds waypointLabel = ElementBounds.Fixed(0.0, 28.0, 150.0, 25.0);
            ElementBounds waypointVisible = waypointLabel.RightCopy(0, -5);

            ElementBounds colorLabelBounds = ElementBounds.FixedSize(150.0, 25.0).FixedUnder(waypointLabel).WithFixedOffset(0, 10);
            ElementBounds colorRow = ElementBounds.FixedSize(25,25).FixedUnder(colorLabelBounds);

            ElementBounds iconLabelBounds = ElementBounds.Fixed(0.0, 220.0, 150.0, 25.0);
            ElementBounds iconRow = ElementBounds.FixedSize(25.0, 25.0).FixedUnder(iconLabelBounds);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[]
            {
                waypointLabel,
                waypointVisible,
                colorRow,
                iconRow,
                colorLabelBounds,
                iconLabelBounds
            });
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);

            if (base.SingleComposer != null)
            {
                base.SingleComposer.Dispose();
            }
            try
            {
                SingleComposer = capi.Gui.CreateCompo("ViconWaypointEditor", dialogBounds);
                SingleComposer.AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddStaticText("Waypoint Visible:", CairoFont.WhiteSmallText(), waypointLabel)
                    .AddSwitch(new Action<bool>(OnToggleWaypointVisible), waypointVisible, "admin")
                    .AddStaticText("Color:", CairoFont.WhiteSmallText(), colorLabelBounds)
                    .AddColorListPicker(colors, onToggleColor, colorRow, 250, "colorpicker")
                    .AddStaticText("Icon:", CairoFont.WhiteSmallText(), iconLabelBounds)
                    .AddIconListPicker(icons, onToggleIcon, iconRow, 250, "iconpicker")
                .Compose();

                ShopRegistration shop = modsystem.GetRegistry().GetShop(ID);
                if (shop != null  && shop.IsWaypointBroadcasted)
                {
                    //GuiComposerHelpers.GetButton(base.SingleComposer, "saveButton").Enabled = false;
                    GuiComposerHelpers.ColorListPickerSetValue(base.SingleComposer, "colorpicker", colors.IndexOf(shop.WaypointColor));
                    this.currentColor = shop.WaypointColor;
                    GuiComposerHelpers.IconListPickerSetValue(base.SingleComposer, "iconpicker", icons.IndexOf(shop.WaypointIcon));
                    this.currentIcon = shop.WaypointIcon;
                } else
                {
                    GuiComposerHelpers.ColorListPickerSetValue(base.SingleComposer, "colorpicker", 0);
                    this.currentColor = colors[0];
                    GuiComposerHelpers.IconListPickerSetValue(base.SingleComposer, "iconpicker", 0);
                    this.currentIcon = icons[0];
                }

            } catch (Exception ex) { }
        }

        private void OnToggleWaypointVisible(bool visible)
        {
            this.isVisible = visible;
            SendWaypointUpdate();
        }

        private void onToggleIcon(int index)
        {
            this.currentIcon = icons[index];
            SendWaypointUpdate();
        }

        private void onToggleColor(int index)
        {
            this.currentColor = colors[index];
            SendWaypointUpdate();
        }

        private void SendWaypointUpdate()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(isVisible);
                writer.Write(currentIcon);
                writer.Write(currentColor);
                data = ms.ToArray();
            }

            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.SET_WAYPOINT, data);
        }

        private void OnTitleBarCloseClicked()
        {
            this.TryClose();
        }
    }
}
