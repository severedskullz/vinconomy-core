using System;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiDialogViconStallOwner : GuiDialogBlockEntity
    {
        BEViconStall stall;
        ViconomyInventory vinInv;
        ViconRegister[] registers;
        ICoreClientAPI api;


        int stallSlotCount;
        private int stacksPerSlot;
        int curTab;
        DummyInventory inv;
        ViconPurchaseSlot purchaseSlot;
        StallSlot stallSlot;

        public GuiDialogViconStallOwner(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            stall = capi.World.BlockAccessor.GetBlockEntity<BEViconStall>(BlockEntityPosition);
            curTab = stallSelection;
            ViconomyModSystem modSystem = capi.ModLoader.GetModSystem<ViconomyModSystem>();
            registers = modSystem.GetRegistry().GetRegistersForOwner(stall.Owner);
            vinInv = Inventory as ViconomyInventory;
            this.stallSlotCount = stall.StallSlotCount;
            this.stacksPerSlot = stall.StacksPerSlot;
            
            
            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;

            this.inv = new DummyInventory(capi);
            purchaseSlot = new ViconPurchaseSlot(inv, 0);
            purchaseSlot.OnActivateLeftClick += DoPurchase;
            this.inv[0] = purchaseSlot;

            this.Compose();
        }

        public void DoPurchase()
        {
            Console.WriteLine("Attempted to purchase item.");
        }


        private void Compose()
        {

   
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);


            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;





            ViconomyInventory vinInv = Inventory as ViconomyInventory;
            int[] uiSlots = new int[stacksPerSlot];
            int offset = curTab * (stacksPerSlot + 1);
            if (vinInv != null)
            {
                for (int i = 0; i < stacksPerSlot; i++)
                {
                    uiSlots[i] = offset + i;
                }
            }

            int selectedIndex = 0;
            int shopLength = registers.Length + 1;
            string[] shopsNames = new string[shopLength];
            string[] shopsKeys = new string[shopLength];

            shopsNames[0] = "None";
            shopsKeys[0] = "None";
            for (int i = 0; i < registers.Length; i++)
            {
                shopsNames[i+1] = registers[i].Name;
                shopsKeys[i+1] = registers[i].ID; 

                if (stall.RegisterID == registers[i].ID)
                {
                    selectedIndex = i + 1;
                }
            }


            stallSlot = vinInv.StallSlots[curTab];
            ItemSlot item = stallSlot.FindFirstNonEmptyStockSlot();
            
            
            if (item != null)
            {
                this.purchaseSlot.Itemstack = item.Itemstack.Clone();
                this.purchaseSlot.Itemstack.StackSize = Math.Min(Math.Max(1, stallSlot.itemsPerPurchase), item.MaxSlotStackSize);
            } else
            {
                this.purchaseSlot.Itemstack = null;
            }


            ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0,GuiStyle.TitleBarHeight);
            
            //settingBounds.BothSizing = ElementSizing.FitToChildren;

            // Auto-sized dialog at the center of the screen
            //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
            ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);      
           
            ElementBounds currencyLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(0, 15);
            ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel);

            ElementBounds purchaseLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(125, 15);
            ElementBounds purchaseSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1,1).FixedUnder(purchaseLabel).WithFixedOffset(125, 00);

            ElementBounds quantitySelectionLabel = ElementBounds.FixedSize(150, 30).FixedUnder(currencySlotBounds).WithFixedOffset(0, 15);
            ElementBounds quantitySelectionBounds = ElementBounds.FixedSize(75, 30).FixedUnder(currencySlotBounds).FixedRightOf(quantitySelectionLabel).WithFixedOffset(25, 10);


            ElementBounds adminShopLabel = ElementBounds.FixedSize(100, 25).FixedUnder(quantitySelectionBounds).WithFixedOffset(0, 15);
            ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(quantitySelectionBounds).FixedRightOf(adminShopLabel).WithFixedOffset(120, 10);

            settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel, quantitySelectionBounds, quantitySelectionLabel, currencyLabel, currencySlotBounds, purchaseSlotBounds, adminShopBounds, adminShopLabel);
            settingBounds.verticalSizing = ElementSizing.FitToChildren;

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

            ElementBounds itemPage = ElementBounds.FixedSize(200, 10).FixedRightOf(settingBounds).WithFixedOffset(10, GuiStyle.TitleBarHeight);
            itemPage.BothSizing = ElementSizing.FitToChildren;


            ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithFixedPosition(0, 0);
            //ElementBounds pageLabel = ElementBounds.FixedSize(50, 20).WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0,15).WithFixedPadding(75,0);
            ElementBounds pageLabel = ElementBounds.FixedSize(50, 25).WithFixedAlignmentOffset(0, 10).FixedRightOf(pagePrev, 10);
            //ElementBounds pageNext = ElementBounds.FixedSize(50, 50).WithAlignment(EnumDialogArea.RightTop);
            CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
            string labelText = "Slot " + (curTab + 1) + " of " + stallSlotCount;
            labelTextFont.AutoBoxSize(labelText, pageLabel, true);

            ElementBounds pageNext = ElementBounds.FixedSize(30, 30).FixedRightOf(pageLabel, 10);
            ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.CenterBottom, 0, 20, (int)Math.Ceiling(Math.Sqrt(uiSlots.Length)), (int)Math.Ceiling(Math.Sqrt(uiSlots.Length)));//.WithParent(itemPage);


            itemPage.WithChildren(pagePrev, pageLabel, pageNext, slotGrid);



            bgBounds.WithChildren(itemPage, settingBounds);

            //IconUtil.DrawArrowRight

            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("myAwesomeDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                SingleComposer.BeginChildElements(settingBounds)
                    .AddStaticText("Shop:", CairoFont.WhiteSmallText(), shopSelectionLabel)
                    .AddDropDown(shopsKeys, shopsNames, selectedIndex, new SelectionChangedDelegate(this.onSelectionChanged), shopSelectBounds)
                    .AddIf(api.World.Player.HasPrivilege("gamemode"))
                        .AddStaticText("Admin Shop:", CairoFont.WhiteSmallText(), adminShopLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleAdminShop), adminShopBounds, "admin")
                    .EndIf()
                    .AddStaticText("Items Per Purchase:", CairoFont.WhiteSmallText(), quantitySelectionLabel)
                    .AddNumberInput(quantitySelectionBounds, new Action<string>(this.onQuantityChanged), CairoFont.WhiteSmallText(), "quantity")
                    //.AddButton("Save", new ActionConsumable(this.onSave),saveButtonBounds, EnumButtonStyle.Small, "save")
                    .AddStaticText("Price:", CairoFont.WhiteSmallText(), currencyLabel)
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { offset + stacksPerSlot }, currencySlotBounds, "currency")
                    .AddStaticText("Product:", CairoFont.WhiteSmallText(), purchaseLabel)
                    .AddPassiveItemSlot(purchaseSlotBounds, inv, purchaseSlot, false)
                    //.AddItemSlotGrid(inv, null, 1, new int[] { 0 }, purchaseSlotBounds, "purchase")
                //.AddPassiveItemSlot(outputSlotBounds, Inventory, )
                .EndChildElements();

                SingleComposer.BeginChildElements(itemPage)
                    .AddButton("<", new ActionConsumable(this.PreviousPage), pagePrev, EnumButtonStyle.Small, "prevPage")
                    .AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel")
                    .AddButton(">", new ActionConsumable(this.NextPage), pageNext, EnumButtonStyle.Small, "nextPage")
                    .AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), (int)Math.Ceiling(Math.Sqrt(uiSlots.Length)), uiSlots, slotGrid, "inventory")
                .EndChildElements();


            if (curTab == 0)
                SingleComposer.GetButton("prevPage").Enabled = false;

            if (curTab == stallSlotCount-1)
                SingleComposer.GetButton("nextPage").Enabled = false;

            if (capi.World.Player.HasPrivilege("gamemode")) 
                SingleComposer.GetSwitch("admin").SetValue(stall.isAdminShip);

            SingleComposer.GetTextInput("quantity").SetValue(stallSlot.itemsPerPurchase);


            //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
            SingleComposer.Compose();

            

        }

        private void OnToggleAdminShop(bool isToggled)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.SET_ADMIN_SHOP, data);

        }

        private bool PreviousPage()
        {
            curTab -= 1;
            curTab = Math.Max(0, curTab);
            this.Compose();
            return true;
        }

        private bool NextPage()
        {
            curTab += 1;
            curTab = Math.Min(stallSlotCount-1, curTab);
            this.Compose();
            return true;
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

        private void onSelectionChanged(string code, bool selected)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(code);
                data = ms.ToArray();
            }
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.SET_SHOP_ID, data);

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
        

        public override void OnGuiClosed()
        {
            //This is identical to base, except the fact that Im using VinConstants for the packet ID
            if (this.Inventory != null)
            {
                this.Inventory.Close(this.capi.World.Player);
                this.capi.World.Player.InventoryManager.CloseInventory(this.Inventory);
            }
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.CLOSE_GUI, null);
            this.capi.Gui.PlaySound(this.CloseSound, true, 1f);

        }
        */
        private void OnInventorySlotModified(int slotid)
        {
            // Direct call can cause InvalidOperationException
            capi.Event.EnqueueMainThreadTask(Compose, "setupvicondlg");
        }
    }
}
