using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Viconomy.Inventory.Impl;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.GUI
{
    public class GuiViconRegister : GuiDialogBlockEntity
    {
        private const int TAB_WIDTH = 200;
        private const int TAB_HEIGHT = 400;
        private int tabIndex;
        private bool isLoaded;

        private ShopRegistration shop;

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

        public GuiViconRegister(ShopRegistration shopRegistration, ViconRegisterInventory Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi) : base(shopRegistration.Name, Inventory, BlockEntityPosition, capi)
        {
            if (base.IsDuplicate)
            {
                return;
            }
            shop = shopRegistration;
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
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-inventory"), Active = false },
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-permissions"), Active = false },
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-configuration"), Active = false },
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-waypoint"), Active = false }
            };
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
                ViconRegisterInventory regInv = (ViconRegisterInventory)Inventory;

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);//.WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0.0);

                ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.DialogToScreenPadding);
                bgBounds.BothSizing = ElementSizing.FitToChildren;

                ElementBounds currencyLabelBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, 200, 25);
                ElementBounds currencySlotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, (int)Math.Ceiling(regInv.CurrencySlotCount / 10.0)).FixedUnder(currencyLabelBounds);

                ElementBounds couponLabelBounds = ElementBounds.FixedSize(200, 25).WithFixedOffset(0,20).FixedUnder(currencySlotGrid);
                ElementBounds couponSlotGrid = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, (int)Math.Ceiling(regInv.CouponSlotCount / 10.0)).FixedUnder(couponLabelBounds);

                bgBounds.WithChildren(currencyLabelBounds, currencySlotGrid, couponLabelBounds, couponSlotGrid);

                //IconUtil.DrawArrowRight
                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds);

                


                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddVerticalTabs(tabs, tabBounds, onTabChanged)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddStaticText(Lang.Get("vinconomy:gui-currency"), CairoFont.WhiteSmallishText(), currencyLabelBounds)
                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 10, GenerateArrayOf(1, regInv.CurrencySlotCount), currencySlotGrid, "currency")
                    .AddStaticText(Lang.Get("vinconomy:gui-coupons"), CairoFont.WhiteSmallishText(), couponLabelBounds)
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

                //IconUtil.DrawArrowRight
                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds);

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddVerticalTabs(tabs, tabBounds, onTabChanged)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddStaticText(Lang.Get("vinconomy:gui-trade-pass"), CairoFont.WhiteSmallishText(), tradePassLabelBounds)
                    .AddItemSlotGrid(Inventory, new Action<object>(this.SendInvPacket), 10, new int[] { 0 }, tradePassBounds, "coupons")
                    .AddStaticText(Lang.Get("vinconomy:gui-stall-access"), CairoFont.WhiteSmallishText(), accessStallsLabelBounds)
                    .AddSwitch(EnableStallAccess, accessStallsBounds)
                    .AddStaticText(Lang.Get("vinconomy:gui-shop-access"), CairoFont.WhiteSmallishText(), accessInputLabelBounds)
                    .AddTextInput(accessInputBounds, OnAccessInputTextChanged)
                    .AddButton("Add", OnAddAccess, accessAddButtonBounds)
                    .AddInset(accessInsetBounds, 3)
                        .BeginClip(accessClipBounds)
                            .AddContainer(accessContainerBounds, "container").BeginChildElements();
                                try
                                {
                                    for (int i = 0; i < 10; i++)
                                    {
                                        ElementBounds nameLabelBounds = ElementBounds.Fixed(10, 5+(40 * i), 300, 25);
                                        // Static Text doesnt bind to the parent for some reason? Why? Dunno. Dynamic text works fine though???
                                        //SingleComposer.AddStaticText($"Player {i}", CairoFont.WhiteMediumText(), nameLabelBounds);
                                        SingleComposer.AddDynamicText($"Player {i}", CairoFont.WhiteSmallishText(), nameLabelBounds);
                                        
                                        ElementBounds removeButtonBounds = ElementBounds.Fixed(315, 40 * i, 150, 25);
                                        SingleComposer.AddButton("Remove", RemovePlayerPermissions, removeButtonBounds, EnumButtonStyle.Small);
                            
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SingleComposer.AddRichtext(Lang.Get("vinconomy:gui-error-tell-the-dev") + ex.Message, CairoFont.WhiteDetailText(), accessContainerBounds, "description");
                                }
                    SingleComposer.EndChildElements().EndClip()
                .AddVerticalScrollbar(OnNewShopAccessScrollbarValue, accessScrollbarBounds, "access-scrollbar");

                UpdateSelectedTab();
                
                SingleComposer.Compose();
                UpdateAccessScrollbar();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

            }
        }

        private void UpdateAccessScrollbar()
        {
            float descScrollVisibleHeight = (float)accessClipBounds.fixedHeight;
            double descScrollTotalHeight = 800;//SingleComposer.GetContainer("container").Bounds.fixedHeight;
            SingleComposer.GetScrollbar("access-scrollbar").SetHeights(descScrollVisibleHeight, (float)descScrollTotalHeight);
        }

        private bool OnAddAccess()
        {
            return true;
        }

        private void OnAccessInputTextChanged(string obj)
        {
            
        }

        private bool RemovePlayerPermissions()
        {
            return true;
        }

        private void EnableStallAccess(bool obj)
        {
            
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

                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds);

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddVerticalTabs(tabs, tabBounds, onTabChanged)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddStaticText(Lang.Get("vinconomy:gui-name"), CairoFont.WhiteSmallText(), shopNameLabelBounds)
                    .AddTextInput(shopNameInputBounds, null, CairoFont.TextInput(), "shopName")

                    .AddStaticText(Lang.Get("vinconomy:gui-short-description"), CairoFont.WhiteSmallText(), shortDescriptionLabelBounds)
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
            ElementBounds bounds = SingleComposer.GetContainer("container").Bounds;
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

            ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds);

           
            try
            {
                SingleComposer = capi.Gui.CreateCompo("ViconRegister", dialogBounds);
                SingleComposer.AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddVerticalTabs(tabs, tabBounds, onTabChanged)
                    .AddStaticText(Lang.Get("vinconomy:gui-waypoint-visible"), CairoFont.WhiteSmallText(), waypointLabel)
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
            this.capi.Gui.PlaySound(this.CloseSound, true, 1f);
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



    }
}
