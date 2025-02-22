using System;
using System.Collections.Generic;
using Viconomy.Registry;
using Viconomy.TradeNetwork;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Viconomy.GUI
{
    public class GuiVinconTrader : GuiDialogGeneric
    {
        TradeNetworkShop shop;
        DummyInventory ProductInventory;
        DummyInventory CurrencyInventory;

        public GuiVinconTrader(string DialogTitle, TradeNetworkShop shop, ICoreClientAPI capi) : base(DialogTitle, capi)
        {
            this.shop = shop;
            List<TradeNetworkProduct> products = shop.products;
            ProductInventory = new DummyInventory(capi, products.Count);
            ProductInventory.TakeLocked = true;
            ProductInventory.PutLocked = true;
            ProductInventory.OnAcquireTransitionSpeed += NoDecay;
            CurrencyInventory = new DummyInventory(capi, products.Count);
            CurrencyInventory.TakeLocked = true;
            CurrencyInventory.PutLocked = true;
            CurrencyInventory.OnAcquireTransitionSpeed += NoDecay;
            int index = 0;

            //Add our Product and Currency to each inventory. Catch JSON errors on attributes, cuz Quotes in descriptions got me once already
            foreach (TradeNetworkProduct product in products)
            {

                ItemStack productStack = ResolveBlockOrItem(product.ProductCode, Math.Clamp(product.TotalStock,0,999));
                try
                {
                    if (product.ProductAttributes != null)
                    {
                        TreeAttribute attr = new TreeAttribute();
                        attr.FromBytes(product.ProductAttributes);

                        // Remove transition state from any food items. SQL entries are the last time it was inserted and isnt updated
                        attr.RemoveAttribute("transitionstate");

                        //JsonObject productAttr = JsonObject.FromJson(product.ProductAttributes);
                        productStack.Attributes = attr;//(ITreeAttribute)ToAttribute(productAttr.Token);

                    }
                }
                catch (Exception ex) { }
                ProductInventory[index].Itemstack = productStack;

                ItemStack currencyStack = ResolveBlockOrItem(product.CurrencyCode, product.CurrencyAmount);
                if (product.CurrencyAttributes != null)
                {
                    TreeAttribute attr = new TreeAttribute();

                    // Remove transition state from any food items. SQL entries are the last time it was inserted and isnt updated
                    attr.RemoveAttribute("transitionstate");

                    attr.FromBytes(product.CurrencyAttributes);
                    //JsonObject currencyAttr = JsonObject.FromJson(product.CurrencyAttributes);
                    currencyStack.Attributes = attr; // (ITreeAttribute)currencyAttr.ToAttribute();
                }
                CurrencyInventory[index].Itemstack = productStack;
                index++;
            }

            try
            {
                this.Compose();
            } catch (Exception ex) { 
                Console.WriteLine(ex.ToString());
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
                ElementBounds itemSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 7, (int)Math.Ceiling(ProductInventory.Count / 7.0f)).WithParent(itemContainerBounds);

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
                        .AddItemSlotGrid(ProductInventory, PacketHandler, 7, itemSlotBounds, "item-grid")
                   .EndClip()
                   .AddVerticalScrollbar(OnNewItemScrollbarValue, itemScrollbarBounds, "item-scrollbar")
               .EndChildElements()

                .AddStaticText(Lang.Get("vinconomy:gui-quantity"), CairoFont.WhiteSmallText(), quantitySelectionLabel)
                .AddNumberInput(quantitySelectionBounds, null, CairoFont.WhiteSmallText(), "quantity")
                .AddButton(Lang.Get("vinconomy:gui-deal"), new ActionConsumable(this.OnPurchase), purchaseButtonBounds, EnumButtonStyle.Small, "save")
                .AddStaticText(Lang.Get("vinconomy:gui-price"), CairoFont.WhiteSmallText(), currencyLabel)
                .AddPassiveItemSlot(currencySlotBounds, ProductInventory, ProductInventory[0], true)

                .AddStaticText(Lang.Get("vinconomy:gui-product"), CairoFont.WhiteSmallText(), purchaseLabel)
                .AddItemSlotGrid(ProductInventory, null, 1, new int[] { 0 }, purchaseSlotBounds);

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

        private bool OnPurchase()
        {
            return true;
        }

        private ItemStack ResolveBlockOrItem(string code, int size)
        {
            AssetLocation location = new AssetLocation(code);
            Item item = capi.World.GetItem(location);
            if (item != null)
            {
                return new ItemStack(item, size);
            }

            Block block = capi.World.GetBlock(location);
            if (block != null)
            {
                return new ItemStack(block, size);
            }

            return null;
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
