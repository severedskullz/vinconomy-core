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
    public class GuiViconTeller : GuiDialogBlockEntity
    {

        InventoryBase vinInv;
        ViconRegister[] registers;
        ICoreClientAPI api;
        BEViconTeller teller;
        bool isOwner;


        public GuiViconTeller(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi, bool isOwner)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            api = capi;
            teller = capi.World.BlockAccessor.GetBlockEntity<BEViconTeller>(BlockEntityPosition);
            this.isOwner = isOwner;
            ViconomyCore modSystem = capi.ModLoader.GetModSystem<ViconomyCore>();
            registers = modSystem.GetRegistry().GetRegistersForOwner(teller.Owner);
            vinInv = Inventory;           
            
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
                shopsKeys[i+1] = registers[i].ID; 

                if (teller.RegisterID == registers[i].ID)
                {
                    selectedIndex = i + 1;
                }
            }



            ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0,GuiStyle.TitleBarHeight);

            ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 75, 30);
            ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(330);
   
            int length = 5;
            ElementBounds[,] currencyBounds = new ElementBounds[length,5];
            for (int i = 0; i < length; i++)
            {
                ElementBounds prevBounds = shopSelectBounds.BelowCopy(0,0,330,5).WithFixedHeight(10);
                if (i != 0)
                {
                    prevBounds = currencyBounds[i-1, 0];
                }
                currencyBounds[i, 0] = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(prevBounds);
                currencyBounds[i, 1] = ElementBounds.FixedSize(48, 48).FixedUnder(prevBounds).FixedRightOf(currencyBounds[i, 0]).WithFixedOffset(5,0);
                currencyBounds[i, 2] = ElementBounds.FixedSize(120, 20).FixedUnder(prevBounds).FixedRightOf(currencyBounds[i, 1]).WithFixedOffset(0,14);
                currencyBounds[i, 3] = ElementBounds.FixedSize(48, 48).FixedUnder(prevBounds).FixedRightOf(currencyBounds[i, 2]);
                currencyBounds[i, 4] = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(prevBounds).FixedRightOf(currencyBounds[i, 3]).WithFixedOffset(5, 0);
                settingBounds.WithChildren(currencyBounds[i, 0], currencyBounds[i, 1], currencyBounds[i, 2], currencyBounds[i, 3], currencyBounds[i, 4]);
            }
          
            ElementBounds adminShopLabel = ElementBounds.FixedSize(100, 25).FixedUnder(currencyBounds[4,0]).WithFixedOffset(0, 10);
            ElementBounds adminShopBounds = ElementBounds.FixedSize(40, 40).FixedUnder(currencyBounds[4, 0]).FixedRightOf(adminShopLabel).WithFixedOffset(0, 7);

            settingBounds.WithChildren(shopSelectBounds, shopSelectionLabel, adminShopBounds, adminShopLabel);
            settingBounds.BothSizing = ElementSizing.FitToChildren;

            CairoFont labelTextFont = CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center);


            bgBounds.WithChildren(settingBounds);

            //IconUtil.DrawArrowRight
            try
            {
                SingleComposer = capi.Gui.CreateCompo("myAwesomeDialog", dialogBounds);
                GuiComposer sc = SingleComposer;
                sc.AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                sc.BeginChildElements(settingBounds)
                    .AddStaticText("Shop:", CairoFont.WhiteSmallText(), shopSelectionLabel)
                    .AddDropDown(shopsKeys, shopsNames, selectedIndex, new SelectionChangedDelegate(this.onSelectionChanged), shopSelectBounds)
                    .AddIf(api.World.Player.HasPrivilege("gamemode"))
                        .AddStaticText("Admin Shop:", CairoFont.WhiteSmallText(), adminShopLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleAdminShop), adminShopBounds, "admin")
                    .EndIf();

                for (int i = 0; i < length; i++)
                {
                    if (isOwner) { 
                        sc.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { i * 2 }, currencyBounds[i, 0], "currency" + i + "-1");
                    } else {
                        sc.AddPassiveItemSlot(currencyBounds[i, 0], vinInv, vinInv[i * 2], true);
                    }
                    sc.AddButton("<-", MakeConsumableFor(i * 2), currencyBounds[i, 1], EnumButtonStyle.Small, "currency" + i + "-to1");
                    sc.AddStaticText("Converts To", labelTextFont, currencyBounds[i, 2]);
                    sc.AddButton("->", MakeConsumableFor((i * 2) + 1), currencyBounds[i, 3], EnumButtonStyle.Small, "currency" + i + "-from1");
                    if (isOwner){
                        sc.AddItemSlotGrid(vinInv, new Action<object>(this.SendInvPacket), 1, new int[] { (i * 2) +1 }, currencyBounds[i, 4], "currency" + i +"-2"); ;
                    } else
                    {
                        sc.AddPassiveItemSlot(currencyBounds[i, 4], vinInv, vinInv[(i * 2) + 1], true);
                    }
                }


                sc.EndChildElements();

                if (capi.World.Player.HasPrivilege("gamemode"))
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
    }
}
