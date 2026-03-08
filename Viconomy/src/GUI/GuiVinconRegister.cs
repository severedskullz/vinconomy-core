using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vinconomy.Inventory.Impl;
using Vinconomy.Registry;
using Vinconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vinconomy.GUI
{
    public class GuiVinconRegister : GuiDialogBlockEntity
    {
        private const int TAB_WIDTH = 200;
        private const int TAB_HEIGHT = 400;
        private int tabIndex;
        private bool isLoaded;
        private bool isOwner;

        private ShopRegistration shop;
        private List<ShopAccess> shopAccess = new List<ShopAccess>();

        private VinconomyCoreSystem modSystem;

        GuiTab[] tabs;

        //Waypoint Page Variables
        private string[] icons;
        private int[] colors;
        private bool isVisible;
        private string currentIcon = "genericZShop";
        private int currentColor = 0;

        // Config Page Variables
        ElementBounds descClipBounds;
        ElementBounds shortDescClipBounds;

        // Permissions Page Variables
        ElementBounds accessClipBounds;


        public GuiVinconRegister(ShopRegistration shopRegistration, VinconRegisterInventory Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi) 
            : base(shopRegistration.Name, Inventory, BlockEntityPosition, capi)
        {
            if (base.IsDuplicate)
            {
                return;
            }
            shop = shopRegistration;
            shopAccess = shop.Permissions.Values.ToList();

            isOwner = shop.Owner == capi.World.Player.PlayerUID;

            modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();
            icons = modSystem.ShopMapLayer.WaypointIcons.Keys.ToArray<string>();
            colors = modSystem.ShopMapLayer.WaypointColors.ToArray();

            capi.World.Player.InventoryManager.OpenInventory(Inventory);

            tabs = GetTabs();
           
        }

        public GuiTab[] GetTabs()
        {
            List<GuiTab> tabs = new List<GuiTab>
            {
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-inventory"), Active = false }
            };

            if (isOwner)
            {
                tabs.Add(new GuiTab() { Name = Lang.Get("vinconomy:tabname-permissions"), Active = false });
                tabs.Add(new GuiTab() { Name = Lang.Get("vinconomy:tabname-configuration"), Active = false });
                tabs.Add(new GuiTab() { Name = Lang.Get("vinconomy:tabname-waypoint"), Active = false });
            }

            tabIndex = Math.Min(tabs.Count-1, tabIndex);
            tabs[tabIndex].Active = true;
            return tabs.ToArray();
        }

        public int[] GenerateArrayOf(int startingNumber, int num)
        {
            List<int> array = new List<int>();
            for (int i = startingNumber; i < startingNumber+ num; i++)
            {
                array.Add(i);
            }
            return array.ToArray();
        }

        public void ComposeInventoryPage()
        {
            try
            {
                VinconRegisterInventory regInv = (VinconRegisterInventory)Inventory;

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                ElementBounds currencyLabelBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 200, 25);
                ElementBounds currencySlotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, (int)Math.Ceiling(regInv.CurrencySlotCount / 10.0)).FixedUnder(currencyLabelBounds);

                ElementBounds couponLabelBounds = ElementBounds.FixedSize(200, 25).WithFixedOffset(0,20).FixedUnder(currencySlotGrid);
                ElementBounds couponSlotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, (int)Math.Ceiling(regInv.CouponSlotCount / 10.0)).FixedUnder(couponLabelBounds);

                bgBounds.WithChildren(currencyLabelBounds, currencySlotGrid, couponLabelBounds, couponSlotGrid);

                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);


                CairoFont hoverText = CairoFont.WhiteDetailText();

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddVerticalTabs(tabs, tabBounds, onTabChanged)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddStaticText(Lang.Get("vinconomy:gui-currency"), CairoFont.WhiteSmallishText(), currencyLabelBounds)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-currency"), hoverText, 500, currencyLabelBounds)

                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 10, GenerateArrayOf(1, regInv.CurrencySlotCount), currencySlotGrid, "currency")
                    .AddStaticText(Lang.Get("vinconomy:gui-coupons"), CairoFont.WhiteSmallishText(), couponLabelBounds)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-coupons"), hoverText, 500, couponLabelBounds)
                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 10, GenerateArrayOf(regInv.CurrencySlotCount + 1, regInv.CouponSlotCount), couponSlotGrid,"coupons");


                UpdateSelectedTab();
                SingleComposer.Compose();


            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
        }

        public void UpdateSelectedTab()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Active = tabIndex == i;
            }
        }

        public void ComposePermissionsPage()
        {
            try
            {
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                
                ElementBounds tradePassBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, GuiStyle.TitleBarHeight, 1, 1);
                ElementBounds tradePassLabelBounds = ElementBounds.Fixed(60, GuiStyle.TitleBarHeight+12, 440, 25);



                ElementBounds accessInputLabelBounds = ElementBounds.FixedSize(500, 25).FixedUnder(tradePassBounds);
                ElementBounds accessInputBounds = ElementBounds.FixedSize(400, 40).FixedUnder(accessInputLabelBounds);
                ElementBounds accessAddButtonBounds = accessInputBounds.RightCopy().WithFixedSize(100,40);//ElementBounds.FixedSize(30, 30).FixedRightOf(accessInputBounds);

                ElementBounds accessInsetBounds = ElementBounds.FixedSize(480, 200).FixedUnder(accessInputBounds);
                accessClipBounds = accessInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding).FixedGrow(0, 0); // I dont know why "Grow" is needed here. It leaves me with 20px of missing space even if padding is 0.
                ElementBounds accessContainerBounds = accessInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
                ElementBounds accessScrollbarBounds = accessInsetBounds.RightCopy().WithFixedWidth(20);


                ElementBounds accessStallsBounds = ElementBounds.FixedSize(30, 30).FixedUnder(accessInsetBounds).WithFixedOffset(0, 10);
                ElementBounds accessStallsLabelBounds = accessStallsBounds.RightCopy().WithFixedSize(200, 30).WithFixedOffset(10, 0);

                bgBounds.WithChildren(tradePassLabelBounds, tradePassBounds, accessStallsLabelBounds, accessStallsBounds,
                    accessInsetBounds, accessScrollbarBounds, accessInputLabelBounds, accessInputBounds, accessAddButtonBounds);

                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                CairoFont hoverText = CairoFont.WhiteDetailText();

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddVerticalTabs(tabs, tabBounds, onTabChanged)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddStaticText(Lang.Get("vinconomy:gui-trade-pass"), CairoFont.WhiteSmallishText(), tradePassLabelBounds)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-trade-pass"), hoverText, 500, tradePassLabelBounds)
                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 10, new int[] { 0 }, tradePassBounds, "coupons")
                    .AddStaticText(Lang.Get("vinconomy:gui-shop-access"), CairoFont.WhiteSmallishText(), accessInputLabelBounds)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-shop-access"), hoverText, 500, accessInputLabelBounds)
                    .AddTextInput(accessInputBounds, null, null, "playerName")
                    .AddButton("Add", OnAddAccess, accessAddButtonBounds)
                    .AddInset(accessInsetBounds, 3)
                        .BeginClip(accessClipBounds)
                            .AddStaticText("", CairoFont.SmallTextInput(), accessContainerBounds, "container")
                            .BeginChildElements();
                                try
                                {
                                    int i = 0;
                                    foreach (ShopAccess access in shopAccess)
                                    {
                                        ElementBounds nameLabelBounds = ElementBounds.Fixed(10, 5+(40 * i), 300, 25);
                                        // Static Text doesnt bind to the parent for some reason? Why? Dunno. Dynamic text works fine though???
                                        //SingleComposer.AddStaticText(access.PlayerName, CairoFont.WhiteMediumText(), nameLabelBounds);
                                        SingleComposer.AddDynamicText(access.PlayerName, CairoFont.WhiteSmallishText(), nameLabelBounds);
                                        
                                        ElementBounds removeButtonBounds = ElementBounds.Fixed(315, 40 * i, 150, 25);
                                        SingleComposer.AddButton("Remove", RemovePlayerPermissions(access), removeButtonBounds);
                                        i++;
                                    }
                    
                                }
                                catch (Exception ex)
                                {
                                    SingleComposer.AddRichtext(Lang.Get("vinconomy:gui-error-tell-the-dev") + ex.Message, CairoFont.WhiteDetailText(), accessContainerBounds, "description");
                                }
                            SingleComposer.EndChildElements()
                        .EndClip()
                .AddVerticalScrollbar(OnNewShopAccessScrollbarValue, accessScrollbarBounds, "access-scrollbar")

                .AddSwitch(EnableStallAccess, accessStallsBounds, "stallAccess")
                .AddStaticText(Lang.Get("vinconomy:gui-stall-access"), CairoFont.WhiteSmallishText(), accessStallsLabelBounds)
                .AddHoverText(Lang.Get("vinconomy:tooltip-stall-access"), hoverText, 500, accessStallsLabelBounds);

                UpdateSelectedTab();
                SingleComposer.GetTextInput("playerName").SetPlaceHolderText("Player Name");
                SingleComposer.GetSwitch("stallAccess").SetValue(shop.StallPermissions);
                SingleComposer.Compose();
                UpdateAccessScrollbar();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
        }

        private ActionConsumable RemovePlayerPermissions(ShopAccess access)
        {
            return () =>
            {
                RemovePlayerPermission(access.PlayerUID);
                return true;
            };
        }

        private void UpdateAccessScrollbar()
        {
            float descScrollVisibleHeight = (float)accessClipBounds.fixedHeight;
            double descScrollTotalHeight = 40 * shopAccess.Count;
            SingleComposer.GetScrollbar("access-scrollbar").SetHeights(descScrollVisibleHeight, (float)descScrollTotalHeight);
        }

        public void AddPlayerPermission(string playerName)
        {
           
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(playerName);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.ADD_PLAYER_PERMISSION, data);
        }

        public void RemovePlayerPermission(string playerUid)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(playerUid);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.REMOVE_PLAYER_PERMISSION, data);
        }

        private bool OnAddAccess()
        {
            GuiElementTextInput input = SingleComposer.GetTextInput("playerName");
            AddPlayerPermission(input.GetText());
            return true;
        }

        private void EnableStallAccess(bool obj)
        {
            GuiElementSwitch input = SingleComposer.GetSwitch("stallAccess");
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(input.On);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_STALL_PERMISSION, data);
        }

        public void ComposeText()
        {
            try
            {
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
                ElementBounds bounds = ElementBounds.FixedSize(500, 300);


                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;
                bgBounds.WithChildren(bounds);


            }
            catch (Exception e) { 
                Console.WriteLine(e);
            }
        }


        public void ComposeConfigPage()
        {
            try
            {
                isLoaded = false;
                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                ElementBounds shopNameLabelBounds = ElementBounds.Fixed(0, 35, 500, 25);
                ElementBounds shopNameInputBounds = ElementBounds.FixedSize(500, 25).FixedUnder(shopNameLabelBounds);

                ElementBounds shortDescriptionLabelBounds = shopNameInputBounds.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(500, 25);

                ElementBounds shortDescInsetBounds = ElementBounds.FixedSize(480, 200).FixedUnder(shortDescriptionLabelBounds);
                shortDescClipBounds = shortDescInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding).FixedGrow(0, 0); // I dont know why "Grow" is needed here. It leaves me with 20px of missing space even if padding is 0.
                ElementBounds shortDescContainerBounds = shortDescInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
                ElementBounds shortDescScrollbarBounds = shortDescInsetBounds.RightCopy().WithFixedWidth(20);



               // ElementBounds shortDescriptionBounds = shortDescriptionLabelBounds.BelowCopy().WithFixedSize(500, 200);
                ElementBounds shortDescriptionSizeLabelBounds = shortDescInsetBounds.BelowCopy().WithFixedSize(500, 25);

                ElementBounds descriptionLabelBounds = shortDescriptionSizeLabelBounds.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(500, 25);
                //ElementBounds descriptionBounds = descriptionLabelBounds.BelowCopy().WithFixedSize(500, 200);
                ElementBounds descriptionInsetBounds = ElementBounds.FixedSize(480, 200).FixedUnder(descriptionLabelBounds);

                descClipBounds = descriptionInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding).FixedGrow(0, 0); // I dont know why "Grow" is needed here. It leaves me with 20px of missing space even if padding is 0.
                ElementBounds descriptionContainerBounds = descriptionInsetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
                ElementBounds descriptionScrollbarBounds = descriptionInsetBounds.RightCopy().WithFixedWidth(20);

                ElementBounds descriptionSizeLabelBounds = descriptionInsetBounds.BelowCopy().WithFixedSize(500, 25);

                ElementBounds webhookLabelBounds = descriptionSizeLabelBounds.BelowCopy().WithFixedOffset(0, 10).WithFixedSize(500, 25);
                ElementBounds webhookBounds = webhookLabelBounds.BelowCopy().WithFixedOffset(0, 0).WithFixedSize(500, 25);

                ElementBounds saveButtonBounds = webhookBounds.BelowCopy().WithFixedSize(60, 20).WithFixedOffset(0, 10).WithAlignment(EnumDialogArea.RightTop);

                bgBounds.WithChildren(shopNameLabelBounds, shopNameInputBounds, shortDescInsetBounds, descriptionInsetBounds,
                    shortDescScrollbarBounds, descriptionScrollbarBounds,
                    shortDescriptionLabelBounds,  shortDescriptionSizeLabelBounds,
                    descriptionLabelBounds, descriptionSizeLabelBounds,
                    webhookLabelBounds, webhookBounds, saveButtonBounds);

                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                CairoFont hoverText = CairoFont.WhiteDetailText();

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddVerticalTabs(tabs, tabBounds, onTabChanged)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddStaticText(Lang.Get("vinconomy:gui-name"), CairoFont.WhiteSmallText(), shopNameLabelBounds)
                    .AddTextInput(shopNameInputBounds, null, CairoFont.TextInput(), "shopName")

                    .AddStaticText(Lang.Get("vinconomy:gui-short-description"), CairoFont.WhiteSmallText(), shortDescriptionLabelBounds)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-short-description"), hoverText, 500, shortDescriptionLabelBounds)

                    .AddInset(shortDescInsetBounds, 3)
                    .BeginClip(shortDescClipBounds);
                    try
                    {
                        SingleComposer.AddTextArea(shortDescContainerBounds, UpdateShortDesc, CairoFont.TextInput(), "shortDescription");
                    }
                    catch (Exception ex)
                    {
                        SingleComposer.AddRichtext(Lang.Get("vinconomy:gui-error-tell-the-dev") + ex.Message, CairoFont.WhiteDetailText(), shortDescContainerBounds, "description");
                    }
                    SingleComposer.EndClip()
                    .AddVerticalScrollbar(OnNewShortDescScrollbarValue, shortDescScrollbarBounds, "shortdescription-scrollbar")
                    .AddDynamicText("0 / 250", CairoFont.WhiteSmallishText(), shortDescriptionSizeLabelBounds, "shortDescriptionLength")

                    .AddStaticText(Lang.Get("vinconomy:gui-description"), CairoFont.WhiteSmallText(), descriptionLabelBounds)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-description"), hoverText, 500, descriptionLabelBounds)
                    //.AddTextArea(descriptionBounds, UpdateLongCount, CairoFont.TextInput(), "description")
                    .AddInset(descriptionInsetBounds, 3)
                    .BeginClip(descClipBounds);
                    try
                    {
                        SingleComposer.AddTextArea(descriptionContainerBounds, UpdateLongDesc, CairoFont.TextInput(), "description");
                    }
                    catch (Exception ex)
                    {
                        SingleComposer.AddRichtext("There was an error in the store's description. Exception " + ex.Message, CairoFont.WhiteDetailText(), descriptionContainerBounds, "description");
                    }
                    SingleComposer.EndClip()
                    .AddVerticalScrollbar(OnNewDescriptionScrollbarValue, descriptionScrollbarBounds, "description-scrollbar")
                    .AddDynamicText("0 / 2500", CairoFont.WhiteSmallishText(), descriptionSizeLabelBounds, "descriptionLength")

                    .AddStaticText(Lang.Get("vinconomy:gui-webhook"), CairoFont.WhiteSmallText(), webhookLabelBounds)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-webhook"), hoverText, 500, webhookLabelBounds)
                    .AddTextInput(webhookBounds, null, CairoFont.TextInput(), "webhook")

                    .AddButton(Lang.Get("vinconomy:gui-save"), OnSaveShopConfigPressed, saveButtonBounds, EnumButtonStyle.Small, "save");

                UpdateSelectedTab();
                SingleComposer.Compose();

                int shortLength = shop.ShortDescription == null ? 0 : shop.ShortDescription.Length;
                int longLength = shop.Description == null ? 0 : shop.Description.Length;
                SingleComposer.GetTextInput("shopName").SetValue(shop.Name);

                GuiElementTextArea shortDesc = SingleComposer.GetTextArea("shortDescription");
                shortDesc.SetValue(shop.ShortDescription);

                GuiElementTextArea longDesc = SingleComposer.GetTextArea("description");
                longDesc.SetValue(shop.Description);

                SingleComposer.GetTextInput("webhook").SetValue(shop.WebHook);
                SingleComposer.GetDynamicText("shortDescriptionLength").SetNewText($"{shortLength} / 250");
                SingleComposer.GetDynamicText("descriptionLength").SetNewText($"{longLength} / 1024");

                UpdateShortDescScrollbar();
                UpdateDescScrollbar();

                isLoaded = true;

            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
        }


        private void UpdateShortDescScrollbar()
        {
            float descScrollVisibleHeight = (float)descClipBounds.fixedHeight;
            double descScrollTotalHeight = SingleComposer.GetTextArea("shortDescription").Bounds.fixedHeight;
            SingleComposer.GetScrollbar("shortdescription-scrollbar").SetHeights(descScrollVisibleHeight, (float)descScrollTotalHeight);
        }

        private void UpdateDescScrollbar()
        {
            float descScrollVisibleHeight = (float)descClipBounds.fixedHeight;
            double descScrollTotalHeight = SingleComposer.GetTextArea("description").Bounds.fixedHeight;
            SingleComposer.GetScrollbar("description-scrollbar").SetHeights(descScrollVisibleHeight, (float)descScrollTotalHeight);
        }

        private void OnNewDescriptionScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetTextArea("description").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void OnNewShopAccessScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetStaticText("container").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }


        private void OnNewShortDescScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetTextArea("shortDescription").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void UpdateShortDesc(string obj)
        {
            if (!isLoaded) return;
            GuiElementDynamicText desc = SingleComposer.GetDynamicText("shortDescriptionLength");

            desc.SetNewText($"{obj.Length} / 250");

            if (obj.Length > 250)
            {
                GuiElementTextArea area = SingleComposer.GetTextArea("shortDescription");
                area.SetValue(obj.Substring(0, 250));
            }

            if (obj.Length >= 250 ) {
                desc.Font.Color[0] = 255;
                desc.Font.Color[1] = 0;
                desc.Font.Color[2] = 0;
            } 
            else
            {
                desc.Font.Color[0] = 255;
                desc.Font.Color[1] = 255;
                desc.Font.Color[2] = 255;
            }
            desc.RecomposeText();
            UpdateShortDescScrollbar();
        }

        private void UpdateLongDesc(string obj)
        {
            if (!isLoaded) return;
            GuiElementDynamicText desc = SingleComposer.GetDynamicText("descriptionLength");
            if (obj.Length > 1024)
            {
                GuiElementTextArea area = SingleComposer.GetTextArea("description");
                area.SetValue(obj.Substring(0, 1024));
            }

            desc.SetNewText($"{obj.Length} / 1024");
            if (obj.Length >= 1024)
            {
                desc.Font.Color[0] = 255;
                desc.Font.Color[1] = 0;
                desc.Font.Color[2] = 0;
            }
            else
            {
                desc.Font.Color[0] = 255;
                desc.Font.Color[1] = 255;
                desc.Font.Color[2] = 255;
            }
            desc.RecomposeText();
            UpdateDescScrollbar();
        }

        private void Compose()
        {
            switch (tabIndex)
            {
                case 0:
                    ComposeInventoryPage();
                    break;
                case 1:
                    ComposePermissionsPage();
                    break;
                case 2:
                    ComposeConfigPage();
                    break;
                case 3:
                    ComposeWaypointPage();
                    break;
                default:
                    break;
            }

        }

        private void ComposeWaypointPage()  
        {
            ElementBounds waypointLabel = ElementBounds.Fixed(0.0, 28.0, 300.0, 25.0);
            ElementBounds waypointVisible = waypointLabel.RightCopy(0, -5);

            ElementBounds colorLabelBounds = ElementBounds.FixedSize(500, 25.0).FixedUnder(waypointLabel).WithFixedOffset(0, 10);
            ElementBounds colorRow = ElementBounds.FixedSize(25, 25).FixedUnder(colorLabelBounds);

            ElementBounds iconLabelBounds = ElementBounds.Fixed(0.0, 220.0, 500, 25.0);
            ElementBounds iconRow = ElementBounds.FixedSize(25, 25.0).FixedUnder(iconLabelBounds);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(new ElementBounds[]
            {
                waypointLabel,
                waypointVisible,
                colorRow,
                iconRow,
                colorLabelBounds,
                iconLabelBounds
            });
            //ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);

           
            try
            {
                SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds);
                SingleComposer.AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddVerticalTabs(tabs, tabBounds, onTabChanged)
                    .AddStaticText(Lang.Get("vinconomy:gui-waypoint-visible"), CairoFont.WhiteSmallText(), waypointLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-waypoint-visible"), CairoFont.WhiteDetailText(), 500, waypointLabel)
                    .AddSwitch(new Action<bool>(OnToggleWaypointVisible), waypointVisible, "isvisible")
                    .AddStaticText(Lang.Get("vinconomy:gui-color"), CairoFont.WhiteSmallText(), colorLabelBounds)
                    .AddColorListPicker(colors, onToggleColor, colorRow, 500, "colorpicker")
                    .AddStaticText(Lang.Get("vinconomy:gui-icon"), CairoFont.WhiteSmallText(), iconLabelBounds)
                    .AddIconListPicker(icons, onToggleIcon, iconRow, 500, "iconpicker");
                    UpdateSelectedTab();
                SingleComposer.Compose();

                if (shop.IsWaypointBroadcasted)
                {
                    //GuiComposerHelpers.GetButton(base.SingleComposer, "saveButton").Enabled = false;
                    GuiComposerHelpers.ColorListPickerSetValue(base.SingleComposer, "colorpicker", colors.IndexOf(shop.WaypointColor));
                    this.currentColor = shop.WaypointColor;
                    GuiComposerHelpers.IconListPickerSetValue(base.SingleComposer, "iconpicker", icons.IndexOf(shop.WaypointIcon));
                    this.currentIcon = shop.WaypointIcon;
                    base.SingleComposer.GetSwitch("isvisible").On = true;
                    this.isVisible = true;
                }
                else
                {
                    GuiComposerHelpers.ColorListPickerSetValue(base.SingleComposer, "colorpicker", 0);
                    this.currentColor = colors[0];
                    GuiComposerHelpers.IconListPickerSetValue(base.SingleComposer, "iconpicker", 0);
                    this.currentIcon = icons[0];
                }

            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private void onTabChanged(int id, GuiTab tab)
        {
            tabIndex = id;
            Compose();
        }

        private bool OnSaveShopConfigPressed()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(SingleComposer.GetTextInput("shopName").GetText());
                writer.Write(SingleComposer.GetTextArea("description").GetText());
                writer.Write(SingleComposer.GetTextArea("shortDescription").GetText());
                writer.Write(SingleComposer.GetTextInput("webhook").GetText());
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SET_SHOP_NAME, data);
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

        public override bool TryOpen()
        {
            this.Compose();
            return base.TryOpen();
        }


        public override void OnGuiClosed()
        {
            if (this.Inventory != null)
            {
                this.Inventory.Close(this.capi.World.Player);
                this.capi.World.Player.InventoryManager.CloseInventory(this.Inventory);
            }
            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.CLOSE_GUI, null);
            this.capi.Gui.PlaySound(this.CloseSound);
        }

        private void OnToggleWaypointVisible(bool visible)
        {
            this.isVisible = visible;
            SendWaypointUpdate();
        }

        private void onToggleIcon(int index)
        {
            this.currentIcon = icons[index];
            SendWaypointUpdate();
        }

        private void onToggleColor(int index)
        {
            this.currentColor = colors[index];
            SendWaypointUpdate();
        }

        private void SendWaypointUpdate()
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(isVisible);
                writer.Write(currentIcon);
                writer.Write(currentColor);
                data = ms.ToArray();
            }

            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.SET_WAYPOINT, data);
        }

        public void UpdateShopPermissions(List<ShopAccess> access, bool stallAccess)
        {
            // Originally, it would have had been as simple as replacing shop.Permissions with the new list and setting the checkbox and then calling Compose() like I do on every other one of my UIs
            // HOWEVER, Simply calling Compose() again gives me a generic "Index out of bounds" on the container in some sort of OnMouseDown() call due to it trying to unfocus elements that have been cleared.
            // To make matters worse, it works fine until I click again after Compose() is called and only THEN does it throw an error. Somehow the old SingleComposer selected index persists across
            // creation of a new composer and as soon as I click it tries to unfocuse the old out of scope composer.
            // I tried clearing the elements before calling Compose(), setting it explicitly to null, manually unfocusing the container, and even iterating through the elements and unfocusing them before clearing them.

            // Grab the old container element, manually unfocus, clear elements, and try to reuse it rather than just recreating the UI.
            // Somehow, this works, but trying to throw away the whole GuiComposer and make it from scratch each time doesn't - fucking rediculous.

            // Now, even if I manually recreate the list on the fly without a recompose, removing any elements from the list *ALSO* triggers the same error.
            // Instead of using a "Remove" button to remove it from the list, last ditch resort is radio buttons that persist after turning them off.
            // That way they cant reduce the size of the list, and therefore the index out of bounds error cannot happen - STILL didnt work.

            // Took multiple UI redesign and over 5 hours of fruitless debugging and trial and error.
            // Nothing works - error is as vague as it possibly can be, and no graceful error handling. Fuck you, Tyron - this UI framework is *MISERABLE* to work with. >:(

            // In the end, after all that, the hack was to use an invisible Static Text as the parent for the scrollbar to modify the Y position for the clip bounds. Fuck you very much, Tyron.

            //shop.Permissions = access;
            shop.StallPermissions = stallAccess;
            shopAccess = access;

            SingleComposer.GetTextInput("playerName").SetValue("");

            Compose();
            UpdateAccessScrollbar();



        }
    }
}
