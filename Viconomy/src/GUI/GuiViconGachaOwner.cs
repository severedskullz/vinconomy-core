using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.GUI
{
    public class GuiViconGachaOwner : GuiDialogBlockEntity
    {
        BEVinconGacha stall;
        ViconomyGachaInventory vinInv;
        ShopRegistration[] registers;
        ICoreClientAPI api;

        bool useTotalRandomizer;

        public GuiViconGachaOwner(string DialogTitle, ViconomyGachaInventory Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconGacha>(BlockEntityPosition);
            ViconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<ViconomyCoreSystem>();
            ShopRegistration[] allRegisters = modSystem.GetRegistry().GetShopsForOwner(stall.Owner);
            List<ShopRegistration> filteredRegisters = new List<ShopRegistration>();
            foreach (ShopRegistration register in allRegisters)
            {
                if (register.Position != null)
                {
                    filteredRegisters.Add(register);
                }
            }

            registers = filteredRegisters.ToArray();
            vinInv = Inventory;
           
            
            
            if (base.IsDuplicate)
            {
                return;
            }
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            Inventory.SlotModified += Inventory_SlotModified;
            this.DialogTitle = DialogTitle;
            useTotalRandomizer = stall.useTotalRandomizer;
            this.Compose();
        }

        private void Inventory_SlotModified(int obj)
        {
            UpdateSlotWinningChances();
        }

        private void Compose()
        {

            try { 
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);


                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                int selectedIndex = 0;
                int shopLength = registers.Length + 1;
                string[] shopsNames = new string[shopLength];
                string[] shopsKeys = new string[shopLength];

                shopsNames[0] = "None";
                shopsKeys[0] = "None";
                for (int i = 0; i < registers.Length; i++)
                {
                    shopsNames[i+1] = registers[i].Name;
                    shopsKeys[i+1] = registers[i].ID.ToString(); 

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

                ElementBounds purchaseLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(125, 15);
                ElementBounds purchaseSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(purchaseLabel).WithFixedOffset(125, 00);

                ElementBounds absoluteLabel = ElementBounds.FixedSize(220, 25).FixedUnder(purchaseSlotBounds).WithFixedOffset(0, 15);
                ElementBounds absoluteBounds = ElementBounds.FixedSize(40, 40).FixedUnder(purchaseSlotBounds).FixedRightOf(absoluteLabel).WithFixedOffset(0, 10);

                ElementBounds adminShopLabel = ElementBounds.FixedSize(220, 25).FixedUnder(absoluteBounds).WithFixedOffset(0, 10);
                ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(absoluteBounds).FixedRightOf(adminShopLabel).WithFixedOffset(0, 5);

                //ElementBounds slotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 20, 1, 1);//.WithParent(itemPage);

              

                // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
                //ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

                ElementBounds itemPage = ElementBounds.FixedSize(200, 50).FixedRightOf(settingBounds).WithFixedOffset(15, GuiStyle.TitleBarHeight);
                itemPage.BothSizing = ElementSizing.FitToChildren;



                ElementBounds chanceLabel = ElementBounds.FixedSize(215, 25).WithFixedOffset(15,0);
                ElementBounds gachaBounds = ElementBounds.FixedSize(100, 300).FixedUnder(chanceLabel).WithFixedOffset(0,5);
                gachaBounds.BothSizing = ElementSizing.FitToChildren;
                int slotCount = vinInv.Count - 1;
                int slotsPerRow = 3;
                int rows = (int)Math.Ceiling(((double)slotCount) / slotsPerRow);
                //Console.WriteLine("We need " + rows + " rows...");

                //This is a lot more complicated than it needs to be. Basically, keep track of the last Row/Column element and put it either
                // Under the last row if its not null, or Right of the last column if its not null. At the end of the row loop, update our last
                // Row object to the first element in the logical "column". This way I can have an arbitrary layout. 
                ElementBounds[] slotBounds = new ElementBounds[slotCount];
                ElementBounds[] labelBounds = new ElementBounds[slotCount];
                ElementBounds curRow = null;
                for (int row = 0; row < rows; row++)
                {
                    ElementBounds curColumn = null;
                    for (int i = 0; i < slotsPerRow; i++)
                    {
                        int curIn = (row * slotsPerRow) + i;
                        //Console.WriteLine("Currently on index" + curIn);
                        if (curIn >= slotCount)
                        {
                            //TODO: Figure out a LESS fucking complicated way to get out of this loop and avoid IndexOutOfRangeException.
                            break;
                        }


                        ElementBounds curLabelBounds = ElementBounds.FixedSize(75, 20).WithParent(gachaBounds);
                        if (curRow != null)
                        {
                            curLabelBounds.FixedUnder(curRow).WithFixedOffset(0, 10);
                        }
                        ElementBounds curSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(curLabelBounds).WithParent(gachaBounds);//.WithFixedPadding(10)
                        if (curColumn != null)
                        {
                            curLabelBounds.FixedRightOf(curColumn).WithFixedOffset(10, 0);
                            curSlotBounds.FixedRightOf(curColumn).WithFixedOffset(20, 0);
                        } else
                        {
                            curSlotBounds.WithFixedOffset(10, 0);
                        }
                        curColumn = curLabelBounds;

                        slotBounds[curIn] = curSlotBounds;
                        labelBounds[curIn] = curLabelBounds;




                    }
                    int index = (row * slotsPerRow) + slotsPerRow;
                    if (index < slotCount)
                        curRow = slotBounds[index - 1];

                }

                

                itemPage.WithChildren(chanceLabel, gachaBounds);


                settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel, currencyLabel, currencySlotBounds, purchaseSlotBounds, adminShopBounds, adminShopLabel);
                settingBounds.BothSizing = ElementSizing.FitToChildren;
                bgBounds.WithChildren(settingBounds, itemPage);

                //IconUtil.DrawArrowRight

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconStallOwner", dialogBounds)
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
                        .AddStaticText("Use Total Count Randomizer:", CairoFont.WhiteSmallText(), absoluteLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleAbsolutePick), absoluteBounds, "absolutePick")
                        //.AddItemSlotGrid(inv, null, 1, new int[] { 0 }, purchaseSlotBounds, "purchase")
                    //.AddPassiveItemSlot(outputSlotBounds, Inventory, )
                    .EndChildElements();

                SingleComposer.BeginChildElements(gachaBounds)
                .AddStaticText("Chances of Winning:", CairoFont.WhiteSmallText(), chanceLabel);

                CairoFont font = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
               

                for (int i = 0; i < slotBounds.Length; i++)
                {
                    SingleComposer.AddDynamicText("0%", font, labelBounds[i], "percentage"+i);
                    SingleComposer.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 5, new int[] { i + 1}, slotBounds[i], "inventory" + i);
                }

                SingleComposer.EndChildElements();

                if (capi.World.Player.HasPrivilege("gamemode")) 
                   SingleComposer.GetSwitch("admin").SetValue(stall.IsAdminShop);

                SingleComposer.GetSwitch("absolutePick").SetValue(useTotalRandomizer);


                UpdateSlotWinningChances();
                //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
                SingleComposer.Compose();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private void UpdateSlotWinningChances()
        {
            int totalItems = vinInv.GetTotalItems();
            int totalSlots = vinInv.GetNonEmptySlotCount();

            for (int i = 0; i < vinInv.Slots.Length-1; i++)
            {
                ItemSlot slot = vinInv.Slots[i+1];
                double percent = 0;
                if (slot.StackSize > 0)
                {
                    if (useTotalRandomizer)
                    {
                        percent = Math.Round(((double)slot.StackSize / (double)totalItems) * 100, 2);
                    } else
                    {
                        percent = Math.Round(((double)1 / (double)totalSlots) * 100, 2);
                    }
                }
                string percentage = percent+"%";
                GuiElementDynamicText text = SingleComposer.GetDynamicText("percentage" + i);
                text.SetNewText(percentage);
            }
        }

        private void OnToggleAbsolutePick(bool isToggled)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            useTotalRandomizer = isToggled;
            UpdateSlotWinningChances();
            //Compose();
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.SET_TOTAL_RANDOMIZER, data);

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

        private void onSelectionChanged(string code, bool selected)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                int id = code.ToInt(-1);
                writer.Write(id);
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

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            vinInv.SlotModified -= Inventory_SlotModified;
        }

    }
}
