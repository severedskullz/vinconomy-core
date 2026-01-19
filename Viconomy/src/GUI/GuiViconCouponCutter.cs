using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.ItemTypes;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiViconCouponCutter : GuiDialogBlockEntity
    {
        private InventoryBase inventory;
        private BEVinconCouponCutter CouponPrinter;

        private ShopRegistration[] shops;


        public GuiViconCouponCutter(string dialogTitle, InventoryBase inventory, BlockPos pos, ICoreClientAPI coreClientAPI) : base(dialogTitle, inventory,  pos, coreClientAPI)
        {

            if (base.IsDuplicate)
            {
                return;
            }

            this.inventory = inventory;
            CouponPrinter = capi.World.BlockAccessor.GetBlockEntity<BEVinconCouponCutter>(pos);
            VinconomyCoreSystem modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();
            ShopRegistration[] allRegisters = modSystem.GetRegistry().GetShopsForOwner(coreClientAPI.World.Player.PlayerUID);
            List<ShopRegistration> filteredRegisters = new List<ShopRegistration>();
            foreach (ShopRegistration register in allRegisters)
            {
                if (register.Position != null)
                {
                    filteredRegisters.Add(register);
                }
            }

            shops = filteredRegisters.ToArray();

            Compose();
        }

        private void Compose()
        {
            int shopLength = shops.Length;
            string[] shopsNames = new string[shopLength];
            string[] shopsKeys = new string[shopLength];

            for (int i = 0; i < shops.Length; i++)
            {
                shopsNames[i] = shops[i].Name;
                shopsKeys[i] = shops[i].ID.ToString();
            }

            try
            {
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);


                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                ElementBounds couponNameLabel = ElementBounds.FixedSize(400, 20).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                ElementBounds couponNameInput = ElementBounds.FixedSize(400, 30).FixedUnder(couponNameLabel).WithFixedOffset(0,10);

                ElementBounds appliedShopsLabel = ElementBounds.FixedSize(400, 20).FixedUnder(couponNameInput).WithFixedOffset(0, 10);
                ElementBounds appliedShopsInput = ElementBounds.FixedSize(400, 30).FixedUnder(appliedShopsLabel).WithFixedOffset(0, 10);
                ElementBounds appliedShopsNoteLabel = ElementBounds.FixedSize(400, 20).FixedUnder(appliedShopsInput);

                ElementBounds couponValueLabel = ElementBounds.FixedSize(250, 20).FixedUnder(appliedShopsNoteLabel).WithFixedOffset(0, 10);
                ElementBounds couponValueInput = ElementBounds.FixedSize(80, 30).FixedUnder(couponValueLabel).WithFixedOffset(0, 10);
                ElementBounds couponDiscountTypeInput = ElementBounds.FixedSize(100, 30).FixedUnder(couponValueLabel).FixedRightOf(couponValueInput).WithFixedOffset(10, 10);
                ElementBounds couponBonusTypeInput = ElementBounds.FixedSize(200, 30).FixedUnder(couponValueLabel).FixedRightOf(couponDiscountTypeInput).WithFixedOffset(10, 10);

                ElementBounds consumeCouponInput = ElementBounds.FixedSize(40, 40).FixedUnder(couponValueInput).WithFixedOffset(0, 10);
                ElementBounds consumeCouponLabel = ElementBounds.FixedSize(360, 20).FixedUnder(couponValueInput).WithFixedOffset(0, 13).FixedRightOf(consumeCouponInput);

                ElementBounds itemWhitelistLabel = ElementBounds.FixedSize(400, 20).FixedUnder(consumeCouponInput).WithFixedOffset(0, 00);
                ElementBounds itemWhitelistBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 5, 2).FixedUnder(itemWhitelistLabel).WithFixedOffset(0, 10);
                ElementBounds itemBlacklistInput = ElementBounds.FixedSize(40, 40).FixedUnder(itemWhitelistBounds).WithFixedOffset(0, 10);
                ElementBounds itemBlacklistLabel = ElementBounds.FixedSize(360, 20).FixedUnder(itemWhitelistBounds).WithFixedOffset(0, 13).FixedRightOf(itemBlacklistInput);

                ElementBounds itemInputLabel = ElementBounds.FixedSize(200, 20).FixedUnder(itemBlacklistInput).WithFixedOffset(0, 0);
                ElementBounds itemInputBounds = ElementStdBounds.Slot().FixedUnder(itemInputLabel).WithFixedOffset(0, 10);

                ElementBounds cutButton = ElementBounds.FixedSize(200, 20).FixedUnder(itemInputBounds).WithFixedOffset(100, 10);

                bgBounds.WithChildren(couponNameLabel, couponNameInput,
                    appliedShopsLabel, appliedShopsNoteLabel, appliedShopsInput,
                    couponValueLabel, couponValueInput, couponDiscountTypeInput, couponBonusTypeInput,
                    consumeCouponInput, consumeCouponLabel,
                    itemWhitelistLabel, itemWhitelistBounds, itemBlacklistInput, itemBlacklistLabel,
                    itemInputLabel, itemInputBounds, 
                    cutButton
                    );


                CairoFont labelFont = CairoFont.WhiteSmallishText();
                CairoFont hoverText = CairoFont.WhiteDetailText();

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconCouponPrinter", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)

                    .AddStaticText(Lang.Get("vinconomy:gui-name"), labelFont, couponNameLabel)
                    .AddTextInput(couponNameInput, OnNameChanged, labelFont, "couponName")

                    .AddStaticText(Lang.Get("vinconomy:gui-whitelisted-shops"), labelFont, appliedShopsLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-whitelisted-shops"), hoverText, 500, appliedShopsLabel)
                    .AddStaticText(Lang.Get("vinconomy:gui-whitelisted-shops-note"), CairoFont.WhiteDetailText(), appliedShopsNoteLabel)
                    .AddMultiSelectDropDown(shopsKeys, shopsNames, -1, OnShopChanged, appliedShopsInput, "appliedShops")

                    .AddStaticText(Lang.Get("vinconomy:gui-coupon"), labelFont, couponValueLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-coupon"), hoverText, 500, couponValueLabel)
                    .AddNumberInput(couponValueInput, OnNumberValueChanged, labelFont, "couponValue")
                    .AddDropDown([ItemCoupon.DISCOUNT_TYPE_UNIT, ItemCoupon.DISCOUNT_TYPE_PERCENT], [Lang.Get("vinconomy:gui-units"), Lang.Get("vinconomy:gui-percent")], 0, OnChangeNumericType, couponDiscountTypeInput, "couponDiscountType")
                    .AddDropDown([ItemCoupon.BONUS_TYPE_PRODUCT, ItemCoupon.BONUS_TYPE_DISCOUNT], [Lang.Get("vinconomy:gui-bonus-product"), Lang.Get("vinconomy:gui-price-discount")], 0, OnChangeBonusType, couponBonusTypeInput, "couponBonusType")

                    .AddSwitch(OnToggleConsumeCoupon, consumeCouponInput, "consumeCoupon")
                    .AddStaticText(Lang.Get("vinconomy:gui-consume-coupon"), labelFont, consumeCouponLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-consume-coupon"), hoverText, 500, consumeCouponLabel)

                    .AddDynamicText(Lang.Get("vinconomy:gui-item-whitelist"), labelFont, itemWhitelistLabel, "itemListLabel")
                    .AddHoverText(Lang.Get("vinconomy:tooltip-item-whitelist"), hoverText, 500, itemWhitelistLabel)
                    .AddItemSlotGrid(inventory, SendInvPacket, 5, [1,2, 3, 4, 5, 6, 7, 8, 9, 10], itemWhitelistBounds)
                    .AddSwitch(OnToggleBlacklist, itemBlacklistInput, "isBlacklist")
                    .AddStaticText(Lang.Get("vinconomy:gui-blacklist"), labelFont, itemBlacklistLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-blacklist"), hoverText, 500, itemBlacklistLabel)

                    .AddStaticText(Lang.Get("vinconomy:gui-paper"), labelFont, itemInputLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-paper"), hoverText, 500, itemInputLabel)
                    .AddItemSlotGrid(inventory, SendInvPacket, 1, [0], itemInputBounds)

                    .AddButton(Lang.Get("vinconomy:gui-cut"), OnCut, cutButton);

                SingleComposer.GetTextInput("couponName").SetValue(CouponPrinter.CouponName);
                SingleComposer.GetNumberInput("couponValue").SetValue(CouponPrinter.CouponValue);
                SingleComposer.GetSwitch("consumeCoupon").SetValue(CouponPrinter.ConsumeCoupon);
                SingleComposer.GetSwitch("isBlacklist").SetValue(CouponPrinter.ItemBlacklist);
                SingleComposer.GetDropDown("couponDiscountType").SetSelectedValue(CouponPrinter.DiscountType);
                SingleComposer.GetDropDown("couponBonusType").SetSelectedValue(CouponPrinter.BonusType);
                SingleComposer.GetDynamicText("itemListLabel").SetNewText(CouponPrinter.ItemBlacklist ? Lang.Get("vinconomy:gui-item-blacklist") : Lang.Get("vinconomy:gui-item-whitelist"));

                SingleComposer.Compose();

                GuiElementDropDown element = SingleComposer.GetDropDown("appliedShops");
                string[] selectedKeys = new string[CouponPrinter.AppliedShops.Length];
                for (int i = 0; i < selectedKeys.Length; i++)
                {
                    selectedKeys[i] = CouponPrinter.AppliedShops[i].ToString();
                }
                element.SetSelectedValue(selectedKeys);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        private void OnChangeBonusType(string code, bool selected)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(code);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_COUPON_BONUS_TYPE, data);
        }

        private void OnChangeNumericType(string code, bool selected)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(code);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_COUPON_DISCOUNT_TYPE, data);
        }

        private void OnNameChanged(string name)
        {
            // Item name shouldnt be more than 100 characters. Thats rediculous. Too bad we cant set a max length on the text input.
            if (name.Length > 100)
            {
                name = name.Substring(0, 100);
            }

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(name);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_ITEM_NAME, data);
        }

        private void OnShopChanged(string code, bool selected)
        {
            GuiElementDropDown dropdown = SingleComposer.GetDropDown("appliedShops");
            string[] selectedShops = dropdown.SelectedValues;

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(selectedShops.Length);
                foreach (string shop in selectedShops)
                {
                    Int32.TryParse(shop, out int val);
                    writer.Write(val);
                }
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_COUPON_SHOPS, data);
        }

        private void OnNumberValueChanged(string txt)
        {
            if (txt.Length == 0)
                return;

            Int32.TryParse(txt, out int val);
            val = Math.Max(val, 0);

            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(val);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_ITEM_PRICE, data);
        }

        private void OnToggleConsumeCoupon(bool consume)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(consume);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_COUPON_CONSUME_ON_PURCHASE, data);
        }

        private void OnToggleBlacklist(bool blacklist)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(blacklist);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_COUPON_BLACKLIST, data);
            SingleComposer.GetDynamicText("itemListLabel").SetNewText(blacklist ? Lang.Get("vinconomy:gui-item-blacklist") : Lang.Get("vinconomy:gui-item-whitelist"));
        }

        private bool OnCut()
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.ACTIVATE_BLOCK, null);
            return true;
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