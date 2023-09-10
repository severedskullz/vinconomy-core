using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.Inventory;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.GUI
{
    public class GuiDialogBlockEntityViconContainer : GuiDialogBlockEntity
    {
        int slots;
        int curTab;
        public GuiDialogBlockEntityViconContainer(string DialogTitle, int slots, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            this.slots = slots;
            
            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;
            this.Compose();
        }

        private void OnTabClicked(int tabId, GuiTab tab)
        {
            this.curTab = tabId;

            this.SingleComposer.GetVerticalTab("slotTabs").SetValue(this.curTab);
            this.Compose();
        }


        private void Compose()
        {

            GuiTab[] tabs = new GuiTab[slots];
            for (int i = 0; i < slots; i++)
            {
                tabs[i] = new GuiTab
                {
                    Name = "Bin #" + i,
                    DataInt = 0
                };
            }

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);


            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;



            ElementBounds settingBounds = ElementBounds.FixedSize(200, 200);
            ElementBounds settingInsetBounds = settingBounds.FlatCopy().WithFixedPadding(10);
            //settingBounds.BothSizing = ElementSizing.FitToChildren;

            ViconomyInventory inv = Inventory as ViconomyInventory;
            int[] uiSlots = new int[inv.Count];
            if (inv != null)
            {
                int offset = curTab * (slots + 1);
                for (int i = 0; i < uiSlots.Length; i++)
                {
                    uiSlots[i] = i;
                }
            }

            string[] shops = new string[10];
            for (int i = 0; i < shops.Length; i++)
            {
                shops[i] = "Shop " + i;
            }

            ElementBounds itemPage = ElementBounds.FixedSize(100, 10).WithFixedOffset(0, GuiStyle.TitleBarHeight) ;
            itemPage.BothSizing = ElementSizing.FitToChildren;

           
            ElementBounds pagePrev = ElementBounds.FixedSize(50, 50);
            ElementBounds pageLabel = ElementBounds.FixedSize(225, 20).WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0,15);
            ElementBounds pageNext = ElementBounds.FixedSize(50, 50).WithAlignment(EnumDialogArea.RightTop);
            ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.CenterBottom, 0, 30, (int)Math.Sqrt(uiSlots.Length), (int)Math.Sqrt(uiSlots.Length));//.WithParent(itemPage);

            CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
            string labelText = "Slot " + 10 + " of " + 10;
            labelTextFont.AutoBoxSize(labelText,pageLabel, true);
            itemPage.WithChildren(pagePrev,pageLabel,pageNext,slotGrid);


            // Auto-sized dialog at the center of the screen
            //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 25).WithParent(settingInsetBounds);
            ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);//ElementBounds.Fixed(0, 0, 100, 25).FixedRightOf(shopSelectionLabel,10);          
            ElementBounds quantitySelectionLabel = shopSelectBounds.BelowCopy(); //ElementBounds.Fixed(10, 0,100,25).FixedUnder(shopSelectionLabel,10)
            ElementBounds quantitySelectionBounds = quantitySelectionLabel.BelowCopy(); // ElementBounds.Fixed(0, 0, 100, 25).FixedRightOf(quantitySelectionLabel,10).FixedUnder(shopSelectionLabel,10);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

            bgBounds.WithChildren(itemPage, settingBounds);



            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("myAwesomeDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                .BeginChildElements(itemPage)
                    .AddButton("<-", new ActionConsumable(this.PreviousPage), pagePrev, EnumButtonStyle.Small)
                    .AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel")
                    .AddButton("->", new ActionConsumable(this.PreviousPage), pageNext, EnumButtonStyle.Small)
                    .AddItemSlotGrid(inv, new Action<object>(this.SendInvPacket), (int)Math.Sqrt(uiSlots.Length), uiSlots, slotGrid, "inventory")
                .EndChildElements()
                /*
                .BeginChildElements(settingInsetBounds)
                    .AddStaticText("Shop:", CairoFont.TextInput(), shopSelectionLabel)
                    .AddDropDown(shops, shops, 0, new SelectionChangedDelegate(this.onSelectionChanged) , shopSelectBounds)

                    .AddStaticText("Quantity Items Sold Per Purchase:", CairoFont.WhiteSmallText(), quantitySelectionLabel)
                    .AddNumberInput(quantitySelectionBounds, new Action<string>(this.onQuantityChanged), CairoFont.WhiteSmallText(), "quantity")
                .EndChildElements()
                
                .BeginChildElements(bgBounds)
                    
                    .AddInset(settingBounds)
                .EndChildElements()
                */

                //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
                .Compose();



        }

        private bool PreviousPage()
        {
            return true;
        }

        private void onQuantityChanged(string obj)
        {
            
        }

        private void onSelectionChanged(string code, bool selected)
        {

        }

        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

        /*
        public override void OnGuiClosed()
        {
            

            SingleComposer.GetSlotGrid("inputSlot").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(capi);

            base.OnGuiClosed();
        }
        */

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
