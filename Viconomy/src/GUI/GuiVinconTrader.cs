using System;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Viconomy.Network;
using Viconomy.TradeNetwork.Api;
using Vintagestory.GameContent;

namespace Viconomy.GUI
{
    public class GuiVinconTrader : GuiDialog
    {
        string DialogTitle;

        TradeNetworkShop Shop;
        VinconNetworkInventory Inventory;
        DummyInventory ProductInventory;
        DummyInventory CurrencyInventory;

        VinconNetworkItemSlot SelectedProduct;
        EntityAgent OwningEntity;

        public override string ToggleKeyCombinationCode => null;

        public GuiVinconTrader(string dialogTitle, TradeNetworkShop shop, EntityAgent owningEntity, ICoreClientAPI capi) : base(capi)
        {
            this.DialogTitle = dialogTitle;
            this.Shop = shop;
            this.OwningEntity = owningEntity;
            Inventory = new VinconNetworkInventory(shop, capi);
            Inventory.OnTradeSelected += OnTradeSelected;
            ProductInventory = new DummyInventory(capi, 1);
            ProductInventory.TakeLocked = true;
            ProductInventory.PutLocked = true;
            ProductInventory.OnAcquireTransitionSpeed += NoDecay;
            CurrencyInventory = new DummyInventory(capi, 1);
            CurrencyInventory.TakeLocked = true;
            CurrencyInventory.PutLocked = true;
            CurrencyInventory.OnAcquireTransitionSpeed += NoDecay;
            

            try
            {
                Compose();
            } catch (Exception ex) { 
                Console.WriteLine(ex.ToString());
            }
        }

        public void UpdateSlotCount(int x, int y, int z, int stallSlot, int amount)
        {
            VinconNetworkItemSlot slot = Inventory.GetProductById(x, y, z, stallSlot);
            slot.TotalStock -= amount * slot.Product.StackSize;
            slot.Itemstack.StackSize = slot.TotalStock;
        }

        private void OnTradeSelected(VinconNetworkItemSlot product)
        {
            if (product.DrawUnavailable && false)
            {
                return;
            }

            SelectedProduct = product;
            if (product.Itemstack != null)
            {
                ProductInventory[0].Itemstack = product.Product.Clone();
            }

            if (product.Currency != null)
            {
                CurrencyInventory[0].Itemstack = product.Currency.Clone();
            }

            GuiElementNumberInput quantity = SingleComposer.GetNumberInput("quantity");
            quantity.SetValue(1);

            float amount = quantity.GetValue();
            UpdatePurchaseAmounts(amount);
        }

        private void UpdatePurchaseAmounts(float amount)
        {

            if (amount <= 1)
            {
                amount = 1;
            }
            if ( SelectedProduct.Product != null)
            {
                ProductInventory[0].Itemstack.StackSize = SelectedProduct.Product.StackSize * (int)amount;
            } else
            {
                ProductInventory[0].Itemstack = null;
            }

            if (SelectedProduct.Currency != null)
            {
                CurrencyInventory[0].Itemstack.StackSize = SelectedProduct.Currency.StackSize * (int)amount;
            }
            else
            {
                CurrencyInventory[0].Itemstack = null;
            }

        }

        private void Compose()
        {
            int insetWidth = 360;
            int insetHeight = 300;
            int insetDepth = 3;

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            ElementBounds settingBounds = ElementBounds.FixedSize(800, 600).WithFixedOffset(0, GuiStyle.TitleBarHeight);

            try
            {
                ElementBounds descLabelBounds = ElementBounds.Fixed(10, GuiStyle.TitleBarHeight+10, insetWidth+25, 20);
                ElementBounds serverLabelBounds = descLabelBounds.BelowCopy().WithFixedOffset(0, 5);

                ElementBounds productLabelBounds = serverLabelBounds.BelowCopy().WithFixedOffset(0,5);
                ElementBounds itemInsetBounds = productLabelBounds.BelowCopy().WithFixedSize(insetWidth, insetHeight).WithFixedOffset(-10,0); // I dont understand where this padding of 10 is coming from
                ElementBounds itemInsetContainerBounds = itemInsetBounds.ForkContainingChild();
                ElementBounds itemScrollbarBounds = itemInsetContainerBounds.RightCopy().WithFixedOffset(5,0).WithFixedWidth(20);
                ElementBounds itemClipBounds = itemInsetBounds.ForkContainingChild();
                ElementBounds itemContainerBounds = itemInsetBounds.ForkContainingChild();
                ElementBounds itemSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 7, (int)Math.Ceiling(Inventory.Count / 7.0f)).WithParent(itemContainerBounds);

                ElementBounds currencyLabel = ElementBounds.FixedSize(60, 25).FixedRightOf(productLabelBounds).FixedUnder(serverLabelBounds).WithFixedOffset(0,-5);
                ElementBounds currencySlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(currencyLabel).FixedRightOf(descLabelBounds).WithFixedOffset(0, 5);

                ElementBounds purchaseLabel = ElementBounds.FixedSize(60, 25).FixedRightOf(currencyLabel).FixedUnder(serverLabelBounds).WithFixedOffset(20, -5);
                ElementBounds purchaseSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1).FixedUnder(purchaseLabel).FixedRightOf(currencyLabel).WithFixedOffset(20, 5);

                ElementBounds quantitySelectionLabel = ElementBounds.FixedSize(75, 30).FixedUnder(currencyLabel).FixedRightOf(descLabelBounds).WithFixedOffset(0, 75);
                ElementBounds quantitySelectionBounds = quantitySelectionLabel.RightCopy().WithFixedSize(75, 30).WithFixedOffset(0, -5);
                ElementBounds purchaseButtonBounds = ElementBounds.FixedSize(150, 40).FixedRightOf(descLabelBounds).FixedUnder(quantitySelectionLabel).WithFixedOffset(0,15);



                bgBounds.WithChildren(itemInsetBounds, itemScrollbarBounds, currencyLabel, currencySlotBounds, purchaseLabel, purchaseSlotBounds, quantitySelectionLabel, quantitySelectionBounds, purchaseButtonBounds);


                SingleComposer = capi.Gui.CreateCompo("GuiVinconTraderCatalog", dialogBounds)
               .AddShadedDialogBG(bgBounds)
               .AddDialogTitleBar(this.DialogTitle, OnTitleBarCloseClicked)
               .AddStaticText(Lang.Get("vinconomy:gui-f-owner", [Shop.Owner]), CairoFont.WhiteSmallText(), descLabelBounds)
               .AddStaticText(Lang.Get("vinconomy:gui-f-server", [Shop.ServerName]), CairoFont.WhiteSmallText(), serverLabelBounds)




               .AddStaticText(Lang.Get("vinconomy:gui-available-products"), CairoFont.WhiteSmallText(), productLabelBounds)
               .BeginChildElements()
                   .AddInset(itemInsetContainerBounds, insetDepth)
                   .BeginClip(itemClipBounds)
                        .AddContainer(itemContainerBounds, "products")
                        .AddItemSlotGrid(Inventory, null, 7, itemSlotBounds, "item-grid")
                   .EndClip()
                   .AddVerticalScrollbar(OnNewItemScrollbarValue, itemScrollbarBounds, "item-scrollbar")
               .EndChildElements()

                .AddStaticText(Lang.Get("vinconomy:gui-quantity"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                .AddNumberInput(quantitySelectionBounds, OnAmountChanged, CairoFont.WhiteSmallText(), "quantity")
                .AddButton(Lang.Get("vinconomy:gui-deal"), OnPurchase, purchaseButtonBounds, EnumButtonStyle.Small, "save")
                .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                .AddItemSlotGrid(CurrencyInventory, null, 1, new int[] { 0 }, currencySlotBounds, "CurrencySlot")

                .AddStaticText(Lang.Get("vinconomy:gui-product"), CairoFont.WhiteSmallText(), purchaseLabel)
                .AddItemSlotGrid(ProductInventory, null, 1, new int[] { 0 }, purchaseSlotBounds, "ProductSlot");

                GuiElementContainer scrollArea = SingleComposer.GetContainer("products");
                scrollArea.Add(SingleComposer.GetSlotGrid("item-grid"));
                SingleComposer.Compose();

                float itemScrollVisibleHeight = (float)itemClipBounds.fixedHeight;
                double itemScrollTotalHeight = SingleComposer.GetContainer("products").Bounds.fixedHeight;
                SingleComposer.GetScrollbar("item-scrollbar").SetHeights(itemScrollVisibleHeight, (float)itemScrollTotalHeight);
            }
            catch (Exception e)
            {
                capi.Logger.Debug(e.ToString());
            }
        }

        private void OnAmountChanged(string txt)
        {
            if (txt.Length == 0)
                return;

            Int32.TryParse(txt, out int amount);
            if (amount == 1)
            {
                return;
            } else if (SelectedProduct != null && amount * SelectedProduct.Product.StackSize > SelectedProduct.TotalStock)
            {
                GuiElementNumberInput quantity = SingleComposer.GetNumberInput("quantity");

                int maxAmount = (int)Math.Floor((double)SelectedProduct.TotalStock) / SelectedProduct.Product.StackSize;
                quantity.SetValue(maxAmount);
                amount = maxAmount;
            } else if (amount < 1)
            {
                amount = 1;
                GuiElementNumberInput quantity = SingleComposer.GetNumberInput("quantity");
                quantity.SetValue(1);
            } 
            UpdatePurchaseAmounts(amount);
        }

        private bool OnPurchase()
        {
            TradeNetworkPurchasePacket purchase = new TradeNetworkPurchasePacket();
            GuiElementNumberInput quantity = SingleComposer.GetNumberInput("quantity");
            purchase.Amount = (int)quantity.GetValue();
            purchase.X = SelectedProduct.X;
            purchase.Y = SelectedProduct.Y;
            purchase.Z = SelectedProduct.Z;
            purchase.StallSlot = SelectedProduct.StallSlot;
            byte[] data = SerializerUtil.Serialize(purchase);
            capi.Network.SendEntityPacket(OwningEntity.EntityId, 2001, data);
            return true;
        }

        private float NoDecay(EnumTransitionType transType, ItemStack stack, float mulByConfig)
        {
            return 0;
        }

        private void OnNewItemScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetContainer("products").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

        /*
        private bool OnBuySellClicked()
        {
            EnumTransactionResult num = traderInventory.TryBuySell(capi.World.Player);
            if (num == EnumTransactionResult.Success)
            {
                capi.Gui.PlaySound(new AssetLocation("sounds/effect/cashregister"), randomizePitch: false, 0.25f);
                (owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Purchase);
            }

            if (num == EnumTransactionResult.PlayerNotEnoughAssets)
            {
                (owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
                if (notifyPlayerMoneyTextSeconds <= 0.0)
                {
                    prevPlrAbsFixedX = base.SingleComposer.GetDynamicText("playerMoneyText").Bounds.absFixedX;
                    prevPlrAbsFixedY = base.SingleComposer.GetDynamicText("playerMoneyText").Bounds.absFixedY;
                }

                notifyPlayerMoneyTextSeconds = 1.5;
            }

            if (num == EnumTransactionResult.TraderNotEnoughAssets)
            {
                (owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
                if (notifyTraderMoneyTextSeconds <= 0.0)
                {
                    prevTdrAbsFixedX = base.SingleComposer.GetDynamicText("traderMoneyText").Bounds.absFixedX;
                    prevTdrAbsFixedY = base.SingleComposer.GetDynamicText("traderMoneyText").Bounds.absFixedY;
                }

                notifyTraderMoneyTextSeconds = 1.5;
            }

            if (num == EnumTransactionResult.TraderNotEnoughSupplyOrDemand)
            {
                (owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
            }

            capi.Network.SendEntityPacket(owningEntity.EntityId, 1000);
            TraderInventory_SlotModified(0);
            CalcAndUpdateAssetsDisplay();
            return true;
        }
        */

    }
}
