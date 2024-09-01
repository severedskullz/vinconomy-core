using System;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiViconRegister : GuiDialogBlockEntity
    {
        private string name;

        public GuiViconRegister(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            name = DialogTitle;

            VinconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();
            //stall = capi.World.BlockAccessor.GetBlockEntity<BEVRegister>(BlockEntityPosition); 
            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
           
        }

        private void Compose()
        {  
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            ElementBounds shopNameLabelBounds = ElementBounds.Fixed(0, 35, 250, 25);
            ElementBounds shopNameInputBounds = ElementBounds.FixedSize(250, 25).FixedUnder(shopNameLabelBounds);
            ElementBounds shopNameUpdateBounds = ElementBounds.FixedSize(60, 20).FixedUnder(shopNameInputBounds,10).WithAlignment(EnumDialogArea.RightTop);

            ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 40, (int)Math.Ceiling(Math.Sqrt(Inventory.Count)), (int)Math.Ceiling(Math.Sqrt(Inventory.Count))).FixedUnder(shopNameUpdateBounds);

            bgBounds.WithChildren(shopNameLabelBounds, shopNameInputBounds, shopNameUpdateBounds, slotGrid);

            //IconUtil.DrawArrowRight

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                .AddStaticText("Shop Name:", CairoFont.WhiteSmallText(), shopNameLabelBounds)
                .AddButton("Save", OnSavePressed, shopNameUpdateBounds, EnumButtonStyle.Small, "save")
                .AddTextInput(shopNameInputBounds, null, CairoFont.TextInput(), "shopName")
                .AddItemSlotGridExcl(Inventory, new Action<object>(this.SendInvPacket), (int)Math.Ceiling(Math.Sqrt(Inventory.Count)), new int[] { }, slotGrid, "currency");

            SingleComposer.GetTextInput("shopName").SetValue(name);
            SingleComposer.Compose();

        }

        private bool OnSavePressed()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(SingleComposer.GetTextInput("shopName").GetText());
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, VinConstants.SET_SHOP_NAME, data);
            return true;
        }


        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

        public override bool TryOpen()
        {
            this.Compose();
            return base.TryOpen();
        }


        public override void OnGuiClosed()
        {
            if (this.Inventory != null)
            {
                this.Inventory.Close(this.capi.World.Player);
                this.capi.World.Player.InventoryManager.CloseInventory(this.Inventory);
            }
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.CLOSE_GUI, null);
            this.capi.Gui.PlaySound(this.CloseSound, true, 1f);
        }

    }
}
