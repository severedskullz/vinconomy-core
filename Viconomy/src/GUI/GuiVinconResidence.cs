
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.BlockEntities.Unfinished;
using Viconomy.Registry;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.GUI
{
    public class GuiVinconResidence : GuiDialogBlockEntity
    {
        int selectedShopIndex;
        string[] shopsNames;
        string[] shopsKeys;

        public BEVinconResidence stall;
        private ShopRegistration[] registers;

        public GuiVinconResidence(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            stall = capi.World.BlockAccessor.GetBlockEntity<BEVinconResidence>(BlockEntityPosition);
            VinconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();
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
            int shopLength = registers.Length + 1;
            shopsNames = new string[shopLength];
            shopsKeys = new string[shopLength];

            selectedShopIndex = 0;
            shopsNames[0] = Lang.Get("vinconomy:gui-none");
            shopsKeys[0] = "None";
            for (int i = 0; i < registers.Length; i++)
            {
                shopsNames[i + 1] = registers[i].Name;
                shopsKeys[i + 1] = registers[i].ID.ToString();

                if (stall.RegisterID == registers[i].ID)
                {
                    selectedShopIndex = i + 1;
                }
            }

            Compose();
        }

        

        private void Compose()
        {
            try
            {

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;


                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                settingBounds.verticalSizing = ElementSizing.FitToChildren;

                ElementBounds shopSelectionLabel = ElementBounds.Fixed(0, 0, 250, 30);
                ElementBounds shopSelectBounds = shopSelectionLabel.BelowCopy().WithFixedWidth(250);

                ElementBounds currencyLabel = ElementBounds.FixedSize(100, 25).FixedUnder(shopSelectBounds, 15);
                ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel);

                ElementBounds costSelectionLabel = ElementBounds.FixedSize(175, 25).FixedUnder(currencySlotBounds,15);
                ElementBounds costSelectionBounds = ElementBounds.FixedSize(75, 30).FixedUnder(currencySlotBounds,10).FixedRightOf(costSelectionLabel);

                ElementBounds monthLimitLabel = ElementBounds.FixedSize(175, 25).FixedUnder(costSelectionBounds,15);
                ElementBounds monthLimitBounds = ElementBounds.FixedSize(75, 30).FixedUnder(costSelectionBounds,10).FixedRightOf(monthLimitLabel);

                ElementBounds chiselLabel = ElementBounds.FixedSize(200, 25).FixedUnder(monthLimitLabel,15);
                ElementBounds chiselSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(chiselLabel);

                ElementBounds adminShopLabel = ElementBounds.FixedSize(220, 25).FixedUnder(chiselSlotBounds,15);
                ElementBounds adminShopBounds = ElementBounds.FixedSize(30, 30).FixedUnder(chiselSlotBounds,10).FixedRightOf(adminShopLabel);


                ElementBounds zoneBounds = ElementBounds.FixedSize(250, 200).FixedRightOf(settingBounds).WithFixedOffset(10, GuiStyle.TitleBarHeight);
                zoneBounds.verticalSizing = ElementSizing.FitToChildren;

                ElementBounds zoneOriginLabel = ElementBounds.FixedSize(250, 25);
                ElementBounds zoneXLabel = ElementBounds.FixedSize(20, 25).FixedUnder(zoneOriginLabel);
                ElementBounds zoneX = ElementBounds.FixedSize(60, 25).FixedUnder(zoneOriginLabel).FixedRightOf(zoneXLabel);
                ElementBounds zoneYLabel = ElementBounds.FixedSize(20, 25).FixedUnder(zoneOriginLabel).FixedRightOf(zoneX, 5);
                ElementBounds zoneY = ElementBounds.FixedSize(60, 25).FixedUnder(zoneOriginLabel).FixedRightOf(zoneYLabel);
                ElementBounds zoneZLabel = ElementBounds.FixedSize(20, 25).FixedUnder(zoneOriginLabel).FixedRightOf(zoneY, 5);
                ElementBounds zoneZ = ElementBounds.FixedSize(60, 25).FixedUnder(zoneOriginLabel).FixedRightOf(zoneZLabel);

                ElementBounds zoneSizeLabel = ElementBounds.FixedSize(250, 25).FixedUnder(zoneXLabel, 15);
                ElementBounds zoneSXLabel = ElementBounds.FixedSize(20, 25).FixedUnder(zoneSizeLabel);
                ElementBounds zoneSX = ElementBounds.FixedSize(60, 25).FixedUnder(zoneSizeLabel).FixedRightOf(zoneSXLabel);
                ElementBounds zoneSYLabel = ElementBounds.FixedSize(20, 25).FixedUnder(zoneSizeLabel).FixedRightOf(zoneSX,5);
                ElementBounds zoneSY = ElementBounds.FixedSize(60, 25).FixedUnder(zoneSizeLabel).FixedRightOf(zoneSYLabel);
                ElementBounds zoneSZLabel = ElementBounds.FixedSize(20, 25).FixedUnder(zoneSizeLabel).FixedRightOf(zoneSY, 5);
                ElementBounds zoneSZ = ElementBounds.FixedSize(60, 25).FixedUnder(zoneSizeLabel).FixedRightOf(zoneSZLabel);

                ElementBounds allowEditingLabel = ElementBounds.FixedSize(220, 25).FixedUnder(zoneSXLabel, 20);
                ElementBounds allowEditing = ElementBounds.FixedSize(30, 30).FixedUnder(zoneSXLabel, 15).FixedRightOf(allowEditingLabel);


                ElementBounds saveBounds = ElementBounds.FixedSize(250, 30).FixedUnder(allowEditingLabel,15);


                ElementBounds itemBounds = ElementBounds.FixedSize(255, 200).FixedRightOf(zoneBounds).WithFixedOffset(10, GuiStyle.TitleBarHeight);
                itemBounds.verticalSizing = ElementSizing.FitToChildren;

                ElementBounds tennantLabel = ElementBounds.FixedSize(250, 25).WithFixedOffset(5, 0);
                ElementBounds tennantName = ElementBounds.FixedSize(250, 25).FixedUnder(tennantLabel).WithFixedOffset(5, 0);

                ElementBounds depositLabel = ElementBounds.FixedSize(250, 25).FixedUnder(tennantName,15).WithFixedOffset(5, 0);
                ElementBounds depositSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 5, 2).FixedUnder(depositLabel);

                ElementBounds paymentLabel = ElementBounds.FixedSize(250, 25).FixedUnder(depositSlots, 15).WithFixedOffset(5, 0);
                ElementBounds paymentSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 5, 2).FixedUnder(paymentLabel);

                ElementBounds evictBounds = ElementBounds.FixedSize(255, 30).FixedUnder(paymentSlots);


                bgBounds.WithChildren(settingBounds, zoneBounds, itemBounds);
                SingleComposer = capi.Gui.CreateCompo("ViconStallOwner", dialogBounds)
                                   .AddShadedDialogBG(bgBounds)
                                   .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                .AddInset(settingBounds, 1)
                .AddInset(zoneBounds,2)
                .AddInset(itemBounds,3);

                SingleComposer.BeginChildElements(settingBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-shop"), CairoFont.WhiteSmallText(), shopSelectionLabel)
                    .AddDropDown(shopsKeys, shopsNames, selectedShopIndex, new SelectionChangedDelegate(this.onSelectionChanged), shopSelectBounds, "shopSelection")
                                        
                    .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 1, new int[] { 1 }, currencySlotBounds, "currency")

                    .AddStaticText(Lang.Get("vinconomy:gui-cost"), CairoFont.WhiteSmallText(), costSelectionLabel)
                    .AddNumberInput(costSelectionBounds, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "costQuantity")

                    .AddStaticText(Lang.Get("vinconomy:gui-month-limit"), CairoFont.WhiteSmallText(), monthLimitLabel)
                    .AddNumberInput(monthLimitBounds, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "monthLimit")




                    .AddStaticText(Lang.Get("vinconomy:gui-decoration-block"), CairoFont.WhiteSmallText(), chiselLabel)
                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 1, new int[] { 0 }, chiselSlotBounds, "chisel")

                    .AddIf(capi.World.Player.HasPrivilege("gamemode"))
                        .AddStaticText(Lang.Get("vinconomy:gui-admin-shop"), CairoFont.WhiteSmallText(), adminShopLabel)
                        .AddSwitch(new Action<bool>(this.OnToggleAdminShop), adminShopBounds, "admin")
                    .EndIf()
                .EndChildElements();

                CairoFont labelText = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);
                SingleComposer.BeginChildElements(zoneBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-zone-origin"), CairoFont.WhiteSmallText(), zoneOriginLabel)
                    .AddStaticText("X", labelText, zoneXLabel)
                    .AddNumberInput(zoneX, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "zoneX")
                    .AddStaticText("Y", labelText, zoneYLabel)
                    .AddNumberInput(zoneY, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "zoneY")
                    .AddStaticText("Z", labelText, zoneZLabel)
                    .AddNumberInput(zoneZ, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "zoneZ")

                    .AddStaticText(Lang.Get("vinconomy:gui-zone-size"), CairoFont.WhiteSmallText(), zoneSizeLabel)
                    .AddStaticText("X", labelText, zoneSXLabel)
                    .AddNumberInput(zoneSX, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "zoneSX")
                    .AddStaticText("Y", labelText, zoneSYLabel)
                    .AddNumberInput(zoneSY, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "zoneSY")
                    .AddStaticText("Z", labelText, zoneSZLabel)
                    .AddNumberInput(zoneSZ, new Action<string>(this.onCostQuantityChanged), CairoFont.WhiteSmallText(), "zoneSZ")

                    .AddStaticText(Lang.Get("vinconomy:gui-allow-editing"), CairoFont.WhiteSmallText(), allowEditingLabel)
                    .AddSwitch(new Action<bool>(this.OnToggleAdminShop), allowEditing, "allowEditing")

                    .AddButton("Save", new ActionConsumable(this.EvictPlayer), saveBounds, EnumButtonStyle.Small)

                .EndChildElements();

                int[] uiSlots = {2,3,4,5,6,7,8,9,10,11};
                SingleComposer.BeginChildElements(itemBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-tennant"), CairoFont.WhiteSmallText(), tennantLabel)
                    .AddStaticText("SeveredSkulz", CairoFont.WhiteSmallText(), tennantName)
                    .AddStaticText(Lang.Get("vinconomy:gui-deposit"), CairoFont.WhiteSmallText(), depositLabel)
                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 5, uiSlots, depositSlots, "deposit")
                    .AddStaticText(Lang.Get("vinconomy:gui-payment"), CairoFont.WhiteSmallText(), paymentLabel)
                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 5, uiSlots, paymentSlots, "payment")
                    .AddButton("Evict", new ActionConsumable(this.EvictPlayer), evictBounds, EnumButtonStyle.Small)

                .EndChildElements();

                SingleComposer.Compose();
            }
            catch (Exception e)
            {
                this.capi.Logger.Debug(e.ToString());
            }
        }

        private bool EvictPlayer()
        {
            throw new NotImplementedException();
        }

        private void onCostQuantityChanged(string obj)
        {
            throw new NotImplementedException();
        }

        private void onSelectionChanged(string code, bool selected)
        {
            throw new NotImplementedException();
        }

        private void OnToggleAdminShop(bool obj)
        {
            throw new NotImplementedException();
        }

        private void SendInvPacket(object obj)
        {
            throw new NotImplementedException();
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
    }
}
