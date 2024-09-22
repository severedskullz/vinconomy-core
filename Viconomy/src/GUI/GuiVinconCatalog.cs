using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Viconomy.Network;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;

namespace Viconomy.GUI
{
    public class GuiVinconCatalog : GuiDialogGeneric
    {
        private List<ShopCatalog> shopList;
        ElementBounds ContainerBounds;

        public GuiVinconCatalog(string dialogTitle, List<ShopCatalog> shopList, ICoreClientAPI api) : base(dialogTitle,api) 
        {
            this.shopList = shopList;
            Compose();
        }

        private void Compose()
        {
            int insetWidth = 800;
            int insetHeight = 800;
            int insetDepth = 3;
            try
            {
                // Auto-sized dialog at the center of the screen
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

                // Bounds of main inset for scrolling content in the GUI
                ElementBounds insetBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, insetWidth, insetHeight);
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

                        ElementBounds locationLabel = ownerLabel.BelowCopy().WithFixedSize(640, 20);
                        shopBounds.Add("locationLabel", locationLabel);

                        ElementBounds shortDescLabel = locationLabel.BelowCopy().WithFixedSize(790, 100);
                        shopBounds.Add("shortDescLabel", shortDescLabel);

                        ElementBounds openButton = shopNameLabel.RightCopy().WithFixedSize(150, 60);
                        shopBounds.Add("openButton", openButton);

                        shopBoundsList[i] = shopBounds;
                        ContainerBounds.WithChildren(shopNameLabel, ownerLabel, shortDescLabel, openButton);
                        lastEntry = shortDescLabel;
                    }
                } else
                {
                    lastEntry = new ElementBounds().WithParent(ContainerBounds).WithFixedSize(640,20);
                    ContainerBounds.WithChildren(lastEntry);
                }


                // Dialog background bounds
                ElementBounds bgBounds = ElementBounds.Fill
                    .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                    .WithSizing(ElementSizing.FitToChildren)
                    .WithChildren(insetBounds, scrollbarBounds);

                // Create the dialog
                SingleComposer = capi.Gui.CreateCompo("demoScrollGui", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar("Shop Catalog", OnTitleBarCloseClicked)
                    .BeginChildElements()
                        .AddInset(insetBounds, insetDepth)
                        .BeginClip(clipBounds);

                CairoFont font = CairoFont.WhiteSmallText();
                GuiElementContainer scrollArea = SingleComposer.GetContainer("scroll-content");
                if (shopList != null && shopList.Count > 0)
                {
                    for (int i = 0; i < shopList.Count; i++)
                    {
                        ShopCatalog catalog = shopList[i];
                        Dictionary<string, ElementBounds> shop = shopBoundsList[i];

                        //scrollArea.Add(new GuiElementStaticText(capi, "Static Text", EnumTextOrientation.Left, shop["shopNameLabel"], font));


                        SingleComposer.AddRichtext(catalog.Name, CairoFont.WhiteSmallText(), shop["shopNameLabel"]);
                        SingleComposer.AddRichtext($"Owner: {catalog.OwnerName}", CairoFont.WhiteSmallText(), shop["ownerLabel"]);

                        string location = catalog.IsWaypointBroadcasted ? $"Location: <a href=\"viewmap://{catalog.WorldX}={catalog.Y}={catalog.WorldZ}\">({catalog.X}, {catalog.Y}, {catalog.Z})</a>" : "Location: Not Broadcasted";

                        SingleComposer.AddRichtext(location, CairoFont.WhiteSmallText(), shop["locationLabel"]);
                        SingleComposer.AddRichtext(catalog.ShortDescription == null ? "" : catalog.ShortDescription, CairoFont.WhiteSmallText(), shop["shortDescLabel"]);
                        int index = i;
                        SingleComposer.AddButton("View", () => { return OpenShop(index); }, shop["openButton"]);
                    }
                } else
                {
                    SingleComposer.AddRichtext("There are currently no shops available to view in the catalog", CairoFont.WhiteSmallText(), lastEntry);
                }


                //SingleComposer.AddContainer(containerBounds, "scroll-content");
                SingleComposer.EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                .EndChildElements();


            

                // Compose the dialog
                SingleComposer.Compose();

                // After composing dialog, need to set the scrolling area heights to enable scroll behavior
                float scrollVisibleHeight = (float)clipBounds.fixedHeight;
                float scrollTotalHeight = 175 * (shopList == null ? 1 : shopList.Count);
                SingleComposer.GetScrollbar("scrollbar").SetHeights(scrollVisibleHeight, scrollTotalHeight);
            } catch (Exception ex) { 
                Console.WriteLine(ex.ToString());
            }
        }

        private bool OpenShop(int index)
        {
            ShopCatalogRequestPacket packet = new ShopCatalogRequestPacket();
            packet.IncludeShopList = true;
            packet.ShopId = shopList[index].ID;
            capi.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL).SendPacket(packet);
            this.TryClose();
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
    }
}