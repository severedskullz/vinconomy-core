using System;
using Viconomy.Inventory;
using Viconomy.TradeNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Viconomy.GUI
{
    public class GuiVinconTrader : GuiDialogGeneric
    {
        TradeNetworkShop shop;
        VinconNetworkInventory inventory;
        DummyInventory ProductInventory;
        DummyInventory CurrencyInventory;

        VinconNetworkItemSlot selectedProduct;

        public GuiVinconTrader(string DialogTitle, TradeNetworkShop shop, ICoreClientAPI capi) : base(DialogTitle, capi)
        {
            this.shop = shop;
            this.inventory = new VinconNetworkInventory(shop, capi);
            inventory.OnTradeSelected += OnTradeSelected;
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
                this.Compose();
            } catch (Exception ex) { 
                Console.WriteLine(ex.ToString());
            }
        }

        private void OnTradeSelected(VinconNetworkItemSlot product)
        {
            selectedProduct = product;
            ProductInventory[0].Itemstack = product.Product.Clone();
            CurrencyInventory[0].Itemstack = product.Currency.Clone();

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
            ProductInventory[0].Itemstack.StackSize = selectedProduct.Product.StackSize * (int)amount;
            CurrencyInventory[0].Itemstack.StackSize = selectedProduct.Currency.StackSize * (int)amount;
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
                ElementBounds itemSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 7, (int)Math.Ceiling(inventory.Count / 7.0f)).WithParent(itemContainerBounds);

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
               .AddStaticText(Lang.Get("vinconomy:gui-owner") + shop.owner, CairoFont.WhiteSmallText(), descLabelBounds)
               .AddStaticText(Lang.Get("vinconomy:gui-server") + shop.serverName, CairoFont.WhiteSmallText(), serverLabelBounds)




               .AddStaticText(Lang.Get("vinconomy:gui-available-products"), CairoFont.WhiteSmallText(), productLabelBounds)
               .BeginChildElements()
                   .AddInset(itemInsetContainerBounds, insetDepth)
                   .BeginClip(itemClipBounds)
                        .AddContainer(itemContainerBounds, "products")
                        .AddItemSlotGrid(inventory, null, 7, itemSlotBounds, "item-grid")
                   .EndClip()
                   .AddVerticalScrollbar(OnNewItemScrollbarValue, itemScrollbarBounds, "item-scrollbar")
               .EndChildElements()

                .AddStaticText(Lang.Get("vinconomy:gui-quantity"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                .AddNumberInput(quantitySelectionBounds, OnAmountChanged, CairoFont.WhiteSmallText(), "quantity")
                .AddButton(Lang.Get("vinconomy:gui-deal"), new ActionConsumable(this.OnPurchase), purchaseButtonBounds, EnumButtonStyle.Small, "save")
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
                this.capi.Logger.Debug(e.ToString());
            }
        }

        private void OnAmountChanged(string txt)
        {
            if (txt.Length == 0)
                return;

            Int32.TryParse(txt, out int amount);

            if (amount < 1)
            {
                amount = 1;
                GuiElementNumberInput quantity = SingleComposer.GetNumberInput("quantity");
                quantity.SetValue(1);
            } else if (amount > selectedProduct.TotalStock) {
                GuiElementNumberInput quantity = SingleComposer.GetNumberInput("quantity");
                quantity.SetValue(selectedProduct.TotalStock);
                amount = selectedProduct.TotalStock;
            }
            UpdatePurchaseAmounts(amount);
        }

        private bool OnPurchase()
        {
            return true;
        }

        private float NoDecay(EnumTransitionType transType, ItemStack stack, float mulByConfig)
        {
            return 0;
        }

        private void PacketHandler(object obj)
        {
            /*
            if (obj is Packet_ActivateInventorySlot)
            {
                Packet_ActivateInventorySlot pack = (Packet_ActivateInventorySlot)obj;
                Console.WriteLine(pack.ToString());
            }
            Packet_Client packet = (Packet_Client)obj;
            Packet_ActivateInventorySlot inv = packet.ActivateInventorySlot;
            Console.WriteLine(inv);
            */
        }

        private void OnNewDescriptionScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetRichtext("description").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
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
    }
}
