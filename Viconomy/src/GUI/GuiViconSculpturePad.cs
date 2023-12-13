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
    public class GuiViconSculpturePadOwner : GuiDialogBlockEntity
    {
        BEViconSculpturePad stall;
        InventoryGeneric vinInv;
        ViconRegister[] registers;
        ICoreClientAPI api;


        int stallSlotCount;
        int curTab;
        DummyInventory inv;

        public GuiViconSculpturePadOwner(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, int stallSelection)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            stall = capi.World.BlockAccessor.GetBlockEntity<BEViconSculpturePad>(BlockEntityPosition);
            curTab = stallSelection;
            ViconomyCore modSystem = capi.ModLoader.GetModSystem<ViconomyCore>();
            registers = modSystem.GetRegistry().GetRegistersForOwner(stall.Owner);
            vinInv = Inventory as InventoryGeneric;
            this.stallSlotCount = stall.GetSizeY();
            
            
            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            this.DialogTitle = DialogTitle;

            vinInv.SlotModified += VinInv_SlotModified;

            this.Compose();
        }

        private void VinInv_SlotModified(int slot)
        {
            //throw new NotImplementedException();
        }

        private void Compose()
        {

            try { 
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);


                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                InventoryGeneric vinInv = Inventory as InventoryGeneric;
                int itemsPerLayer = stall.GetSizeXZ() * stall.GetSizeXZ();
                int[] uiSlots = new int[itemsPerLayer];
                int offset = (curTab * itemsPerLayer) + 1;
                if (vinInv != null)
                {
                    for (int i = 0; i < itemsPerLayer; i++)
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

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0,GuiStyle.TitleBarHeight);
            
                //settingBounds.BothSizing = ElementSizing.FitToChildren;

                // Auto-sized dialog at the center of the screen
                //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
                ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
                ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);      
           
                ElementBounds currencyLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(0, 15);
                ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel);

          
                ElementBounds adminShopLabel = ElementBounds.FixedSize(100, 25).FixedUnder(currencySlotBounds).WithFixedOffset(0, 15);
                ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(currencySlotBounds).FixedRightOf(adminShopLabel).WithFixedOffset(120, 10);

                settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel, currencyLabel, currencySlotBounds, adminShopBounds, adminShopLabel);
                settingBounds.verticalSizing = ElementSizing.FitToChildren;

                // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
                //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

                ElementBounds itemPage = ElementBounds.FixedSize(200, 200).FixedRightOf(settingBounds).WithFixedOffset(10, GuiStyle.TitleBarHeight);
                itemPage.BothSizing = ElementSizing.FitToChildren;


                ElementBounds pagePrev = ElementBounds.FixedSize(30, 30).WithFixedPosition(0, 0);
                //ElementBounds pageLabel = ElementBounds.FixedSize(50, 20).WithAlignment(EnumDialogArea.CenterTop).WithFixedAlignmentOffset(0,15).WithFixedPadding(75,0);
                ElementBounds pageLabel = ElementBounds.FixedSize(50, 25).WithFixedAlignmentOffset(0, 10).FixedRightOf(pagePrev, 10);
                //ElementBounds pageNext = ElementBounds.FixedSize(50, 50).WithAlignment(EnumDialogArea.RightTop);
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                string labelText = "Layer " + (curTab + 1) + " of " + stallSlotCount;
                labelTextFont.AutoBoxSize(labelText, pageLabel, true);

                ElementBounds pageNext = ElementBounds.FixedSize(30, 30).FixedRightOf(pageLabel, 10);
                ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.CenterBottom, 0, 20, stall.GetSizeXZ(), (int)Math.Ceiling(Math.Sqrt(uiSlots.Length)));//.WithParent(itemPage);


                itemPage.WithChildren(pagePrev, pageLabel, pageNext, slotGrid);

                ElementBounds disabledSlotsPage = ElementBounds.FixedSize(200, 10).FixedUnder(itemPage).FixedRightOf(settingBounds).WithFixedOffset(10,0);

                ElementBounds disabledSlotsLabel = ElementBounds.FixedSize(125, 25).WithParent(disabledSlotsPage);

                ElementBounds[][] bounds = new ElementBounds[stall.GetSizeXZ()][];
                for (int y = 0; y < stall.GetSizeXZ(); y++)
                {
                    bounds[y] = new ElementBounds[stall.GetSizeXZ()];

                    for (int x = 0; x < stall.GetSizeXZ(); x++)
                    {
                        ElementBounds disabledBounds = ElementBounds.FixedSize(40, 40).WithFixedOffset(10 + x * 50, 35 +(y * 50)).WithParent(disabledSlotsPage);
                        bounds[y][x] = disabledBounds;

                    }
                }
                
                disabledSlotsPage.BothSizing = ElementSizing.FitToChildren;



                bgBounds.WithChildren(itemPage, settingBounds, disabledSlotsPage);

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
                   
                        //.AddButton("Save", new ActionConsumable(this.onSave),saveButtonBounds, EnumButtonStyle.Small, "save")
                        .AddStaticText("Price:", CairoFont.WhiteSmallText(), currencyLabel)
                        .AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { 0 }, currencySlotBounds, "currency")
                   
                        //.AddItemSlotGrid(inv, null, 1, new int[] { 0 }, purchaseSlotBounds, "purchase")
                    //.AddPassiveItemSlot(outputSlotBounds, Inventory, )
                    .EndChildElements();

                    SingleComposer.BeginChildElements(itemPage)
                        .AddButton("<", new ActionConsumable(this.PreviousLayer), pagePrev, EnumButtonStyle.Small, "prevPage")
                        .AddDynamicText(labelText, labelTextFont, pageLabel, "pageLabel")
                        .AddButton(">", new ActionConsumable(this.NextLayer), pageNext, EnumButtonStyle.Small, "nextPage")
                        .AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), stall.GetSizeXZ(), uiSlots, slotGrid, "inventory")
                    .EndChildElements();

                SingleComposer.BeginChildElements(disabledSlotsPage)
                .AddStaticText("Disabled Slots:", CairoFont.WhiteSmallText(), disabledSlotsLabel);


                for (int y = 0; y < stall.GetSizeXZ(); y++)
                {
                    for (int x = 0; x < stall.GetSizeXZ(); x++)
                    {
                        SingleComposer.AddSwitch(new Action<bool>(this.OnToggleAdminShop), bounds[y][x], "disabled-" + x + "-" + y);

                    }
                }
                SingleComposer.EndChildElements();


                if (curTab == 0)
                    SingleComposer.GetButton("prevPage").Enabled = false;

                if (curTab == stallSlotCount-1)
                    SingleComposer.GetButton("nextPage").Enabled = false;

                if (capi.World.Player.HasPrivilege("gamemode")) 
                    SingleComposer.GetSwitch("admin").SetValue(stall.isAdminShop);


                //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
                SingleComposer.Compose();

            }
            catch (Exception e)
            {
                this.capi.Logger.Debug(e.ToString());
            }

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

        private bool PreviousLayer()
        {
            curTab -= 1;
            curTab = Math.Max(0, curTab);
            this.Compose();
            return true;
        }

        private bool NextLayer()
        {
            curTab += 1;
            curTab = Math.Min(stall.GetSizeY()-1, curTab);
            this.Compose();
            return true;
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
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.SET_REGISTER_ID, data);

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
    }
}
