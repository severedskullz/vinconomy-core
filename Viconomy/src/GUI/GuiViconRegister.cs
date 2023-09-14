using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using static System.Formats.Asn1.AsnWriter;

namespace Viconomy.GUI
{
    public class GuiViconRegister : GuiDialogBlockEntity
    {
        BEViconStall stall;
        ViconomyInventory vinInv;
        ViconRegister[] registers;



        int stallSlotCount;
        private int stacksPerSlot;
        int curTab;
        DummyInventory inv;
        ViconPurchaseSlot purchaseSlot;
        StallSlot stallSlot;

        public GuiViconRegister(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {

            ViconomyModSystem modSystem = capi.ModLoader.GetModSystem<ViconomyModSystem>();
            //registers = modSystem.GetRegistry().GetRegistersForOwner(stall.Owner);           
            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.Compose();
        }

        private void Compose()
        {  
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
           
            ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.CenterBottom, 0, 20, (int)Math.Ceiling(Math.Sqrt(Inventory.Count)), (int)Math.Ceiling(Math.Sqrt(Inventory.Count)));

            bgBounds.WithChildren(slotGrid);

            //IconUtil.DrawArrowRight

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("myAwesomeDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                .AddItemSlotGridExcl(Inventory, new Action<object>(this.SendInvPacket), (int)Math.Ceiling(Math.Sqrt(Inventory.Count)), new int[] { }, slotGrid, "currency");

            SingleComposer.Compose();

        }

      

        private void onQuantityChanged(string amount)
        {
            int val = 1;
            Int32.TryParse(amount, out val);

            if (val > 0 && val <= 64 && val != stallSlot.itemsPerPurchase)
            {
                stallSlot.itemsPerPurchase = val;
                byte[] data;
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(curTab);
                    writer.Write(val);
                    data = ms.ToArray();
                }
                this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.SET_ITEMS_PER_PURCHASE, data);

                if (purchaseSlot.Itemstack != null)
                {
                    purchaseSlot.Itemstack.StackSize = val;
                }

            }
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
            if (this.Inventory != null)
            {
                this.Inventory.Close(this.capi.World.Player);
                this.capi.World.Player.InventoryManager.CloseInventory(this.Inventory);
            }
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.CLOSE_GUI, null);
            this.capi.Gui.PlaySound(this.CloseSound, true, 1f);
        }

        private void OnInventorySlotModified(int slotid)
        {
            // Direct call can cause InvalidOperationException
            capi.Event.EnqueueMainThreadTask(Compose, "setupvicondlg");
        }
    }
}
