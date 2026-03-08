using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Viconomy.Registry;
using Vinconomy.BlockEntities;
using Vinconomy.Inventory.Slots;
using Vinconomy.Registry;
using Vinconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vinconomy.GUI
{

    /***
        Quite literally one of the most functionally complex UI's I've done so far. For anyone looking for examples on what NOT to do, this is it.
        A lot of it stems from the sheer lack of dynamic components and the lack of any sort of state management (e.g. ability to show/hide parts of the UI dynamically)
        where I need to hide the configuration panels for the Currency and Products unless we actually *load* one - and since dynamic elements cant be referenced by keys,
        I cannot use an ElementContainer to throw all of them in and simply do a Recompose() call, because then I would not be able to get the values from said elements.
        Perhaps there is actually a way, and I simply am not seeing it due to the awful decompilation results and the fact that I am only getting interfaces shown to me and
        not actual implementations... A problem for another day.

        Another issue is inheretence does not seem to work at all for Protobuff - I cant make ProductDefinition extend CurrencyDefinition, for example.
    */
    public class GuiVinconAdminRegister : GuiDialogBlockEntity
    {
        private const int TAB_WIDTH = 200;
        private const int TAB_HEIGHT = 400;
        int insetWidth = 200;
        int insetHeight = 600;
        int insetDepth = 5;
        int settingWidth = 500;
        int settingHeight = 675;

        private int tabIndex;
        private bool isLoaded;
        private bool isOwner;

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


        private ElementBounds SearchContainerBounds;

        private DummyInventory UIInventory;
        private DummyInventory ResultInventory;
        private ElementBounds SettingContainerBounds;

        List<EntryResultDefinition> ProductsCache;
        List<EntryResultDefinition> CurrencyCache;
        List<EntryResultDefinition> FilteredCache;

        ProductDefinition ProductDefinition;
        CurrencyDefinition CurrencyDefinition;

        CairoFont DefaultFont = CairoFont.WhiteSmallText();
        CairoFont HeaderFont = CairoFont.WhiteSmallishText();
        CairoFont HoverFont = CairoFont.WhiteSmallText().WithColor(GuiStyle.ActiveButtonTextColor);

        public GuiVinconAdminRegister(ShopRegistration shopRegistration, InventoryGeneric Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi) 
            : base(shopRegistration.Name, Inventory, BlockEntityPosition, capi)
        {
            if (base.IsDuplicate)
            {
                return;
            }
            shop = shopRegistration;

            isOwner = shop.Owner == capi.World.Player.PlayerUID;

            modSystem = capi.ModLoader.GetModSystem<VinconomyCoreSystem>();
            icons = modSystem.ShopMapLayer.WaypointIcons.Keys.ToArray<string>();
            colors = modSystem.ShopMapLayer.WaypointColors.ToArray();

            capi.World.Player.InventoryManager.OpenInventory(Inventory);

            tabs = GetTabs();

            UIInventory = new DummyInventory(capi, 3);
            UIInventory[0] = new VinconCurrencySlot(UIInventory);
            UIInventory.SlotModified += OnSearchItemSelected;
            UIInventory[1] = new VinconCurrencySlot(UIInventory);
            UIInventory.SlotModified += OnSearchItemSelected;
            UIInventory[2] = new VinconCurrencySlot(UIInventory);
            UIInventory.SlotModified += OnSearchItemSelected;

        }



        public GuiTab[] GetTabs()
        {
            List<GuiTab> tabs = new List<GuiTab>
            {
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-inventory-currency"), Active = false },
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-inventory-products"), Active = false },
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-configuration"), Active = false },
                new GuiTab() { Name = Lang.Get("vinconomy:tabname-waypoint"), Active = false }
            };

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

        private void Compose()
        {
            isLoaded = false;
            switch (tabIndex)
            {
                case 0:
                    ComposeCurrencyPage();
                    break;
                case 1:
                    ComposeProductPage();
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
            isLoaded = true;
        }

        public void ComposeCurrencyPage()
        {

            try
            {

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);


                ElementBounds searchPageBounds = ElementBounds.FixedSize(insetWidth + 50, settingHeight).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                ElementBounds searchPageContent = searchPageBounds.ForkContainingChild().WithFixedPadding(10);


                ElementBounds searchItem = ElementStdBounds.Slot(0, 0);
                ElementBounds searchItemLabel = ElementBounds.FixedSize(150, 20).FixedRightOf(searchItem).WithFixedOffset(5, 15);

                // Bounds of main inset for scrolling content in the GUI
                ElementBounds insetBounds = ElementBounds.FixedSize(insetWidth, insetHeight).FixedUnder(searchItem, 10);
                ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds);

                // Create child elements bounds for within the inset
                ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
                SearchContainerBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);

                ElementBounds settingsPageBounds = ElementBounds.FixedSize(settingWidth, settingHeight).FixedRightOf(searchPageBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                SettingContainerBounds = settingsPageBounds.ForkContainingChild().WithFixedPadding(10);

                ElementBounds productAmountLabel = ElementBounds.FixedSize(settingWidth / 2, 25).WithFixedOffset(0, 10);
                ElementBounds productAmount = ElementStdBounds.Slot(0, 0).FixedUnder(productAmountLabel);

                ElementBounds supplyCountLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(productAmount, 30);
                ElementBounds supplyCountInput = ElementBounds.FixedSize(settingWidth / 4, 30).FixedUnder(supplyCountLabel, 5);
                ElementBounds addItemBounds = ElementBounds.FixedSize(settingWidth / 5, 20).FixedUnder(supplyCountLabel).FixedRightOf(supplyCountInput, 5);
                ElementBounds removeItemBounds = ElementBounds.FixedSize(settingWidth / 5, 20).FixedUnder(supplyCountLabel).FixedRightOf(addItemBounds, 5);
                ElementBounds setItemBounds = ElementBounds.FixedSize(settingWidth / 5, 20).FixedUnder(supplyCountLabel).FixedRightOf(removeItemBounds, 5);

                ElementBounds injectionLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(addItemBounds, 30);

                ElementBounds injectionIntervalLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(injectionLabel, 10);
                ElementBounds injectionIntervalDropDown = ElementBounds.FixedSize(125, 30).FixedUnder(injectionIntervalLabel);
                ElementBounds injectionIntervalInput = ElementBounds.FixedSize(125, 30).FixedUnder(injectionIntervalLabel).FixedRightOf(injectionIntervalDropDown, 10);
                ElementBounds injectionIntervalSpanDropDown = ElementBounds.FixedSize(125, 30).FixedUnder(injectionIntervalLabel).FixedRightOf(injectionIntervalInput, 10);

                ElementBounds injectionActionlLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(injectionIntervalInput, 15);
                ElementBounds injectionAmountDropDown = ElementBounds.FixedSize(260, 30).FixedUnder(injectionActionlLabel);
                ElementBounds injectionAmountInput = ElementBounds.FixedSize(125, 30).FixedUnder(injectionActionlLabel).FixedRightOf(injectionAmountDropDown, 10);

                // Dialog background bounds
                ElementBounds bgBounds = ElementBounds.Fill
                    .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                    .WithSizing(ElementSizing.FitToChildren)
                    .WithChildren(searchPageBounds, settingsPageBounds);

                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);




                // Create the dialog
                SingleComposer = capi.Gui.CreateCompo("GuiVinconShopCatalog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddVerticalTabs(tabs, tabBounds, OnTabChanged)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                .BeginChildElements(searchPageContent)
                    .AddItemSlotGrid(UIInventory, null, 1, [0], searchItem)
                    .AddStaticText(Lang.Get("vinconomy:gui-search"), DefaultFont, searchItemLabel)
                    .AddInset(insetBounds, insetDepth)
                    .BeginClip(clipBounds)
                        .AddContainer(SearchContainerBounds, "searchResults")
                    .EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                .EndChildElements()
                .AddInset(settingsPageBounds)
                .AddIf(CurrencyDefinition != null)
                    .BeginChildElements(SettingContainerBounds)

                        .AddStaticText(Lang.Get("vinconomy:gui-currency"), DefaultFont, productAmountLabel)
                        .AddItemSlotGrid(UIInventory, null, 1, [1], productAmount, "Product")

                        .AddDynamicText("", HeaderFont, supplyCountLabel, "SupplyAmount")
                        .AddNumberInput(supplyCountInput, null, null, "SupplyModifyAmount")
                        .AddButton(Lang.Get("vinconomy:gui-add"), () => { return OnClick("Add"); }, addItemBounds)
                        .AddButton(Lang.Get("vinconomy:gui-remove"), () => { return OnClick("Remove"); }, removeItemBounds)
                        .AddButton(Lang.Get("vinconomy:gui-set"), () => { return OnClick("Set"); }, setItemBounds)

                        .AddStaticText(Lang.Get("vinconomy:gui-injection-settings"), HeaderFont, injectionLabel)
                        .AddStaticText(Lang.Get("vinconomy:gui-interval-duration"), DefaultFont, injectionIntervalLabel)
                        .AddDropDown(["0", "1", "2"], [Lang.Get("vinconomy:gui-interval-manual"), Lang.Get("vinconomy:gui-interval-ingame"), Lang.Get("vinconomy:gui-interval-realtime")], 0, (code, selected) => { OnDropdownChanged(code, selected, "IntervalType"); }, injectionIntervalDropDown, "IntervalType")
                        .AddNumberInput(injectionIntervalInput, (val) => { OnNumberChanged(val, "IntervalDuration"); }, null, "IntervalDuration")
                        .AddDropDown(["0", "1", "2"], [Lang.Get("vinconomy:gui-period-day"), Lang.Get("vinconomy:gui-period-month"), Lang.Get("vinconomy:gui-period-year")], 0, (code, selected) => { OnDropdownChanged(code, selected, "IntervalPeriod"); }, injectionIntervalSpanDropDown, "IntervalPeriod")
                        .AddStaticText(Lang.Get("vinconomy:gui-interval-action"), DefaultFont, injectionActionlLabel)
                        .AddDropDown(["0", "1", "2"], [Lang.Get("vinconomy:gui-action-set-specific"), Lang.Get("vinconomy:gui-action-add-additional"), Lang.Get("vinconomy:gui-action-meet-minimum")], 0, (code, selected) => { OnDropdownChanged(code, selected, "IntervalAction"); }, injectionAmountDropDown, "IntervalAction")
                        .AddNumberInput(injectionAmountInput, (val) => { OnNumberChanged(val, "IntervalActionValue"); }, null, "IntervalActionValue")
                    .EndChildElements()
                .EndIf();


                // Compose the dialog
                UpdateSelectedTab();
                SingleComposer.Compose();
                SingleComposer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)0);

                if (CurrencyDefinition != null)
                {
                    UIInventory[1].Itemstack = VinUtils.DeserializeProduct(capi, CurrencyDefinition.CurrencyCode, 1, CurrencyDefinition.CurrencyAttributes);
                    SingleComposer.GetNumberInput("SupplyModifyAmount").SetValue(0);
                    SingleComposer.GetDynamicText("SupplyAmount").SetNewText(Lang.Get("vinconomy:gui-f-supply", [CurrencyDefinition.Supply]));

                    SingleComposer.GetDropDown("IntervalType").SetSelectedIndex(CurrencyDefinition.IntervalType);
                    SingleComposer.GetNumberInput("IntervalDuration").SetValue(CurrencyDefinition.IntervalDuration);
                    SingleComposer.GetDropDown("IntervalPeriod").SetSelectedIndex(CurrencyDefinition.IntervalPeriod);
                    SingleComposer.GetDropDown("IntervalAction").SetSelectedIndex(CurrencyDefinition.IntervalAction);
                    SingleComposer.GetNumberInput("IntervalActionValue").SetValue(CurrencyDefinition.IntervalActionValue);

                }


                if (CurrencyCache == null)
                {
                    SearchItem(null, BEVinconAdminRegister.TYPE_CURRENCY);
                    SetLoadingResults();
                }
                else
                {
                    PopulateResultsList(CurrencyCache);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void ComposeProductPage()
        {


            try
            {

                ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

                ElementBounds searchPageBounds = ElementBounds.FixedSize(insetWidth + 50, settingHeight).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                ElementBounds searchPageContent = searchPageBounds.ForkContainingChild().WithFixedPadding(10);

                ElementBounds searchItem = ElementStdBounds.Slot(0, 0);
                ElementBounds searchItemLabel = ElementBounds.FixedSize(150, 20).FixedRightOf(searchItem).WithFixedOffset(5, 15);

                // Bounds of main inset for scrolling content in the GUI
                ElementBounds insetBounds = ElementBounds.FixedSize(insetWidth, insetHeight).FixedUnder(searchItem, 10);
                ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds);

                // Create child elements bounds for within the inset
                ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
                SearchContainerBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);

                ElementBounds settingsPageBounds = ElementBounds.FixedSize(settingWidth, settingHeight).FixedRightOf(searchPageBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);
                ElementBounds settingContent = settingsPageBounds.ForkContainingChild().WithFixedPadding(10);

                ElementBounds productAmountLabel = ElementBounds.FixedSize(settingWidth / 2, 25).WithFixedOffset(0, 10);
                ElementBounds productAmount = ElementStdBounds.Slot(0, 0).FixedUnder(productAmountLabel);
                ElementBounds productAmountInput = ElementBounds.FixedSize(75, 30).FixedUnder(productAmountLabel, 10).FixedRightOf(productAmount, 10);

                ElementBounds basePriceLabel = ElementBounds.FixedSize(settingWidth / 2, 25).FixedRightOf(productAmountLabel).WithFixedOffset(0, 10);
                ElementBounds basePriceAmount = ElementStdBounds.Slot(0, 0).FixedUnder(basePriceLabel).FixedRightOf(productAmountLabel);
                ElementBounds basePriceAmountInput = ElementBounds.FixedSize(75, 30).FixedUnder(basePriceLabel, 10).FixedRightOf(basePriceAmount, 10);

                ElementBounds supplyCountLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(basePriceAmount, 30);
                ElementBounds supplyCountInput = ElementBounds.FixedSize(settingWidth / 4, 30).FixedUnder(supplyCountLabel, 5);
                ElementBounds addItemBounds = ElementBounds.FixedSize(settingWidth / 5, 20).FixedUnder(supplyCountLabel).FixedRightOf(supplyCountInput, 5);
                ElementBounds removeItemBounds = ElementBounds.FixedSize(settingWidth / 5, 20).FixedUnder(supplyCountLabel).FixedRightOf(addItemBounds, 5);
                ElementBounds setItemBounds = ElementBounds.FixedSize(settingWidth / 5, 20).FixedUnder(supplyCountLabel).FixedRightOf(removeItemBounds, 5);

                ElementBounds saleContributionCheckbox = ElementBounds.FixedSize(30, 30).FixedUnder(supplyCountInput, 5);
                ElementBounds saleContributionLabel = ElementBounds.FixedSize(250, 30).FixedUnder(supplyCountInput, 10).FixedRightOf(saleContributionCheckbox, 5);

                ElementBounds injectionLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(saleContributionCheckbox, 30);

                ElementBounds injectionIntervalLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(injectionLabel, 10);
                ElementBounds injectionIntervalDropDown = ElementBounds.FixedSize(125, 30).FixedUnder(injectionIntervalLabel);
                ElementBounds injectionIntervalInput = ElementBounds.FixedSize(125, 30).FixedUnder(injectionIntervalLabel).FixedRightOf(injectionIntervalDropDown, 10);
                ElementBounds injectionIntervalSpanDropDown = ElementBounds.FixedSize(125, 30).FixedUnder(injectionIntervalLabel).FixedRightOf(injectionIntervalInput, 10);

                ElementBounds injectionActionlLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(injectionIntervalInput, 15);
                ElementBounds injectionAmountDropDown = ElementBounds.FixedSize(260, 30).FixedUnder(injectionActionlLabel);
                ElementBounds injectionAmountInput = ElementBounds.FixedSize(125, 30).FixedUnder(injectionActionlLabel).FixedRightOf(injectionAmountDropDown, 10);

                ElementBounds supplyLabel = ElementBounds.FixedSize(settingWidth, 30).FixedUnder(injectionAmountInput, 30);

                ElementBounds supplyThresholdLabel = ElementBounds.FixedSize(settingWidth / 3, 30).FixedUnder(supplyLabel, 10);
                ElementBounds supplyThresholdInput = ElementBounds.FixedSize(125, 30).FixedUnder(supplyThresholdLabel);

                ElementBounds priceLowSupplyLabel = ElementBounds.FixedSize(settingWidth / 3, 30).FixedRightOf(supplyThresholdLabel).FixedUnder(supplyLabel, 10);
                ElementBounds priceLowSupplyInput = ElementBounds.FixedSize(125, 30).FixedUnder(priceLowSupplyLabel).FixedRightOf(supplyThresholdLabel);

                ElementBounds priceHighSupplyLabel = ElementBounds.FixedSize(settingWidth / 3, 30).FixedRightOf(priceLowSupplyLabel).FixedUnder(supplyLabel, 10);
                ElementBounds priceHighSupplyInput = ElementBounds.FixedSize(125, 30).FixedUnder(priceHighSupplyLabel).FixedRightOf(priceLowSupplyLabel);

                ElementBounds idealSupplyLabel = ElementBounds.FixedSize(settingWidth / 2, 30).FixedUnder(priceHighSupplyInput, 10);
                ElementBounds idealSupplyInput = ElementBounds.FixedSize(125, 30).FixedUnder(idealSupplyLabel);
                ElementBounds unlimitedSupplyCheckbox = ElementBounds.FixedSize(30, 30).FixedUnder(idealSupplyInput, 5);
                ElementBounds unlimitedSupplyLabel = ElementBounds.FixedSize(150, 30).FixedUnder(idealSupplyInput, 10).FixedRightOf(unlimitedSupplyCheckbox, 5);

                ElementBounds maxDemandLabel = ElementBounds.FixedSize(settingWidth / 2, 30).FixedUnder(priceHighSupplyInput, 10).FixedRightOf(idealSupplyLabel);
                ElementBounds maxDemandInput = ElementBounds.FixedSize(125, 30).FixedUnder(maxDemandLabel).FixedRightOf(idealSupplyLabel);
                ElementBounds unlimitedDemandCheckbox = ElementBounds.FixedSize(30, 30).FixedUnder(maxDemandInput, 5).FixedRightOf(idealSupplyLabel);
                ElementBounds unlimitedDemandLabel = ElementBounds.FixedSize(150, 30).FixedUnder(maxDemandInput, 10).FixedRightOf(unlimitedDemandCheckbox, 5);


                // Dialog background bounds
                ElementBounds bgBounds = ElementBounds.Fill
                    .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                    .WithSizing(ElementSizing.FitToChildren)
                    .WithChildren(searchPageBounds, settingsPageBounds);

                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                // Create the dialog
                SingleComposer = capi.Gui.CreateCompo("GuiVinconShopCatalog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddVerticalTabs(tabs, tabBounds, OnTabChanged)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                .BeginChildElements(searchPageContent)
                    .AddItemSlotGrid(UIInventory, null, 1, [0], searchItem)
                    .AddStaticText(Lang.Get("vinconomy:gui-search"), DefaultFont, searchItemLabel)
                    .AddInset(insetBounds, insetDepth)
                    .BeginClip(clipBounds)
                        .AddContainer(SearchContainerBounds, "searchResults")
                    .EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                .EndChildElements()
                .AddInset(settingsPageBounds)
                .AddIf(ProductDefinition != null)
                    .BeginChildElements(SettingContainerBounds)
                        .AddStaticText(Lang.Get("vinconomy:gui-product"), DefaultFont, productAmountLabel)
                        .AddItemSlotGrid(UIInventory, null, 1, [1], productAmount, "Product")
                        .AddNumberInput(productAmountInput, (val) => { OnNumberChanged(val, "ProductQuantity"); }, null, "ProductQuantity")

                        .AddStaticText(Lang.Get("vinconomy:gui-price"), DefaultFont, basePriceLabel)
                        .AddItemSlotGrid(UIInventory, null, 1, [2], basePriceAmount, "BasePrice")
                        .AddNumberInput(basePriceAmountInput, (val) => { OnNumberChanged(val, "BasePriceAmount"); }, null, "BasePriceAmount")

                        .AddDynamicText("", HeaderFont, supplyCountLabel, "SupplyAmount")
                        .AddNumberInput(supplyCountInput, (val) => { OnNumberChanged(val, "SupplyModifyAmount"); }, null, "SupplyModifyAmount")
                        .AddButton(Lang.Get("vinconomy:gui-add"), () => { return OnClick("Add");  }, addItemBounds)
                        .AddButton(Lang.Get("vinconomy:gui-remove"), () => { return OnClick("Remove"); }, removeItemBounds)
                        .AddButton(Lang.Get("vinconomy:gui-set"), () => { return OnClick("Set"); }, setItemBounds)

                        .AddSwitch((val) => { OnSwitchToggled(val, "SalesContribute"); }, saleContributionCheckbox, "SalesContribute")
                        .AddStaticText(Lang.Get("vinconomy:gui-sales-contribute"), DefaultFont, saleContributionLabel)


                        .AddStaticText(Lang.Get("vinconomy:gui-injection-settings"), HeaderFont, injectionLabel)
                        .AddStaticText(Lang.Get("vinconomy:gui-interval-duration"), DefaultFont, injectionIntervalLabel)
                        .AddDropDown(["0", "1", "2"], [Lang.Get("vinconomy:gui-interval-manual"), Lang.Get("vinconomy:gui-interval-ingame"), Lang.Get("vinconomy:gui-interval-realtime")], 0, (code, selected) => { OnDropdownChanged(code, selected, "IntervalType"); }, injectionIntervalDropDown, "IntervalType")
                        .AddNumberInput(injectionIntervalInput, (val) => { OnNumberChanged(val, "IntervalDuration"); }, null, "IntervalDuration")
                        .AddDropDown(["0", "1", "2"], [Lang.Get("vinconomy:gui-period-day"), Lang.Get("vinconomy:gui-period-month"), Lang.Get("vinconomy:gui-period-year")], 0, (code, selected) => { OnDropdownChanged(code, selected, "IntervalPeriod"); }, injectionIntervalSpanDropDown, "IntervalPeriod")
                        .AddStaticText(Lang.Get("vinconomy:gui-interval-action"), DefaultFont, injectionActionlLabel)
                        .AddDropDown(["0", "1", "2"], [Lang.Get("vinconomy:gui-action-set-specific"), Lang.Get("vinconomy:gui-action-add-additional"), Lang.Get("vinconomy:gui-action-meet-minimum")], 0, (code, selected) => { OnDropdownChanged(code, selected, "IntervalAction"); }, injectionAmountDropDown, "IntervalAction")
                        .AddNumberInput(injectionAmountInput, (val) => { OnNumberChanged(val, "IntervalActionValue"); }, null, "IntervalActionValue")

                        .AddStaticText(Lang.Get("vinconomy:gui-supply-settings"), HeaderFont, supplyLabel)
                        .AddStaticText(Lang.Get("vinconomy:gui-supply-threshold"), DefaultFont, supplyThresholdLabel)
                        .AddNumberInput(supplyThresholdInput, (val) => { OnNumberChanged(val, "SupplyThreshold"); }, null, "SupplyThreshold")
                        .AddStaticText(Lang.Get("vinconomy:gui-price-low"), DefaultFont, priceLowSupplyLabel)
                        .AddNumberInput(priceLowSupplyInput, (val) => { OnNumberChanged(val, "LowPrice"); }, null, "LowPrice")
                        .AddStaticText(Lang.Get("vinconomy:gui-price-high"), DefaultFont, priceHighSupplyLabel)
                        .AddNumberInput(priceHighSupplyInput, (val) => { OnNumberChanged(val, "HighPrice"); }, null, "HighPrice")
                        .AddStaticText(Lang.Get("vinconomy:gui-supply-ideal"), DefaultFont, idealSupplyLabel)
                        .AddNumberInput(idealSupplyInput, (val) => { OnNumberChanged(val, "IdealSupply"); }, null, "IdealSupply")
                        .AddSwitch((val) => { OnSwitchToggled(val, "UnlimitedSupply"); }, unlimitedSupplyCheckbox, "UnlimitedSupply")
                        .AddStaticText(Lang.Get("vinconomy:gui-supply-unlimited"), DefaultFont, unlimitedSupplyLabel)
                        .AddStaticText(Lang.Get("vinconomy:gui-demand-max"), DefaultFont, maxDemandLabel)
                        .AddNumberInput(maxDemandInput, (val) => { OnNumberChanged(val, "MaxSupply"); }, null, "MaxSupply")
                        .AddSwitch((val) => { OnSwitchToggled(val, "UnlimitedDemand"); }, unlimitedDemandCheckbox, "UnlimitedDemand")
                        .AddStaticText(Lang.Get("vinconomy:gui-demand-unlimited"), DefaultFont, unlimitedDemandLabel)
                    .EndChildElements()
                .EndIf();

                if (ProductDefinition != null)
                {
                    UIInventory[1].Itemstack = VinUtils.DeserializeProduct(capi, ProductDefinition.ProductCode, ProductDefinition.ProductQuantity, ProductDefinition.CurrencyAttributes);
                    SingleComposer.GetNumberInput("ProductQuantity").SetValue(ProductDefinition.ProductQuantity);

                    UIInventory[2].Itemstack = VinUtils.DeserializeProduct(capi, ProductDefinition.CurrencyCode, ProductDefinition.CurrencyQuantity, ProductDefinition.CurrencyAttributes);
                    SingleComposer.GetNumberInput("BasePriceAmount").SetValue(ProductDefinition.CurrencyQuantity);

                    SingleComposer.GetDynamicText("SupplyAmount").SetNewText(Lang.Get("vinconomy:gui-f-supply", [ProductDefinition.Supply]));
                    SingleComposer.GetNumberInput("SupplyModifyAmount").SetValue(0);
                    SingleComposer.GetSwitch("SalesContribute").On = ProductDefinition.SalesContribute;
                    SingleComposer.GetDropDown("IntervalType").SetSelectedIndex(ProductDefinition.IntervalType);
                    SingleComposer.GetNumberInput("IntervalDuration").SetValue(ProductDefinition.IntervalDuration);
                    SingleComposer.GetDropDown("IntervalPeriod").SetSelectedIndex(ProductDefinition.IntervalPeriod);
                    SingleComposer.GetDropDown("IntervalAction").SetSelectedIndex(ProductDefinition.IntervalAction);
                    SingleComposer.GetNumberInput("IntervalActionValue").SetValue(ProductDefinition.IntervalActionValue);


                    SingleComposer.GetNumberInput("SupplyThreshold").SetValue(ProductDefinition.SupplyThreshold);
                    SingleComposer.GetNumberInput("LowPrice").SetValue(ProductDefinition.CurrencyLowQuantity);
                    SingleComposer.GetNumberInput("HighPrice").SetValue(ProductDefinition.CurrencyHighQuantity);
                    SingleComposer.GetNumberInput("IdealSupply").SetValue(ProductDefinition.IdealSupply);
                    SingleComposer.GetNumberInput("MaxSupply").SetValue(ProductDefinition.MaxSupply);
                    SingleComposer.GetSwitch("UnlimitedSupply").On = ProductDefinition.UnlimitedSupply;
                    SingleComposer.GetSwitch("UnlimitedDemand").On = ProductDefinition.UnlimitedDemand;

                }

                // Compose the dialog
                UpdateSelectedTab();
                SingleComposer.Compose();
                SingleComposer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)0);

                if (ProductsCache == null)
                {
                    SearchItem(null, BEVinconAdminRegister.TYPE_PRODUCT);
                    SetLoadingResults();
                } else
                {
                    PopulateResultsList(ProductsCache);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
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
                    shortDescriptionLabelBounds, shortDescriptionSizeLabelBounds,
                    descriptionLabelBounds, descriptionSizeLabelBounds,
                    webhookLabelBounds, webhookBounds, saveButtonBounds);

                ElementBounds tabBounds = ElementBounds.FixedSize(TAB_WIDTH, TAB_HEIGHT).FixedLeftOf(bgBounds).WithFixedOffset(0, GuiStyle.TitleBarHeight);

                CairoFont hoverText = CairoFont.WhiteDetailText();

                // Lastly, create the dialog
                SingleComposer = capi.Gui.CreateCompo("ViconAdminRegister" + Inventory.InventoryID, dialogBounds)
                    .AddShadedDialogBG(bgBounds)
                    .AddVerticalTabs(tabs, tabBounds, OnTabChanged)
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
                SingleComposer = capi.Gui.CreateCompo("ViconAdminRegister" + Inventory.InventoryID, dialogBounds);
                SingleComposer.AddShadedDialogBG(bgBounds)
                    .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                    .AddVerticalTabs(tabs, tabBounds, OnTabChanged)
                    .AddStaticText(Lang.Get("vinconomy:gui-waypoint-visible"), CairoFont.WhiteSmallText(), waypointLabel)
                    .AddHoverText(Lang.Get("vinconomy:tooltip-waypoint-visible"), CairoFont.WhiteDetailText(), 500, waypointLabel)
                    .AddSwitch(new Action<bool>(OnToggleWaypointVisible), waypointVisible, "isvisible")
                    .AddStaticText(Lang.Get("vinconomy:gui-color"), CairoFont.WhiteSmallText(), colorLabelBounds)
                    .AddColorListPicker(colors, OnToggleColor, colorRow, 500, "colorpicker")
                    .AddStaticText(Lang.Get("vinconomy:gui-icon"), CairoFont.WhiteSmallText(), iconLabelBounds)
                    .AddIconListPicker(icons, OnToggleIcon, iconRow, 500, "iconpicker");
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

        private bool OnClick(string key)
        {

            int val = (int) SingleComposer.GetNumberInput("SupplyModifyAmount").GetValue();
            switch (key)
            {
                case "Add":
                    if (CurrencyDefinition != null)
                        CurrencyDefinition.Supply += val;
                    else
                        ProductDefinition.Supply += val;
                    break;
                case "Remove":
                    if (CurrencyDefinition != null)
                        CurrencyDefinition.Supply -= val;
                    else
                        ProductDefinition.Supply -= val;
                    break;
                case "Set":
                    if (CurrencyDefinition != null)
                        CurrencyDefinition.Supply = val;
                    else
                        ProductDefinition.Supply = val;
                    break;
                default:
                    break;

            }
            PersistChanges();

            return true;
        }

        private void OnDropdownChanged(string code, bool selected, string key)
        {
            if (!isLoaded)
                return;

            int val = Int32.Parse(code);
            switch (key)
            {
                case "IntervalType":
                    if (CurrencyDefinition != null)
                        CurrencyDefinition.IntervalType = val;
                    else
                        ProductDefinition.IntervalType = val;

                    break;
                case "IntervalPeriod":
                    if (CurrencyDefinition != null)
                        CurrencyDefinition.IntervalPeriod = val;
                    else
                        ProductDefinition.IntervalPeriod = val;
                    break;
                case "IntervalAction":
                    if (CurrencyDefinition != null)
                        CurrencyDefinition.IntervalAction = val;
                    else
                        ProductDefinition.IntervalAction = val;
                    break;
                default:
                    break;

            }

            PersistChanges();

        }

        private void OnSwitchToggled(bool val, string key)
        {
            if (!isLoaded)
                return;

            switch (key)
            {
                case "SalesContribute":
                    ProductDefinition.SalesContribute = val;
                    break;
                case "UnlimitedSupply":
                    ProductDefinition.UnlimitedSupply = val;
                    break;
                case "UnlimitedDemand":
                    ProductDefinition.UnlimitedDemand = val;
                    break;
                default:
                    break;

            }

            PersistChanges();
        }

        private void OnNumberChanged(string val, string key)
        {
            if (!isLoaded)
                return;

            int num = Int32.Parse(val);
            switch(key)
            {

                case "IntervalDuration":
                    if (CurrencyDefinition != null)
                        CurrencyDefinition.IntervalDuration = num;
                    else
                        ProductDefinition.IntervalDuration = num;
                    break;
                case "IntervalActionValue":
                    if (CurrencyDefinition != null)
                        CurrencyDefinition.IntervalActionValue = num;
                    else
                        ProductDefinition.IntervalActionValue = num;
                    break;

                //Product Specific
                case "ProductQuantity":
                    ProductDefinition.ProductQuantity = num;
                    break;
                case "BasePriceAmount":
                    ProductDefinition.CurrencyQuantity = num;
                    break;
                case "SupplyThreshold":
                    ProductDefinition.SupplyThreshold = num;
                    break;
                case "LowPrice":
                    ProductDefinition.CurrencyLowQuantity = num;
                    break;
                case "HighPrice":
                    ProductDefinition.CurrencyHighQuantity = num;
                    break;
                case "IdealSupply":
                    ProductDefinition.IdealSupply = num;
                    break;
                case "MaxSupply":
                    ProductDefinition.MaxSupply = num;
                    break;
                default:
                    break;

            }

            PersistChanges();
        }

        private void PersistChanges()
        {
            byte[] data;
            byte type = 0;
            if (CurrencyDefinition != null)
            {
                type = BEVinconAdminRegister.TYPE_CURRENCY;
                data = SerializerUtil.Serialize(CurrencyDefinition);
            }
                
            else
            {
                type = BEVinconAdminRegister.TYPE_PRODUCT;
                data = SerializerUtil.Serialize(ProductDefinition);
            }
               

            using (MemoryStream mos = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(mos);
                writer.Write(type);
                writer.Write(data.Length);
                writer.Write(data);
                capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SAVE_ITEM, mos.ToArray());
            }
            
        }



        public void PopulateResultsList(List<EntryResultDefinition> items)
        {
            GuiElementContainer container = SingleComposer.GetContainer("searchResults");
            container.Clear();

            ResultInventory = new DummyInventory(this.capi, items.Count);

            int insetWidth = 200;
            ElementBounds lastEntry = new ElementBounds().WithParent(SearchContainerBounds);
            if (items.Count > 0)
            {
                int i = 0;
                foreach (EntryResultDefinition item in items)
                {

                    ResultInventory[i].Itemstack = VinUtils.DeserializeProduct(capi, item.Code, 1, item.Attributes);


                    //SingleComposer.AddPassiveItemSlot(shop["entryItem"], Inventory, Inventory[0], false);
                    ElementBounds entryItem = ElementStdBounds.Slot(0, 0).FixedUnder(lastEntry);
                    container.Add(new GuiElementPassiveItemSlot(capi, entryItem, ResultInventory, ResultInventory[i], false));
                    //SingleComposer.AddRichtext("Name", font, shop["entryLabel"]);

                    ElementBounds entryLabel = ElementBounds.FixedSize(200, 30).FixedUnder(lastEntry, 5).RightOf(entryItem, 5);
                    container.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, ResultInventory[i].GetStackName(), CairoFont.WhiteSmallText()), entryLabel));

                    ElementBounds entrySupplyLabel = ElementBounds.FixedSize(200, 30).FixedUnder(lastEntry, 25).RightOf(entryItem, 5);
                    container.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, "x "+ item.Supply, CairoFont.WhiteSmallText()), entrySupplyLabel));

                    int index = item.Id;
                    ElementBounds viewButton = ElementBounds.FixedSize(90, 25).FixedUnder(entryItem, 5);
                    GuiElementTextButton view = new GuiElementTextButton(capi, Lang.Get("vinconomy:gui-view"), DefaultFont, HoverFont, () => { return ViewEntry(index, item.Type); }, viewButton, EnumButtonStyle.Small);
                    view.SetOrientation(EnumTextOrientation.Center);
                    container.Add(view);

                    ElementBounds deleteButton = ElementBounds.FixedSize(90, 25).FixedUnder(entryItem, 5).FixedRightOf(viewButton, 5);
                    GuiElementTextButton del = new GuiElementTextButton(capi, Lang.Get("vinconomy:gui-remove"), DefaultFont, HoverFont, () => { return RemoveEntry(index, item.Type); }, deleteButton, EnumButtonStyle.Small);
                    del.SetOrientation(EnumTextOrientation.Center);
                    container.Add(del);

                    lastEntry = deleteButton;
                    i++;
                }
            } else if (UIInventory[0].Itemstack != null)
            {
                ElementBounds entrySupplyLabel = ElementBounds.FixedSize(insetWidth, 30).FixedUnder(lastEntry, 15);
                container.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, Lang.Get("vinconomy:gui-no-results"), CairoFont.WhiteSmallText()), entrySupplyLabel));

                ElementBounds createButton = ElementBounds.FixedSize(insetWidth, 30).FixedUnder(entrySupplyLabel, 5);
                GuiElementTextButton create = new GuiElementTextButton(capi, Lang.Get("vinconomy:gui-create"), DefaultFont, HoverFont, () => { return CreateEntry(); }, createButton, EnumButtonStyle.Small);
                create.SetOrientation(EnumTextOrientation.Center);
                container.Add(create);
            }

            container.CalcTotalHeight();
            SearchContainerBounds.CalcWorldBounds();
            SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)SearchContainerBounds.fixedHeight);

            Recompose();
        }

        private bool RemoveEntry(int index, int type)
        {
            return true;
        }

        private bool ViewEntry(int index, int type)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(type);
                writer.Write(index);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.LOAD_ITEM, data);
            return true;
        }

        private bool CreateEntry()
        {
            ItemStack stack = UIInventory[0].Itemstack;

            EntryResultDefinition def = new EntryResultDefinition();
            def.ShopId = this.shop.ID;
            def.Code = stack.Collectible.Code;
            def.Supply = stack.StackSize;

            MemoryStream productStream = new MemoryStream();
            BinaryWriter productWriter = new BinaryWriter(productStream);
            stack.Attributes.ToBytes(productWriter);
            byte[] productData = productStream.ToArray();
            def.Attributes = productData;

            //TODO: Better way to tell if this is currency or product?
            def.Type = tabIndex == 0 ? BEVinconAdminRegister.TYPE_CURRENCY : BEVinconAdminRegister.TYPE_PRODUCT;

            byte[] data = SerializerUtil.Serialize(def);
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.CREATE_ITEM, data);
            //SetLoadingResults();
            return true;
        }

        private void OnNewScrollbarValue(float value)
        {
            ElementBounds bounds = SearchContainerBounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        public void UpdateSelectedTab()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].Active = tabIndex == i;
            }
        }
        private void OnTabChanged(int id, GuiTab tab)
        {
            tabIndex = id;
            CurrencyDefinition = null;
            ProductDefinition = null;
            FilteredCache = null;
            UIInventory[0].Itemstack = null;
            Compose();
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

        private void OnToggleIcon(int index)
        {
            this.currentIcon = icons[index];
            SendWaypointUpdate();
        }

        private void OnToggleColor(int index)
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

        internal void OnSearchResults(int type, List<EntryResultDefinition> data, bool isPartial)
        {
            if (!isPartial)
            {
                if (type == BEVinconAdminRegister.TYPE_CURRENCY)
                    CurrencyCache = data;
                else
                    ProductsCache = data;
            }

                PopulateResultsList(data);
        }

        internal void OnLoadCurrency(CurrencyDefinition currency)
        {
            this.CurrencyDefinition = currency;
            this.ProductDefinition = null;
            this.FilteredCache = null;
           
            this.Compose();
        }

        public void OnLoadProduct(ProductDefinition def)
        {
            this.ProductDefinition = def;
            this.CurrencyDefinition = null;
            this.FilteredCache = null;

            Compose();
        }


        public bool CreateItem()
        {

            ItemStack stack = UIInventory[0].Itemstack;

            EntryResultDefinition def = new EntryResultDefinition();
            def.ShopId = this.shop.ID;
            def.Code = stack.Collectible.Code;
            def.Supply = stack.StackSize;

            MemoryStream productStream = new MemoryStream();
            BinaryWriter productWriter = new BinaryWriter(productStream);
            stack.Attributes.ToBytes(productWriter);
            byte[] productData = productStream.ToArray();
            def.Attributes = productData;

            //TODO: Better way to tell if this is currency or product?
            def.Type = tabIndex == 0 ? BEVinconAdminRegister.TYPE_CURRENCY : BEVinconAdminRegister.TYPE_PRODUCT;

            byte[] data = SerializerUtil.Serialize(def);
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, VinConstants.SAVE_ITEM, data);
            SetLoadingResults();
            return true;
        }

        public void SetLoadingResults()
        {
            GuiElementContainer container = SingleComposer.GetContainer("searchResults");
            container.Clear();
            ElementBounds loadingLabel = ElementBounds.FixedSize(insetWidth, 30);
            container.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, Lang.Get("vinconomy:gui-loading"), CairoFont.WhiteSmallText()), loadingLabel));
            this.Recompose();
        }

        private void OnSearchItemSelected(int slot)
        {
            ItemSlot itemSlot = UIInventory[slot]; // Shouldnt EVER be anything other than 0, but still...

            if (tabIndex == BEVinconAdminRegister.TYPE_CURRENCY)
            {
                // Currency
                SearchItem(itemSlot.Itemstack?.Collectible.Code, BEVinconAdminRegister.TYPE_CURRENCY);
            }
            else
            {
                // Products
                SearchItem(itemSlot.Itemstack?.Collectible.Code, BEVinconAdminRegister.TYPE_PRODUCT);
            }

            SetLoadingResults();
        }

        private void SearchItem(string code, int type)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(code == null ? "" : code);
                writer.Write(type);
                data = ms.ToArray();
            }

            this.capi.Network.SendBlockEntityPacket(this.BlockEntityPosition, VinConstants.SEARCH_ITEM, data);
        }
    }
}
