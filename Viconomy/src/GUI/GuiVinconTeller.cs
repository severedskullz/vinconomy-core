using System;
using System.Collections.Generic;
using System.IO;
using Vinconomy.BlockEntities;
using Vinconomy.Registry;
using Vinconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vinconomy.GUI
{
    public class GuiVinconTeller : GuiDialogBlockEntity
    {

        InventoryBase vinInv;
        ShopRegistration[] registers;
        ICoreClientAPI api;
        BEVinconTeller teller;
        bool isOwner;
        int length = 5;
        bool[] reverseTrade;


        public GuiVinconTeller(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, bool isOwner)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            teller = capi.World.BlockAccessor.GetBlockEntity<BEVinconTeller>(BlockEntityPosition);
            this.isOwner = isOwner && !VinconomyCoreSystem.ShouldForceCustomerScreen;
            VinconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();
            ShopRegistration[] allRegisters = modSystem.GetRegistry().GetShopsForOwner(teller.Owner);
            List<ShopRegistration> filteredRegisters = new List<ShopRegistration>();
            foreach (ShopRegistration register in allRegisters)
            {
                if (register.Position != null)
                {
                    filteredRegisters.Add(register);
                }
            }

            registers = filteredRegisters.ToArray();
            
            if (!this.isOwner)
            {
                vinInv = new DummyInventory(capi, Inventory.Count);
                for (int i = 0; i < Inventory.Count; i++)
                {
                    vinInv[i] = new ItemSlot(vinInv);
                    vinInv[i].Itemstack = Inventory[i].Itemstack?.Clone();
                }

                vinInv.TakeLocked = true;
                vinInv.PutLocked = true;
            } else
            {
                vinInv = Inventory;
            }
                reverseTrade = new bool[length];
            
            if (base.IsDuplicate)
            {
                return;
            }
            this.DialogTitle = DialogTitle;

            this.Compose();
        }

        private void Compose()
        {
            if (isOwner)
                ComposeOwner();
            else
                ComposeCustomer();
        }

        public void ComposeOwner() {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            int selectedIndex = 0;
            int shopLength = registers.Length + 1;
            string[] shopsNames = new string[shopLength];
            string[] shopsKeys = new string[shopLength];

            shopsNames[0] = Lang.Get("vinconomy:gui-none");
            shopsKeys[0] = "None";
            for (int i = 0; i < registers.Length; i++)
            {
                shopsNames[i + 1] = registers[i].Name;
                shopsKeys[i + 1] = registers[i].ID.ToString();

                if (teller.ShopId == registers[i].ID)
                {
                    selectedIndex = i + 1;
                }
            }



            ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);

            ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
            ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(330);

            ElementBounds adminShopLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(0, 10);
            ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(shopSelectBounds).FixedRightOf(adminShopLabel).WithFixedOffset(0, 7);

            ElementBounds[,] currencyBounds = new ElementBounds[length, 3];
            for (int i = 0; i < length; i++)
            {
                ElementBounds prevBounds = adminShopBounds;
                if (i != 0)
                {
                    prevBounds = currencyBounds[i - 1, 0];
                }

                currencyBounds[i, 0] = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(prevBounds).WithFixedOffset(45, 10);
                currencyBounds[i, 1] = ElementBounds.FixedSize(100, 48).FixedUnder(prevBounds).FixedRightOf(currencyBounds[i, 0]).WithFixedOffset(30, 20);
                currencyBounds[i, 2] = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(prevBounds).FixedRightOf(currencyBounds[i, 1]).WithFixedOffset(30, 10);

                settingBounds.WithChildren(currencyBounds[i, 0], currencyBounds[i, 1], currencyBounds[i, 2]);
            }



            settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel, adminShopBounds, adminShopLabel);
            settingBounds.BothSizing = ElementSizing.FitToChildren;


            CairoFont labelFont = CairoFont.WhiteSmallText();
            CairoFont centeredFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
            CairoFont hoverText = CairoFont.WhiteDetailText();
            CairoFont mediumFont = CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center);

            bgBounds.WithChildren(settingBounds);

            try
            {
                SingleComposer = capi.Gui.CreateCompo("ViconTeller" + Inventory.InventoryID, dialogBounds);
                GuiComposer sc = SingleComposer;
                sc.AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                sc.BeginChildElements(settingBounds);


                sc.AddStaticText(Lang.Get("vinconomy:gui-shop"), labelFont, shopSelectionLabel)

                    .AddHoverText(Lang.Get("vinconomy:tooltip-shop"), hoverText, 500, shopSelectionLabel)
                    .AddDropDown(shopsKeys, shopsNames, selectedIndex, new SelectionChangedDelegate(this.onSelectionChanged), shopSelectBounds)
                    .AddIf(teller.IsAdminShop || VinUtils.IsCreativePlayer(api.World.Player))
                        .AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), labelFont, adminShopLabel)
                        .AddHoverText(Lang.Get("vinconomy:tooltip-admin-shop"), hoverText, 500, adminShopLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleAdminShop), adminShopBounds, "admin")
                    .EndIf();


                for (int i = 0; i < length; i++)
                {
                   
                    sc.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { i * 2 }, currencyBounds[i, 0]);
                    sc.AddStaticText("<=  =>", mediumFont, currencyBounds[i, 1]);
                    sc.AddHoverText(Lang.Get("vinconomy:tooltip-teller-conversion"), hoverText, 500, currencyBounds[i, 1]);
                    sc.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { (i * 2) + 1 }, currencyBounds[i, 2]); ;
                }


                sc.EndChildElements();

                if (isOwner && (teller.IsAdminShop || VinUtils.IsCreativePlayer(api.World.Player)))
                    sc.GetSwitch("admin").SetValue(teller.IsAdminShop);

                //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
                sc.Compose();
            }
            catch (Exception e)
            {
                this.capi.Logger.Debug(e.ToString());
            }
        
        }

        public void ComposeCustomer() {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;


            ElementBounds reverseLabelBounds = ElementBounds.FixedSize(350, 15).WithFixedOffset(0, GuiStyle.TitleBarHeight);

            ElementBounds[,] currencyBounds = new ElementBounds[length, 5];
            for (int i = 0; i < length; i++)
            {
                ElementBounds prevBounds = reverseLabelBounds;
                if (i != 0)
                {
                    prevBounds = currencyBounds[i - 1, 0];
                }
                currencyBounds[i, 0] = ElementBounds.FixedSize(60, 48).FixedUnder(prevBounds).WithFixedOffset(0, 10);
                currencyBounds[i, 1] = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedRightOf(currencyBounds[i, 0]).FixedUnder(prevBounds).WithFixedOffset(10, 10);
                currencyBounds[i, 2] = ElementBounds.FixedSize(48, 48).FixedUnder(prevBounds).FixedRightOf(currencyBounds[i, 1]).WithFixedOffset(30, 10);
                currencyBounds[i, 3] = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedRightOf(currencyBounds[i, 2]).FixedUnder(prevBounds).WithFixedOffset(30, 10);
                currencyBounds[i, 4] = ElementBounds.FixedSize(60, 48).FixedRightOf(currencyBounds[i, 3]).FixedUnder(prevBounds).WithFixedOffset(10, 10);

                bgBounds.WithChildren(currencyBounds[i, 0], currencyBounds[i, 1], currencyBounds[i, 2], currencyBounds[i, 3], currencyBounds[i, 4]);
            }



            bgBounds.WithChildren(reverseLabelBounds);

            CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);


            try
            {
                SingleComposer = capi.Gui.CreateCompo("ViconTeller" + Inventory.InventoryID, dialogBounds);
                GuiComposer sc = SingleComposer;
                sc.AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);



                sc.AddStaticText(Lang.Get("vinconomy:gui-reverse-trade"), labelTextFont, reverseLabelBounds);
                
                for (int i = 0; i < length; i++)
                {
                    sc.AddButton(Lang.Get("vinconomy:gui-deal"), MakeConsumableFor((i * 2)), currencyBounds[i, 0], EnumButtonStyle.Small, "currency" + i + "-1");
                    sc.GetButton("currency" + i + "-1").Enabled = reverseTrade[i];

                    sc.AddItemSlotGrid(vinInv, null, 1, new int[] { i * 2 }, currencyBounds[i, 1]);

                    sc.AddButton(reverseTrade[i] ? "<-" : "->", ReverseTradeFor(i), currencyBounds[i, 2], EnumButtonStyle.Small, "convertDirection" + i);

                    sc.AddItemSlotGrid(vinInv, null, 1, new int[] { (i * 2) + 1 }, currencyBounds[i, 3]); ;

                    sc.AddButton(Lang.Get("vinconomy:gui-deal"), MakeConsumableFor((i * 2) + 1), currencyBounds[i, 4], EnumButtonStyle.Small, "currency" + i + "-2");
                    sc.GetButton("currency" + i + "-2").Enabled = !reverseTrade[i];

                }
                

                sc.Compose();
            }
            catch (Exception e)
            {
                this.capi.Logger.Debug(e.ToString());
            }
        }

        private ActionConsumable MakeConsumableFor(int currencySlot)
        {
           return new ActionConsumable(() => {
               byte[] data;
               using (MemoryStream ms = new MemoryStream())
               {
                   BinaryWriter writer = new BinaryWriter(ms);
                   writer.Write(currencySlot);
                   data = ms.ToArray();
               }
               this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.CONVERT_CURRENCY, data);
               return true; 
           });
        }

        private ActionConsumable ReverseTradeFor(int currencySlot)
        {
            return new ActionConsumable(() => {
                reverseTrade[currencySlot] = !reverseTrade[currencySlot];
                Compose();
                return true;
            });
        }

        private void OnToggleAdminShop(bool isToggled)
        {
            VinUtils.SendSingleBool(capi, BlockEntityPosition, VinConstants.SET_ADMIN_SHOP, isToggled);
        }


        private void onSelectionChanged(string code, bool selected)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                if (code == "None")
                {
                    code = "-1";
                }

                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(Int32.Parse(code));
                data = ms.ToArray();
            }
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.SET_REGISTER_ID, data);

        }

        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
    }
}
