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

        DummyInventory ProductInventory;
        DummyInventory CurrencyInventory;

        public static T[] ToPrimitiveArray<T>(JArray array)
        {
            T[] array2 = new T[array.Count];
            for (int i = 0; i < array2.Length; i++)
            {
                _ = array[i];
                array2[i] = array[i].ToObject<T>();
            }

            return array2;
        }

        private static IAttribute ToAttribute(JToken token)
        {
            if (token is JValue jValue)
            {
                if (jValue.Value is int)
                {
                    return new IntAttribute((int)jValue.Value);
                }

                if (jValue.Value is long)
                {
                    return new LongAttribute((long)jValue.Value);
                }

                if (jValue.Value is float)
                {
                    return new FloatAttribute((float)jValue.Value);
                }

                if (jValue.Value is double)
                {
                    return new DoubleAttribute((double)jValue.Value);
                }

                if (jValue.Value is bool)
                {
                    return new BoolAttribute((bool)jValue.Value);
                }

                if (jValue.Value is string)
                {
                    return new StringAttribute((string)jValue.Value);
                }
            }

            if (token is JObject jObject)
            {
                TreeAttribute treeAttribute = new TreeAttribute();
                {
                    foreach (KeyValuePair<string, JToken> item in jObject)
                    {
                        treeAttribute[item.Key] = ToAttribute(item.Value);
                    }

                    return treeAttribute;
                }
            }

            if (token is JArray jArray)
            {
                if (!jArray.HasValues)
                {
                    return new TreeArrayAttribute(new TreeAttribute[0]);
                }

                if (jArray[0] is JValue jValue2)
                {
                    if (jValue2.Value is int)
                    {
                        return new IntArrayAttribute(ToPrimitiveArray<int>(jArray));
                    }

                    if (jValue2.Value is long)
                    {
                        return new LongArrayAttribute(ToPrimitiveArray<long>(jArray));
                    }

                    if (jValue2.Value is float)
                    {
                        return new FloatArrayAttribute(ToPrimitiveArray<float>(jArray));
                    }

                    if (jValue2.Value is double)
                    {
                        return new DoubleArrayAttribute(ToPrimitiveArray<double>(jArray));
                    }

                    if (jValue2.Value is bool)
                    {
                        return new BoolArrayAttribute(ToPrimitiveArray<bool>(jArray));
                    }

                    if (jValue2.Value is string)
                    {
                        return new StringArrayAttribute(ToPrimitiveArray<string>(jArray));
                    }

                    return null;
                }

                TreeAttribute[] array = new TreeAttribute[jArray.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = (TreeAttribute)ToAttribute(jArray[i]);
                }

                return new TreeArrayAttribute(array);
            }

            return null;
        }

        public GuiVinconShopCatalog(string DialogTitle, ShopCatalog catalog, ICoreClientAPI capi): base(DialogTitle, capi)
        {
            this.Catalog = catalog;
            this.DialogTitle = catalog.Name;

            List<ShopProduct> products = catalog.Products.Products;
            ProductInventory = new DummyInventory(capi, products.Count);
            CurrencyInventory = new DummyInventory(capi, products.Count);
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

                ElementBounds descInsetBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, insetWidth, insetHeight);
                ElementBounds descScrollbarBounds = descInsetBounds.RightCopy().WithFixedWidth(20);

                ElementBounds descClipBounds = descInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
                ElementBounds descContainerBounds = descInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);

                ElementBounds itemInsetBounds = descInsetBounds.BelowCopy().WithFixedSize(insetWidth, insetHeight); // ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, insetWidth, insetHeight);
                ElementBounds itemScrollbarBounds = itemInsetBounds.RightCopy().WithFixedWidth(20);

                ElementBounds itemClipBounds = itemInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
                ElementBounds itemContainerBounds = itemInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);

                ElementBounds containerRowBounds = ElementBounds.Fixed(0, 0, insetWidth, rowHeight);

                ElementBounds itemSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, insetWidth, insetHeight, 10, 5).WithFixedPosition(0,0);
                bgBounds.WithChildren(descInsetBounds, descScrollbarBounds, itemInsetBounds, itemScrollbarBounds);

                SingleComposer = capi.Gui.CreateCompo("GuiVinconShopCatalog", dialogBounds)
               .AddShadedDialogBG(bgBounds)
               .AddDialogTitleBar(this.DialogTitle, OnTitleBarCloseClicked)
               .BeginChildElements()
                   .AddInset(descInsetBounds, insetDepth)
                   .BeginClip(descClipBounds)
                        //.AddContainer(containerBounds, "scroll-content")
                        .AddRichtext(Catalog.Description != null ? Catalog.Description : "", CairoFont.WhiteDetailText(), descContainerBounds, "description")
                   .EndClip()
                   .AddVerticalScrollbar(OnNewDescriptionScrollbarValue, descScrollbarBounds, "description-scrollbar")
               .EndChildElements()

               
                .BeginChildElements()
                   .AddInset(itemInsetBounds, insetDepth)
                   .BeginClip(itemClipBounds)
                        .AddContainer(itemContainerBounds, "products")
                        .AddItemSlotGrid(ProductInventory, PacketHandler, 10, itemSlotBounds, "item-grid")
                   .EndClip()
                   .AddVerticalScrollbar(OnNewItemScrollbarValue, itemScrollbarBounds, "item-scrollbar")
               .EndChildElements();
               
                
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
