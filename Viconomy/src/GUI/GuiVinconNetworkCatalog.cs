using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.Network.Api;
using Viconomy.TradeNetwork.Api;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Viconomy.GUI
{
    public class GuiVinconNetworkCatalog : GuiDialogGeneric
    {
        List<SearchResult> shopList = new List<SearchResult>();
        SearchResult currentShop = null;
        ElementBounds ContainerBounds;
        BlockPos BlockPos;

        public DummyInventory ProductInventory;
        private bool isLoading;

        public GuiVinconNetworkCatalog(string dialogTitle, BlockPos pos, ICoreClientAPI capi): base(dialogTitle, capi)
        {
            BlockPos = pos;
            DialogTitle = dialogTitle;
            ProductInventory = new DummyInventory(capi, 1);
            ProductInventory.TakeLocked = true;
            ProductInventory.PutLocked = true;
            ProductInventory.OnAcquireTransitionSpeed += NoDecay;
            Compose();
        }

        private float NoDecay(EnumTransitionType transType, ItemStack stack, float mulByConfig)
        {
            return 0;
        }

        public void ComposeSearchPage(ElementBounds dialogBounds, ElementBounds bgBounds)
        {
            int insetWidth = 800;
            int insetHeight = 600;
            int insetDepth = 3;

            ElementBounds shopNameInputLabel = ElementBounds.Fixed(20, GuiStyle.TitleBarHeight + 15, 160, 25);
            ElementBounds shopNameTexbox = shopNameInputLabel.RightCopy().WithFixedSize(200, 25).WithFixedOffset(0, -5);

            ElementBounds itemNameLabel = shopNameInputLabel.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(160, 25);
            ElementBounds itemNameTexbox = itemNameLabel.RightCopy().WithFixedSize(200, 25).WithFixedOffset(0, -5);

            ElementBounds currencyNameLabel = itemNameLabel.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(160, 25);
            ElementBounds currencyNameTexbox = currencyNameLabel.RightCopy().WithFixedSize(200, 25).WithFixedOffset(0, -5);

            ElementBounds searchButton = ElementBounds.FixedSize(120, 95).FixedRightOf(shopNameTexbox).WithFixedOffset(10, GuiStyle.TitleBarHeight + 10);

            // Bounds of main inset for scrolling content in the GUI
            ElementBounds insetBounds = ElementBounds.FixedSize(insetWidth, insetHeight).FixedUnder(currencyNameLabel);
            ElementBounds scrollbarBounds = insetBounds.RightCopy().WithFixedWidth(20);

            // Create child elements bounds for within the inset
            ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ContainerBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);

            ElementBounds lastEntry = null;
            Dictionary<string, ElementBounds>[] shopBoundsList = null;

            if (shopList != null && shopList.Count > 0)
            {
                shopBoundsList = new Dictionary<string, ElementBounds>[shopList.Count];
                for (int i = 0; i < shopList.Count; i++)
                {
                    //ShopCatalog catalog = shopList[i];
                    if (lastEntry == null)
                    {
                        lastEntry = new ElementBounds().WithParent(ContainerBounds);

                    }
                    Dictionary<string, ElementBounds> shopBounds = new Dictionary<string, ElementBounds>();

                    ElementBounds shopNameLabel = lastEntry.BelowCopy().WithFixedSize(640, 20).WithFixedOffset(0, 15);
                    shopBounds.Add("shopNameLabel", shopNameLabel);

                    ElementBounds ownerLabel = shopNameLabel.BelowCopy().WithFixedSize(640, 20);
                    shopBounds.Add("ownerLabel", ownerLabel);

                    ElementBounds serverLabel = ownerLabel.BelowCopy().WithFixedSize(640, 20);
                    shopBounds.Add("serverLabel", serverLabel);

                    ElementBounds shortDescLabel = serverLabel.BelowCopy().WithFixedSize(790, 100);
                    shopBounds.Add("shortDescLabel", shortDescLabel);

                    ElementBounds openButton = shopNameLabel.RightCopy().WithFixedSize(150, 60);
                    shopBounds.Add("openButton", openButton);

                    shopBoundsList[i] = shopBounds;
                    ContainerBounds.WithChildren(shopNameLabel, ownerLabel, shortDescLabel, openButton);
                    lastEntry = shortDescLabel;
                }
            }
            else
            {
                lastEntry = new ElementBounds().WithParent(ContainerBounds).WithFixedSize(640, 20);
                ContainerBounds.WithChildren(lastEntry);
            }

            bgBounds.WithChildren(insetBounds, scrollbarBounds);
            // Create the dialog
            SingleComposer = capi.Gui.CreateCompo("GuiVinconNetworkShopCatalog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(Lang.Get("vinconomy:gui-shop-catalog"), OnTitleBarCloseClicked)
                .AddStaticText(Lang.Get("vinconomy:gui-shop-name"), CairoFont.WhiteSmallText(), shopNameInputLabel)
                .AddTextInput(shopNameTexbox, null, null, "shopName")
                .AddStaticText(Lang.Get("vinconomy:gui-product-name"), CairoFont.WhiteSmallText(), itemNameLabel)
                .AddTextInput(itemNameTexbox, null, null, "itemName")
                .AddStaticText(Lang.Get("vinconomy:gui-currency-name"), CairoFont.WhiteSmallText(), currencyNameLabel)
                .AddTextInput(currencyNameTexbox, null, null, "currencyName")
                .AddButton(Lang.Get("vinconomy:gui-search"), doSearch, searchButton)
                .BeginChildElements()
                    .AddInset(insetBounds, insetDepth)
                    .BeginClip(clipBounds);

                        CairoFont font = CairoFont.WhiteSmallText();
                        if (shopList != null && shopList.Count > 0)
                        {
                            for (int i = 0; i < shopList.Count; i++)
                            {
                                SearchResult catalog = shopList[i];
                                Dictionary<string, ElementBounds> shop = shopBoundsList[i];

                                //scrollArea.Add(new GuiElementStaticText(capi, "Static Text", EnumTextOrientation.Left, shop["shopNameLabel"], font));


                                SingleComposer.AddRichtext(catalog.shopName, CairoFont.WhiteSmallText(), shop["shopNameLabel"]);
                                SingleComposer.AddRichtext(Lang.Get("vinconomy:gui-f-owner", catalog.shopOwner), CairoFont.WhiteSmallText(), shop["ownerLabel"]);
                                SingleComposer.AddRichtext(Lang.Get("vinconomy:gui-f-server", catalog.nodeName), CairoFont.WhiteSmallText(), shop["serverLabel"]);

                                SingleComposer.AddRichtext(catalog.description == null ? Lang.Get("vinconomy:gui-no-description") : catalog.description, CairoFont.WhiteSmallText(), shop["shortDescLabel"]);
                                int index = i;
                                SingleComposer.AddButton(Lang.Get("vinconomy:gui-view"), () => { return OpenShop(index); }, shop["openButton"]);
                            }
                        }
                        else
                        {
                            SingleComposer.AddRichtext(Lang.Get("vinconomy:gui-no-shops"), CairoFont.WhiteSmallText(), lastEntry);
                        }


                        SingleComposer.EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                .EndChildElements()
                .Compose();

            // After composing dialog, need to set the scrolling area heights to enable scroll behavior
            float scrollVisibleHeight = (float)clipBounds.fixedHeight;
            float scrollTotalHeight = 175 * (shopList == null ? 1 : shopList.Count);
            SingleComposer.GetScrollbar("scrollbar").SetHeights(scrollVisibleHeight, scrollTotalHeight);
        }

        public void ComposeShopResults(ElementBounds dialogBounds,ElementBounds bgBounds)
        {
  
            int insetWidth = 800;
            int insetHeight = 300;
            int insetDepth = 3;
            isLoading = false; 

            ElementBounds settingBounds = ElementBounds.FixedSize(800, 600).WithFixedOffset(0, GuiStyle.TitleBarHeight);

            try
            {
                ElementBounds shopNameLabel = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 800, 25).WithParent(bgBounds);
                ElementBounds ownerLabel = shopNameLabel.BelowCopy().WithParent(bgBounds);
                ElementBounds serverNameLabel = ownerLabel.BelowCopy().WithParent(bgBounds);
                ElementBounds descriptionLabel = serverNameLabel.BelowCopy().WithParent(bgBounds);

                ElementBounds productLabelBounds = descriptionLabel.BelowCopy().WithFixedSize(300, 20).WithFixedOffset(0,20).WithParent(bgBounds);
                ElementBounds itemInsetBounds = productLabelBounds.BelowCopy().WithFixedSize(insetWidth, insetHeight).WithFixedOffset(0,10); // ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, insetWidth, insetHeight);
                ElementBounds itemScrollbarBounds = itemInsetBounds.RightCopy().WithFixedWidth(20);

                ElementBounds itemClipBounds = itemInsetBounds.ForkContainingChild().FixedGrow(20, 0); // I dont know why "Grow" is needed here. It leaves me with 20px of missing space even if padding is 0.;
                ContainerBounds = itemInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);


                ElementBounds itemSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, insetWidth, insetHeight, 15, (int)Math.Ceiling(ProductInventory.Count / 15.0f)).WithFixedPosition(15, 0).WithParent(ContainerBounds);
                bgBounds.WithChildren(itemInsetBounds, itemScrollbarBounds);

                ElementBounds backButtonBounds =  itemInsetBounds.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(100, 30);
                //Not sure which one I like more...
                //ElementBounds summonButtonBounds = itemInsetBounds.BelowCopy().FixedRightOf(itemInsetBounds).WithFixedOffset(-100, 0).WithFixedSize(100, 30);
                ElementBounds summonButtonBounds = backButtonBounds.RightCopy().WithFixedOffset(10, 0).WithFixedSize(100, 30);
                bgBounds.WithChildren(backButtonBounds, summonButtonBounds);


                SingleComposer = capi.Gui.CreateCompo("GuiVinconShopCatalog", dialogBounds)
               .AddShadedDialogBG(bgBounds)
               .AddDialogTitleBar(this.DialogTitle, OnTitleBarCloseClicked)
               .AddStaticText(Lang.Get("vinconomy:gui-f-shop", currentShop.shopName), CairoFont.WhiteSmallishText(), shopNameLabel)
               .AddStaticText(Lang.Get("vinconomy:gui-f-owner", currentShop.shopOwner), CairoFont.WhiteSmallishText(), ownerLabel)
               .AddStaticText(Lang.Get("vinconomy:gui-f-server", currentShop.nodeName), CairoFont.WhiteSmallishText(), serverNameLabel)
               .AddStaticText(currentShop.description == null ? Lang.Get("vinconomy:gui-no-description") : currentShop.description, CairoFont.WhiteSmallishText(), descriptionLabel)
               .AddStaticText(Lang.Get("vinconomy:gui-available-products"), CairoFont.WhiteSmallishText(), productLabelBounds)
               .BeginChildElements()
                    .AddInset(itemInsetBounds, insetDepth);
                         
                    if (isLoading)
                    {
                        ElementBounds loadingBounds = new ElementBounds().WithParent(ContainerBounds);
                        SingleComposer.AddRichtext(Lang.Get("vinconomy:gui-loading"), CairoFont.WhiteSmallText(), itemSlotBounds);
                    } else
                    {
                        SingleComposer.BeginClip(itemClipBounds)
                            .AddContainer(ContainerBounds, "products")
                            .AddItemSlotGrid(ProductInventory, PacketHandler, 15, itemSlotBounds, "item-grid")
                        .EndClip()
                        .AddVerticalScrollbar(OnNewItemScrollbarValue, itemScrollbarBounds, "item-scrollbar");
                    }

                SingleComposer.EndChildElements();

                SingleComposer.AddButton(Lang.Get("vinconomy:gui-back"), ReturnToShopList, backButtonBounds, EnumButtonStyle.Normal);
                SingleComposer.AddButton(Lang.Get("vinconomy:gui-summon-trader"), SummonTrader, summonButtonBounds, EnumButtonStyle.Normal);

                if (!isLoading)
                {
                    GuiElementContainer scrollArea = SingleComposer.GetContainer("products");
                    scrollArea.Add(SingleComposer.GetSlotGrid("item-grid"));
                }

                SingleComposer.Compose();
                if (!isLoading)
                {
                    float itemScrollVisibleHeight = (float)itemClipBounds.fixedHeight;
                    double itemScrollTotalHeight = SingleComposer.GetContainer("products").Bounds.fixedHeight;
                    SingleComposer.GetScrollbar("item-scrollbar").SetHeights(itemScrollVisibleHeight, (float)itemScrollTotalHeight);
                }
            }
            catch (Exception e)
            {
                this.capi.Logger.Debug(e.ToString());
            }

        }

        private bool SummonTrader()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);

                writer.Write(currentShop.nodeId);
                writer.Write(currentShop.shopId);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockPos, VinConstants.SET_TRADER, data);
            return true;
        }

        private bool ReturnToShopList()
        {
            currentShop = null;
            Compose();
            return true;
        }

        private void Compose()
        {
            try
            {
                // Auto-sized dialog at the center of the screen
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

                // Dialog background bounds
                ElementBounds bgBounds = ElementBounds.Fill
                    .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                    .WithSizing(ElementSizing.FitToChildren);


               if (currentShop != null)
               {
                    ComposeShopResults(dialogBounds, bgBounds);
               } else {
                    ComposeSearchPage(dialogBounds, bgBounds);
               }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void PacketHandler(object obj)
        {
            // Do nothing! These are fake items.
        }

        private void OnNewItemScrollbarValue(float obj)
        {
  
        }

        public void PopulateResultsFromServer(List<SearchResult> results)
        {
            shopList = results;
            this.Compose();
        }

        private bool doSearch()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);

                writer.Write(SingleComposer.GetTextInput("shopName").GetText());
                writer.Write(SingleComposer.GetTextInput("itemName").GetText());
                writer.Write(SingleComposer.GetTextInput("currencyName").GetText());
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockPos, VinConstants.SEARCH_SHOPS, data);
            return true;
        }

        private bool OpenShop(int index)
        {
            currentShop = shopList[index];
            isLoading = true;
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);

                writer.Write(currentShop.nodeId);
                writer.Write(currentShop.shopId);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockPos, VinConstants.GET_PRODUCTS, data);
            Compose();
            return true;
        }

        private void OnNewScrollbarValue(float value)
        {
            //ElementBounds bounds = SingleComposer.GetContainer("scroll-content").Bounds;
            ElementBounds bounds = ContainerBounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

        public void LoadShopResults(TradeNetworkShop result)
        {
            isLoading = false;
            ProductInventory = new DummyInventory(capi, result.Products.Count);
            ProductInventory.TakeLocked = true;
            ProductInventory.PutLocked = true;
            ProductInventory.OnAcquireTransitionSpeed += NoDecay;

            int index = 0;
            foreach (ShopProduct product in result.Products)
            {
                ProductInventory[index].Itemstack = VinUtils.DeserializeProduct(capi, product.ProductCode, product.TotalStock, product.ProductAttributes);
                index++;
            }
            Compose();

        }
    }
}
