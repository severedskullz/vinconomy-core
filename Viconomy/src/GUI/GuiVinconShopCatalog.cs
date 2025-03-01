using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Viconomy.GUI
{
    public class GuiVinconShopCatalog : GuiDialogGeneric
    {
        ShopCatalog Catalog { get; set; }
        List<ShopCatalog> ShopList;
        DummyInventory ProductInventory;
        DummyInventory CurrencyInventory;

       

        public GuiVinconShopCatalog(string DialogTitle, ShopCatalog catalog, List<ShopCatalog> shopList, ICoreClientAPI capi): base(DialogTitle, capi)
        {
            this.Catalog = catalog;
            this.DialogTitle = catalog.Name;
            this.ShopList = shopList;

            List<ShopProduct> products = catalog.Products.Products;
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
            foreach (ShopProduct product in products)
            {
                ProductInventory[index].Itemstack = VinUtils.DeserializeProduct(capi, product.ProductCode, product.TotalStock, product.ProductAttributes);
                CurrencyInventory[index].Itemstack = VinUtils.DeserializeProduct(capi, product.CurrencyCode, product.CurrencyQuantity, product.CurrencyAttributes);
                index++;
            }
            
                
            

            this.Compose();
        }

        private float NoDecay(EnumTransitionType transType, ItemStack stack, float mulByConfig)
        {
            return 0;
        }
        private void Compose()
        {
            int insetWidth = 800;
            int insetHeight = 300;
            int insetDepth = 3;

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            ElementBounds settingBounds = ElementBounds.FixedSize(800, 600).WithFixedOffset(0, GuiStyle.TitleBarHeight);


            

            //IconUtil.DrawArrowRight
            try
            {
                //GuiComposer sc = SingleComposer;
                // sc.AddShadedDialogBG(bgBounds)
                //     .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked);

                ElementBounds descLabelBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 300, 20).WithFixedMargin(5, 0).WithFixedPadding(10,5);
                ElementBounds descInsetBounds = descLabelBounds.BelowCopy().WithFixedSize(insetWidth, insetHeight);
                ElementBounds descScrollbarBounds = descInsetBounds.RightCopy().WithFixedWidth(20);

                ElementBounds descClipBounds = descInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding).FixedGrow(20,0); // I dont know why "Grow" is needed here. It leaves me with 20px of missing space even if padding is 0.
                ElementBounds descContainerBounds = descInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);

                ElementBounds productLabelBounds = descInsetBounds.BelowCopy().WithFixedSize(300,20).WithFixedMargin(5,0).WithFixedPadding(10,5).WithFixedOffset(0,15);
                ElementBounds itemInsetBounds = productLabelBounds.BelowCopy().WithFixedSize(insetWidth, insetHeight); // ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, insetWidth, insetHeight);
                ElementBounds itemScrollbarBounds = itemInsetBounds.RightCopy().WithFixedWidth(20);

                ElementBounds itemClipBounds = itemInsetBounds.ForkContainingChild().FixedGrow(20, 0); // I dont know why "Grow" is needed here. It leaves me with 20px of missing space even if padding is 0.;
                ElementBounds itemContainerBounds = itemInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);

                ElementBounds itemSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, insetWidth, insetHeight, 15, (int)Math.Ceiling(Catalog.Products.Products.Count / 15.0f)).WithFixedPosition(15,0).WithParent(itemContainerBounds);
                bgBounds.WithChildren(descInsetBounds, descScrollbarBounds, itemInsetBounds, itemScrollbarBounds);

                ElementBounds backButtonBounds = null;
                if (ShopList != null)
                {
                    backButtonBounds = itemInsetBounds.BelowCopy().WithFixedOffset(0,10).WithFixedSize(100, 30);
                    bgBounds.WithChild(backButtonBounds);
                }

                SingleComposer = capi.Gui.CreateCompo("GuiVinconShopCatalog", dialogBounds)
               .AddShadedDialogBG(bgBounds)
               .AddDialogTitleBar(this.DialogTitle, OnTitleBarCloseClicked)
               .AddStaticText(Lang.Get("vinconomy:gui-owner") + Catalog.OwnerName, CairoFont.WhiteSmallishText(), descLabelBounds)
               .BeginChildElements()
                   .AddInset(descInsetBounds, insetDepth)
                   .BeginClip(descClipBounds);
                        //.AddContainer(containerBounds, "scroll-content")
                        try
                        {
                            SingleComposer.AddRichtext(Catalog.Description != null ? Catalog.Description : "", CairoFont.WhiteDetailText(), descContainerBounds, "description");
                        } catch (Exception ex)
                        {
                            SingleComposer.AddRichtext(Lang.Get("vinconomy:gui-error-tell-the-devl") + ex.Message, CairoFont.WhiteDetailText(), descContainerBounds, "description");
                        }
                SingleComposer.EndClip()
                   .AddVerticalScrollbar(OnNewDescriptionScrollbarValue, descScrollbarBounds, "description-scrollbar")
               .EndChildElements()

               .AddStaticText(Lang.Get("vinconomy:gui-available-products"), CairoFont.WhiteSmallishText(), productLabelBounds)
               .BeginChildElements()
                   .AddInset(itemInsetBounds, insetDepth)
                   .BeginClip(itemClipBounds)
                        .AddContainer(itemContainerBounds, "products")
                        .AddItemSlotGrid(ProductInventory, PacketHandler, 15, itemSlotBounds, "item-grid")
                   .EndClip()
                   .AddVerticalScrollbar(OnNewItemScrollbarValue, itemScrollbarBounds, "item-scrollbar")
               .EndChildElements();
               
                if (ShopList != null)
                {
                    SingleComposer.AddButton(Lang.Get("vinconomy:gui-back"), ReturnToShopList, backButtonBounds, EnumButtonStyle.Normal);
                }
                
                GuiElementContainer scrollArea = SingleComposer.GetContainer("products");
                scrollArea.Add(SingleComposer.GetSlotGrid("item-grid"));
                /*
                for (int i = 0; i < 10; i++)
                {
                    scrollArea.Add(new GuiElementStaticText(capi, $"- Example Row {i + 1} -", EnumTextOrientation.Center, containerRowBounds, CairoFont.WhiteSmallishText()));
                    containerRowBounds = containerRowBounds.BelowCopy();
                }*/
                
                SingleComposer.GetRichtext("description").CalcHeightAndPositions();
                SingleComposer.GetRichtext("description").Bounds.CalcWorldBounds();


                SingleComposer.Compose();

                float descScrollVisibleHeight = (float)descClipBounds.fixedHeight;
                double descScrollTotalHeight = SingleComposer.GetRichtext("description").Bounds.fixedHeight;
                SingleComposer.GetScrollbar("description-scrollbar").SetHeights(descScrollVisibleHeight, (float)descScrollTotalHeight);

                float itemScrollVisibleHeight = (float)itemClipBounds.fixedHeight;
                double itemScrollTotalHeight = SingleComposer.GetContainer("products").Bounds.fixedHeight;
                SingleComposer.GetScrollbar("item-scrollbar").SetHeights(itemScrollVisibleHeight, (float)itemScrollTotalHeight);
            } catch (Exception e)
            {
                this.capi.Logger.Debug(e.ToString());
            }

            

        }

        private bool ReturnToShopList()
        {
            GuiVinconCatalog catalog = new GuiVinconCatalog(DialogTitle, ShopList, capi);
            catalog.TryOpen();
            this.TryClose();
            return true;
        }

        private void PacketHandler(object obj)
        {
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
