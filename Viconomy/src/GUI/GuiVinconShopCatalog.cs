using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

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
            CurrencyInventory = new DummyInventory(capi, products.Count);
            CurrencyInventory.TakeLocked = true;
            CurrencyInventory.PutLocked = true;
            int index = 0;

            //Add our Product and Currency to each inventory. Catch JSON errors on attributes, cuz Quotes in descriptions got me once already
            foreach (ShopProduct product in products)
            {
                
                ItemStack productStack = ResolveBlockOrItem(product.ProductCode, product.TotalStock);
                try
                {
                    if (product.ProductAttributes != null)
                    {
                        TreeAttribute attr = new TreeAttribute();
                        attr.FromBytes(product.ProductAttributes);
                        //JsonObject productAttr = JsonObject.FromJson(product.ProductAttributes);
                        productStack.Attributes = attr;//(ITreeAttribute)ToAttribute(productAttr.Token);
                    }
                } catch (Exception ex) { }
                ProductInventory[index].Itemstack = productStack;

                ItemStack currencyStack = ResolveBlockOrItem(product.CurrencyCode, product.CurrencyAmount);
                if (product.CurrencyAttributes != null)
                {
                    TreeAttribute attr = new TreeAttribute();
                    attr.FromBytes(product.CurrencyAttributes);
                    //JsonObject currencyAttr = JsonObject.FromJson(product.CurrencyAttributes);
                    currencyStack.Attributes = attr; // (ITreeAttribute)currencyAttr.ToAttribute();
                }
                CurrencyInventory[index].Itemstack = productStack;
                index++;
            }
            
                
            

            this.Compose();
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

        private void Compose()
        {
            int insetWidth = 800;
            int insetHeight = 300;
            int insetDepth = 3;
            int rowHeight = 35;
            int rowCount = 40;

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

                ElementBounds containerRowBounds = ElementBounds.Fixed(0, 0, insetWidth, rowHeight);

                ElementBounds itemSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, insetWidth, insetHeight, 15, (int)Math.Ceiling(Catalog.Products.Products.Count / 15.0f)).WithFixedPosition(15,0).WithParent(itemContainerBounds);
                bgBounds.WithChildren(descInsetBounds, descScrollbarBounds, itemInsetBounds, itemScrollbarBounds);

                ElementBounds backButtonBounds = null;
                if (ShopList != null)
                {
                    backButtonBounds = itemInsetBounds.BelowCopy().WithFixedSize(100, 60);
                    bgBounds.WithChild(backButtonBounds);
                }

                SingleComposer = capi.Gui.CreateCompo("GuiVinconShopCatalog", dialogBounds)
               .AddShadedDialogBG(bgBounds)
               .AddDialogTitleBar(this.DialogTitle, OnTitleBarCloseClicked)
               .AddStaticText($"Owner: {Catalog.OwnerName}",CairoFont.WhiteSmallishText(),descLabelBounds)
               .BeginChildElements()
                   .AddInset(descInsetBounds, insetDepth)
                   .BeginClip(descClipBounds)
                        //.AddContainer(containerBounds, "scroll-content")
                        .AddRichtext(Catalog.Description != null ? Catalog.Description : "", CairoFont.WhiteDetailText(), descContainerBounds, "description")
                   .EndClip()
                   .AddVerticalScrollbar(OnNewDescriptionScrollbarValue, descScrollbarBounds, "description-scrollbar")
               .EndChildElements()

               .AddStaticText($"Available Products for sale:", CairoFont.WhiteSmallishText(), productLabelBounds)
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
                    SingleComposer.AddButton("Back", ReturnToShopList, backButtonBounds, EnumButtonStyle.Normal);
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
