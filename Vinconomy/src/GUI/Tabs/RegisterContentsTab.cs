using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Commercially.Vinconomy.GUI.Tabs
{
    public class RegisterContentsTab : ModularTab
    {
        public const string CODE = "Vinconomy.RegisterContents";
        public override string Code => CODE;
        public override string TabName => Lang.Get("commercially:tabname-container");

        RegisterInventory Inventory;
        int NumColumns;

        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            if (Inventory != null)
            {
                CairoFont labelFont = CairoFont.WhiteSmallishText();
                CairoFont hoverFont = CairoFont.WhiteDetailText();
                CairoFont inputFont = CairoFont.TextInput();



                ElementBounds tradeGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).WithFixedOffset(15,GuiStyle.TitleBarHeight+15);
                composer.AddItemSlotGrid(Inventory, (Gui as GUIModularBlockEntity).SendPacket, 1, [0], tradeGrid);
                rootBounds.WithChild(tradeGrid);

                ElementBounds tradePassLabelBounds = ElementBounds.Fixed(15, GuiStyle.TitleBarHeight, 500, 25).FixedRightOf(tradeGrid).WithFixedOffset(15,25);
                composer.AddStaticText("Trade Pass", labelFont, tradePassLabelBounds);
                composer.AddHoverText("", hoverFont, 500, tradePassLabelBounds);
                rootBounds.WithChild(tradePassLabelBounds);

                ElementBounds currencyLabelBounds = ElementBounds.FixedSize(500, 25).FixedUnder(tradeGrid,15).WithFixedOffset(15,0);
                composer.AddStaticText("Currency", labelFont, currencyLabelBounds);
                composer.AddHoverText("", hoverFont, 500, currencyLabelBounds);
                rootBounds.WithChild(currencyLabelBounds);

                ElementBounds currencyGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 15, 0, NumColumns, (int)Math.Ceiling(Inventory.CurrencySlots.Length / (double)NumColumns)).FixedUnder(currencyLabelBounds);
                composer.AddItemSlotGrid(Inventory, (Gui as GUIModularBlockEntity).SendPacket, NumColumns, GUIUtils.GetArrayRange(1, Inventory.CurrencySlots.Length), currencyGrid);
                rootBounds.WithChild(currencyGrid);

                ElementBounds couponLabelBounds = ElementBounds.FixedSize(500, 25).FixedUnder(currencyGrid,15).WithFixedOffset(15, 0);
                composer.AddStaticText("Coupons", labelFont, couponLabelBounds);
                composer.AddHoverText("", hoverFont, 500, couponLabelBounds);
                rootBounds.WithChild(couponLabelBounds);

                ElementBounds couponGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 15, 0, NumColumns, (int)Math.Ceiling(Inventory.CouponSlots.Length / (double)NumColumns)).FixedUnder(couponLabelBounds);
                composer.AddItemSlotGrid(Inventory, (Gui as GUIModularBlockEntity).SendPacket, NumColumns, GUIUtils.GetArrayRange(Inventory.CurrencySlots.Length+1, Inventory.CouponSlots.Length),  couponGrid);
                rootBounds.WithChild(couponGrid);


            }
            else
            {
                ElementBounds textBounds = ElementBounds.FixedSize(400, 300).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(textBounds);

                composer.AddStaticText(Lang.Get("commercially:container-no-inventory"), CairoFont.WhiteSmallText(), textBounds);
            }
        }

        public override void Initialize(IModularGui gui, BlockEntity entity = null)
        {
            base.Initialize(gui, entity);
            Inventory = entity?.GetBehavior<IInventoryProvider>()?.Inventory as RegisterInventory;
            NumColumns = GetConfiguration()?["NumColumns"].AsInt(10) ?? 10;

        }

        public override bool IsVisible()
        {
            return true;
        }

        public override void OnGuiClosed()
        {
            
        }

        public override void OnGuiOpened()
        {
            
        }

        public override void OnRecievedData(byte[] data)
        {
        }

        public override byte[] OnSendData(BlockEntity entity, Caller caller, BlockSelection blockSel, string key)
        {
            return null;
        }
    }
}
