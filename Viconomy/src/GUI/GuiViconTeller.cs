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

namespace Viconomy.GUI
{
    public class GuiViconTeller : GuiDialogBlockEntity
    {

        InventoryBase vinInv;
        ShopRegistration[] registers;
        ICoreClientAPI api;
        BEViconTeller teller;
        bool isOwner;
        int length = 5;
        bool[] reverseTrade;


        public GuiViconTeller(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, bool isOwner)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            teller = capi.World.BlockAccessor.GetBlockEntity<BEViconTeller>(BlockEntityPosition);
            this.isOwner = isOwner;
            ViconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<ViconomyCoreSystem>();
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
            vinInv = Inventory;      
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

   
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

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

                if (teller.RegisterID == registers[i].ID)
                {
                    selectedIndex = i + 1;
                }
            }



            ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0,GuiStyle.TitleBarHeight);

            ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
            ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(330);

            ElementBounds adminShopLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds).WithFixedOffset(0, 10);
            ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(shopSelectBounds).FixedRightOf(adminShopLabel).WithFixedOffset(0, 7);
            ElementBounds reverseLabelBounds = ElementBounds.FixedSize(350, 15).FixedUnder(adminShopBounds).WithFixedOffset(0, isOwner ? 10 : -110);

            ElementBounds[,] currencyBounds = new ElementBounds[length,5];
            for (int i = 0; i < length; i++)
            {
                ElementBounds prevBounds = reverseLabelBounds;
                if (i != 0)
                {
                    prevBounds = currencyBounds[i-1, 0];
                }
                currencyBounds[i, 0] = ElementBounds.FixedSize(60, 48) 
                    .FixedUnder(prevBounds)
                    .WithFixedOffset(0, 10);
                currencyBounds[i, 1] = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1)
                    .FixedRightOf(currencyBounds[i, 0])
                    .FixedUnder(prevBounds)
                    .WithFixedOffset(10, 10);
                currencyBounds[i, 2] = ElementBounds.FixedSize(48, 48)
                    .FixedUnder(prevBounds)
                    .FixedRightOf(currencyBounds[i, 1])
                    .WithFixedOffset(30, 10);
                currencyBounds[i, 3] = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1)
                    .FixedRightOf(currencyBounds[i, 2])
                    .FixedUnder(prevBounds)
                    .WithFixedOffset(30, 10);
                currencyBounds[i, 4] = ElementBounds.FixedSize(60, 48)
                    .FixedRightOf(currencyBounds[i, 3])
                    .FixedUnder(prevBounds)
                    .WithFixedOffset(10, 10);

                settingBounds.WithChildren(currencyBounds[i, 0], currencyBounds[i, 1], currencyBounds[i, 2], currencyBounds[i, 3], currencyBounds[i, 4]);
            }
          


            settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel, adminShopBounds, adminShopLabel, reverseLabelBounds);
            settingBounds.BothSizing = ElementSizing.FitToChildren;

            CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);


            bgBounds.WithChildren(settingBounds);

            //IconUtil.DrawArrowRight
            try
            {
                SingleComposer = capi.Gui.CreateCompo("ViconTeller", dialogBounds);
                GuiComposer sc = SingleComposer;
                sc.AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                sc.BeginChildElements(settingBounds);

                if (isOwner)
                {
                    sc.AddStaticText("Shop:", CairoFont.WhiteSmallText(), shopSelectionLabel)
                     .AddDropDown(shopsKeys, shopsNames, selectedIndex, new SelectionChangedDelegate(this.onSelectionChanged), shopSelectBounds)
                     .AddIf(api.World.Player.HasPrivilege("gamemode"))
                         .AddStaticText("Admin Shop:", CairoFont.WhiteSmallText(), adminShopLabel)
                         .AddSwitch(new Action<bool>(this.OnToggleAdminShop), adminShopBounds, "admin")
                     .EndIf();
                }

                sc.AddStaticText("Reverse Trade?", labelTextFont, reverseLabelBounds);
                for (int i = 0; i < length; i++)
                {
                    sc.AddButton("Deal", MakeConsumableFor((i * 2)), currencyBounds[i, 0], EnumButtonStyle.Small, "currency" + i + "-1");
                    sc.GetButton("currency" + i + "-1").Enabled = reverseTrade[i];
                    if (isOwner) { 
                        sc.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { i * 2 }, currencyBounds[i, 1]);
                    } else {
                        sc.AddPassiveItemSlot(currencyBounds[i, 1], vinInv, vinInv[i * 2], true);
                    }
                    sc.AddButton(reverseTrade[i] ? "<-" : "->", ReverseTradeFor(i), currencyBounds[i, 2], EnumButtonStyle.Small, "convertDirection" + i);
                    if (isOwner)
                    {
                        sc.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { (i * 2) + 1 }, currencyBounds[i, 3]); ;
                    }
                    else
                    {
                        sc.AddPassiveItemSlot(currencyBounds[i, 3], vinInv, vinInv[(i * 2) + 1], true);
                    }
                    //sc.AddStaticText("Converts To", labelTextFont, currencyBounds[i, 2]);
                    sc.AddButton("Deal", MakeConsumableFor((i * 2)+1), currencyBounds[i, 4], EnumButtonStyle.Small, "currency" + i + "-2");
                    sc.GetButton("currency" + i + "-2").Enabled = !reverseTrade[i];

                }


                sc.EndChildElements();

                if (capi.World.Player.HasPrivilege("gamemode") && isOwner)
                    sc.GetSwitch("admin").SetValue(teller.isAdminShop);

                //.AddHorizontalTabs(tabs, tabBounds, new Action<int>(this.OnTabClicked), tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
                sc.Compose();
            } catch (Exception e)
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
               this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition.X, this.BlockEntityPosition.Y, this.BlockEntityPosition.Z, VinConstants.CURRENCY_CONVERSION, data);
               return true; 
           });
        }

        private ActionConsumable ReverseTradeFor(int currencySlot)
        {
            return new ActionConsumable(() => {
                reverseTrade[currencySlot] = !reverseTrade[currencySlot];
                //SingleComposer.GetButton("convertDirection" + currencySlot).Text = reverseTrade[currencySlot] ? "<-" : "->";
                Compose();
                return true;
            });
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
                if (code == "None")
                {
                    code = "-1";
                }

                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(Int32.Parse(code));
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
    }
}
