
using Commercially.Common.GUI;
using Commercially.Common.Interfaces;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory;
using Commercially.Vinconomy.Inventory.StallSlots;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Commercially.Vinconomy.GUI.Tabs
{
    internal class ClothingStandCustomerTab : ModularTab
    {
        public const string CODE = "Vinconomy.ClothingStandCustomer";
        public override string Code => CODE;
        public override string TabName => Lang.Get("vinconomy:tabname-generic-customer");

        DummyInventory DInv;
        VinconBaseInventory Inventory;
        IStallInventoryProvider StallProvider;
        
        public override void Initialize(IModularGui gui, BlockEntity entity = null)
        {
            base.Initialize(gui, entity);
            StallProvider = entity?.GetBehavior<IStallInventoryProvider>();
            Inventory = StallProvider?.Inventory as VinconBaseInventory;

            DInv = new DummyInventory(this.ClientApi, 31);

            


        }

        private void UpdateStock()
        {
            for (int i = 0; i < 15; i++)
            {
                BaseStallSlot slot = StallProvider.GetStallSlot(i);
                ItemStack currency = slot.Currency.Itemstack?.Clone();
                ItemStack product = slot.Product.Itemstack?.Clone();

                DInv[i * 2] = new DummySlot(currency);
                DInv[(i * 2) + 1] = new DummySlot(product);
            }
        }

        public override void Compose(GuiComposer composer, ElementBounds rootBounds)
        {
            if (Inventory != null && StallProvider != null)
            {
                UpdateStock();

                CairoFont hoverText = CairoFont.WhiteDetailText();
                CairoFont smallText = CairoFont.WhiteSmallText();
                CairoFont labelTextFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center);

                ElementBounds settingBounds = ElementBounds.FixedSize(250, 200).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                int btnWidth = 80;
                int elementHeight = 45;
                int paddingH = 15;
                int paddingW = 5;
                int columnPadding = 40;

                ElementBounds armorHeadCurrencyIcon =   ElementBounds.FixedSize(elementHeight,  elementHeight);
                ElementBounds armorHeadDealButton =     ElementBounds.FixedSize(btnWidth,       elementHeight).FixedRightOf(armorHeadCurrencyIcon,  paddingW);
                ElementBounds armorHeadProductIcon =    ElementBounds.FixedSize(elementHeight,  elementHeight).FixedRightOf(armorHeadDealButton,    paddingW);
                settingBounds.WithChildren(armorHeadDealButton, armorHeadCurrencyIcon, armorHeadProductIcon);
                ElementBounds headCurrencyIcon =        ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(armorHeadCurrencyIcon, paddingH);
                ElementBounds headDealButton =          ElementBounds.FixedSize(btnWidth,       elementHeight).FixedUnder(armorHeadCurrencyIcon, paddingH).FixedRightOf(headCurrencyIcon,   paddingW);
                ElementBounds headProductIcon =         ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(armorHeadCurrencyIcon, paddingH).FixedRightOf(headDealButton,     paddingW);
                settingBounds.WithChildren(headDealButton, headCurrencyIcon, headProductIcon);
                ElementBounds faceCurrencyIcon =        ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(headCurrencyIcon, paddingH);
                ElementBounds faceDealButton =          ElementBounds.FixedSize(btnWidth,       elementHeight).FixedUnder(headCurrencyIcon, paddingH).FixedRightOf(faceCurrencyIcon,    paddingW);
                ElementBounds faceProductIcon =         ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(headCurrencyIcon, paddingH).FixedRightOf(faceDealButton,      paddingW);
                settingBounds.WithChildren(faceDealButton, faceCurrencyIcon, faceProductIcon);
                ElementBounds neckCurrencyIcon =        ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(faceCurrencyIcon, paddingH);
                ElementBounds neckDealButton =          ElementBounds.FixedSize(btnWidth,       elementHeight).FixedUnder(faceCurrencyIcon, paddingH).FixedRightOf(neckCurrencyIcon,    paddingW);
                ElementBounds neckProductIcon =         ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(faceCurrencyIcon, paddingH).FixedRightOf(neckDealButton,      paddingW);
                settingBounds.WithChildren(neckDealButton, neckCurrencyIcon, neckProductIcon);
                ElementBounds emblemCurrencyIcon =      ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(neckCurrencyIcon, paddingH);
                ElementBounds emblemDealButton =        ElementBounds.FixedSize(btnWidth,       elementHeight).FixedUnder(neckCurrencyIcon, paddingH).FixedRightOf(emblemCurrencyIcon,  paddingW);
                ElementBounds emblemProductIcon =       ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(neckCurrencyIcon, paddingH).FixedRightOf(emblemDealButton,    paddingW);
                settingBounds.WithChildren(emblemDealButton, emblemCurrencyIcon, emblemProductIcon);


                ElementBounds armorBodyCurrencyIcon =   ElementBounds.FixedSize(elementHeight,  elementHeight).FixedRightOf(emblemProductIcon, paddingW + columnPadding);
                ElementBounds armorBodyDealButton =     ElementBounds.FixedSize(btnWidth,       elementHeight).FixedRightOf(armorBodyCurrencyIcon, paddingW);
                ElementBounds armorBodyProductIcon =    ElementBounds.FixedSize(elementHeight,  elementHeight).FixedRightOf(armorBodyDealButton, paddingW);
                settingBounds.WithChildren(armorBodyDealButton, armorBodyCurrencyIcon, armorBodyProductIcon);
                ElementBounds upperBodyOverCurrencyIcon = ElementBounds.FixedSize(elementHeight, elementHeight).FixedUnder(armorBodyCurrencyIcon, paddingH).FixedRightOf(emblemProductIcon,      paddingW + columnPadding);
                ElementBounds upperBodyOverDealButton =   ElementBounds.FixedSize(btnWidth,      elementHeight).FixedUnder(armorBodyCurrencyIcon, paddingH).FixedRightOf(upperBodyOverCurrencyIcon, paddingW);
                ElementBounds upperBodyOverProductIcon =  ElementBounds.FixedSize(elementHeight, elementHeight).FixedUnder(armorBodyCurrencyIcon, paddingH).FixedRightOf(upperBodyOverDealButton,   paddingW);
                settingBounds.WithChildren(upperBodyOverDealButton, upperBodyOverCurrencyIcon, upperBodyOverProductIcon);
                ElementBounds upperBodyCurrencyIcon =   ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(upperBodyOverCurrencyIcon, paddingH).FixedRightOf(emblemProductIcon, paddingW + columnPadding);
                ElementBounds upperBodyDealButton =     ElementBounds.FixedSize(btnWidth,       elementHeight).FixedUnder(upperBodyOverCurrencyIcon, paddingH).FixedRightOf(upperBodyCurrencyIcon, paddingW);
                ElementBounds upperBodyProductIcon =    ElementBounds.FixedSize(elementHeight,  elementHeight).FixedUnder(upperBodyOverCurrencyIcon, paddingH).FixedRightOf(upperBodyDealButton, paddingW);
                settingBounds.WithChildren(upperBodyDealButton, upperBodyCurrencyIcon, upperBodyProductIcon);
                ElementBounds waistCurrencyIcon =       ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(upperBodyCurrencyIcon, paddingH).FixedRightOf(emblemProductIcon, paddingW + columnPadding);
                ElementBounds waistDealButton =         ElementBounds.FixedSize(btnWidth,        elementHeight).FixedUnder(upperBodyCurrencyIcon, paddingH).FixedRightOf(waistCurrencyIcon, paddingW);
                ElementBounds waistProductIcon =        ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(upperBodyCurrencyIcon, paddingH).FixedRightOf(waistDealButton, paddingW);
                settingBounds.WithChildren(waistDealButton, waistCurrencyIcon, waistProductIcon);
                ElementBounds lowerBodyCurrencyIcon =   ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(waistCurrencyIcon, paddingH).FixedRightOf(emblemProductIcon, paddingW + columnPadding);
                ElementBounds lowerBodyDealButton =     ElementBounds.FixedSize(btnWidth,        elementHeight).FixedUnder(waistCurrencyIcon, paddingH).FixedRightOf(lowerBodyCurrencyIcon, paddingW);
                ElementBounds lowerBodyProductIcon =    ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(waistCurrencyIcon, paddingH).FixedRightOf(lowerBodyDealButton, paddingW);
                settingBounds.WithChildren(lowerBodyDealButton, lowerBodyCurrencyIcon, lowerBodyProductIcon);

                ElementBounds shoulderCurrencyIcon =    ElementBounds.FixedSize(elementHeight,   elementHeight).FixedRightOf(lowerBodyProductIcon, paddingW + columnPadding);
                ElementBounds shoulderDealButton =      ElementBounds.FixedSize(btnWidth,        elementHeight).FixedRightOf(shoulderCurrencyIcon, paddingW);
                ElementBounds shoulderProductIcon =     ElementBounds.FixedSize(elementHeight,   elementHeight).FixedRightOf(shoulderDealButton, paddingW);
                settingBounds.WithChildren(shoulderDealButton, shoulderCurrencyIcon, shoulderProductIcon);
                ElementBounds armCurrencyIcon =         ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(shoulderCurrencyIcon, paddingH).FixedRightOf(upperBodyProductIcon, paddingW + columnPadding);
                ElementBounds armDealButton =           ElementBounds.FixedSize(btnWidth,        elementHeight).FixedUnder(shoulderCurrencyIcon, paddingH).FixedRightOf(armCurrencyIcon, paddingW);
                ElementBounds armProductIcon =          ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(shoulderCurrencyIcon, paddingH).FixedRightOf(armDealButton, paddingW);
                settingBounds.WithChildren(armDealButton, armCurrencyIcon, armProductIcon);
                ElementBounds handCurrencyIcon =        ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(armCurrencyIcon, paddingH).FixedRightOf(upperBodyProductIcon, paddingW + columnPadding);
                ElementBounds handDealButton =          ElementBounds.FixedSize(btnWidth,        elementHeight).FixedUnder(armCurrencyIcon, paddingH).FixedRightOf(handCurrencyIcon, paddingW);
                ElementBounds handProductIcon =         ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(armCurrencyIcon, paddingH).FixedRightOf(handDealButton, paddingW);
                settingBounds.WithChildren(handDealButton, handCurrencyIcon, handProductIcon);
                ElementBounds armorLegsCurrencyIcon =   ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(handCurrencyIcon, paddingH).FixedRightOf(upperBodyProductIcon, paddingW + columnPadding);
                ElementBounds armorLegsDealButton =     ElementBounds.FixedSize(btnWidth,        elementHeight).FixedUnder(handCurrencyIcon, paddingH).FixedRightOf(armorLegsCurrencyIcon, paddingW);
                ElementBounds armorLegsProductIcon =    ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(handCurrencyIcon, paddingH).FixedRightOf(armorLegsDealButton, paddingW);
                settingBounds.WithChildren(armorLegsDealButton, armorLegsCurrencyIcon, armorLegsProductIcon);
                ElementBounds footCurrencyIcon =        ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(armorLegsCurrencyIcon, paddingH).FixedRightOf(upperBodyProductIcon, paddingW + columnPadding);
                ElementBounds footDealButton =          ElementBounds.FixedSize(btnWidth,        elementHeight).FixedUnder(armorLegsCurrencyIcon, paddingH).FixedRightOf(footCurrencyIcon, paddingW);
                ElementBounds footProductIcon =         ElementBounds.FixedSize(elementHeight,   elementHeight).FixedUnder(armorLegsCurrencyIcon, paddingH).FixedRightOf(footDealButton, paddingW);
                settingBounds.WithChildren(footDealButton, footCurrencyIcon, footProductIcon);


                //ElementBounds outfitDealButton =        ElementBounds.FixedSize(((paddingW + elementHeight) * 2) + btnWidth, elementHeight).FixedUnder(footCurrencyIcon, paddingH*2).FixedRightOf(armorHeadProductIcon, paddingW + columnPadding);
                //settingBounds.WithChild(outfitDealButton);

                settingBounds.BothSizing = ElementSizing.FitToChildren;

                rootBounds.WithChildren(settingBounds);

                string dealText = Lang.Get("vinconomy:gui-deal");
                string buyAllText = Lang.Get("vinconomy:gui-buy-all");

                composer.BeginChildElements(settingBounds)
                    .AddButton(dealText, () => { return this.OnPurchase(0); }, armorHeadDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [0], armorHeadCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [1], armorHeadProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(1); }, headDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [2], headCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [3], headProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(2); }, faceDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [4], faceCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [5], faceProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(3); }, neckDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [6], neckCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [7], neckProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(4); }, emblemDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [8], emblemCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [9], emblemProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(5); }, armorBodyDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [0], armorBodyCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [11], armorBodyProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(6); }, upperBodyDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [12], upperBodyCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [13], upperBodyProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(7); }, upperBodyOverDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [14], upperBodyOverCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [15], upperBodyOverProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(8); }, waistDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [16], waistCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [17], waistProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(9); }, lowerBodyDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [18], lowerBodyCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [19], lowerBodyProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(10); }, shoulderDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [20], shoulderCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [21], shoulderProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(11); }, armDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [22], armCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [23], armProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(12); }, handDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [24], handCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [25], handProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(13); }, armorLegsDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [26], armorLegsCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [27], armorLegsProductIcon)
                    .AddButton(dealText, () => { return this.OnPurchase(14); }, footDealButton, EnumButtonStyle.Small)
                    .AddItemSlotGrid(DInv, null, 1, [28], footCurrencyIcon)
                    .AddItemSlotGrid(DInv, null, 1, [29], footProductIcon)
                    //.AddButton(Lang.Get("vinconomy:gui-buy-all"), () => { return this.OnPurchase(-1); }, outfitDealButton, EnumButtonStyle.Small)
                .EndChildElements();
            }
            else if (StallProvider == null)
            {
                ElementBounds textBounds = ElementBounds.FixedSize(400, 450).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(textBounds);
                composer.AddStaticText(Lang.Get("vinconomy:not-a-shop"), CairoFont.WhiteSmallText(), textBounds);
            }
            else
            {
                ElementBounds textBounds = ElementBounds.FixedSize(400, 450).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                rootBounds.WithChild(textBounds);
                composer.AddStaticText(Lang.Get("commercially:container-no-inventory"), CairoFont.WhiteSmallText(), textBounds);
            }
        }

        private bool OnPurchase(int stallSlot)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(stallSlot);
                writer.Write(1);
                data = ms.ToArray();

                ClientApi.Network.SendBlockEntityPacket(BlockEntity.Pos, CommerciallyConstants.PURCHASE_ITEMS, data);
            }
            return true;
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
