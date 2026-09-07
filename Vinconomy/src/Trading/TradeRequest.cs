using Commercially.Common.Inventory.Slots;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using System.Collections.Generic;
using Vinconomy.ItemTypes;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Trading
{

    public enum TradeType
    {
        Generic,
        Liquid,
        Meal
    }

    public class TradeInfo
    {
        public ICoreAPI Api;
        public IPlayer Customer;
        public IShopComponent ParentEntity;
        public IStallComponent SellingEntity;
        public ItemStack ProductNeeded;
        public ItemStack CurrencyNeeded;
        public ItemStack TradePassNeeded;
        public ItemStack CouponNeeded;
        public bool ConsumeCoupon;
        public int CouponValue;
        public string CouponBonusType;
        public string CouponDiscountType;
        public int NumPurchases;
        public int RequestedPurchases;
        public bool IsAdminShop;
        public int StallSlot;
        public int CurrencyNeededPerPurchase => GetFinalCurrencyNeededPerPurchase();
        public int ProductNeededPerPurchase => GetFinalProductNeededPerPurchase();
        public int GetFinalProductNeededPerPurchase()
        {
            int bonusProduct = 0;
            if (CouponBonusType == ItemCoupon.BONUS_TYPE_PRODUCT)
            {
                if (CouponDiscountType == ItemCoupon.DISCOUNT_TYPE_PERCENT)
                    bonusProduct = (int)((CouponValue / 100.0f) * ProductNeeded.StackSize);
                else if (CouponDiscountType == ItemCoupon.DISCOUNT_TYPE_UNIT)
                    bonusProduct = CouponValue;
            }
            return ProductNeeded.StackSize + bonusProduct;
        }

        public int GetFinalCurrencyNeededPerPurchase()
        {
            int priceDiscount = 0;
            if (CouponBonusType == ItemCoupon.BONUS_TYPE_DISCOUNT)
            {
                if (CouponDiscountType == ItemCoupon.DISCOUNT_TYPE_PERCENT)
                    priceDiscount = (int)((CouponValue / 100.0f) * CurrencyNeeded.StackSize);
                else if (CouponDiscountType == ItemCoupon.DISCOUNT_TYPE_UNIT)
                    priceDiscount = CouponValue;
            }

            return Math.Max(0, CurrencyNeeded.StackSize - priceDiscount);
        }
    }

    public class TradeRequest : TradeInfo
    {
        public TradeType TradeType = TradeType.Generic;
        public AggregatedSlots ProductSourceSlots;
        public bool IsProductGenerated = false;
        public AggregatedSlots CurrencySourceSlots;
        public AggregatedSlots TradePassSourceSlots;
        public AggregatedSlots CouponSourceSlots;
        public DurabilityAggregatedSlots ToolSourceSlots;
        public CapacityAggregatedSlots ContainerSourceSlots;
        public int ToolUsesNeededPerTrade;

        public TradeRequest(ICoreAPI api, IPlayer player, TradeType tradeType = TradeType.Generic)
        {
            Api = api;
            Customer = player;
            TradeType = tradeType;
        }

        public TradeRequest WithShop(IShopComponent parentShop, IStallComponent sellingEntity, int stallSlot, bool adminShop)
        {
            ParentEntity = parentShop;
            SellingEntity = sellingEntity;
            IsAdminShop = adminShop;
            StallSlot = stallSlot;
            return this;
        }

        public TradeRequest WithProduct(ItemStack productNeeded, AggregatedSlots slots, int productPerPurchase)
        {
            ProductNeeded = productNeeded;
            ProductSourceSlots = slots;
            //_ProductNeededPerPurchase = productPerPurchase;
            return this;
        }

        public TradeRequest WithGeneratedProduct(ItemStack productNeeded)
        {
            ProductNeeded = productNeeded;
            IsProductGenerated = true;
            return this;
        }

        public TradeRequest WithCurrency(ItemStack currencyNeeded, AggregatedSlots slots, int currencyPerPurchase)
        {
            CurrencyNeeded = currencyNeeded;
            CurrencySourceSlots = slots;
            //_CurrencyNeededPerPurchase = currencyPerPurchase;

            return this;
        }

        public TradeRequest WithTools(DurabilityAggregatedSlots slots, int usesPerTrade)
        {
            ToolSourceSlots = slots;
            ToolUsesNeededPerTrade = usesPerTrade;
            return this;
        }

        public TradeRequest WithContainers(CapacityAggregatedSlots slots)
        {
            ContainerSourceSlots = slots;
            return this;
        }

        public TradeRequest WithCoupons(ItemSlot couponSlot)
        {
            if (couponSlot.Itemstack != null && couponSlot.Itemstack.Class == EnumItemClass.Item)
            {
                if (couponSlot.Itemstack.Item.Code == "vinconomy:coupon")
                {
                    CouponNeeded = couponSlot.Itemstack;
                    //TODO: Kinda pointless if we only want to ever have 1 coupon applied at at time. Should see about refactoring this.
                    GenericAggregatedSlots couponSlots = new GenericAggregatedSlots(Api);
                    couponSlots.Add(couponSlot);
                    CouponSourceSlots = couponSlots;


                    ITreeAttribute attrs = couponSlot.Itemstack.Attributes;
                    ConsumeCoupon = attrs.GetBool(ItemCoupon.CONSUME_COUPON);
                    CouponDiscountType = attrs.GetString(ItemCoupon.DISCOUNT_TYPE);
                    CouponBonusType = attrs.GetString(ItemCoupon.BONUS_TYPE);
                    CouponValue = attrs.GetInt(ItemCoupon.VALUE);
                }
            }
            return this;
        }

        public TradeRequest WithTradePass(ItemStack pass, AggregatedSlots slots)
        {
            TradePassNeeded = pass;
            TradePassSourceSlots = slots;
            return this;
        }

        public TradeRequest WithPurchases(int numPurchases)
        {
            RequestedPurchases = numPurchases;
            return this;
        }

        public ICurrencySinkProvider GetCurrencySink()
        {

            ICurrencySinkProvider sink = SellingEntity.GetStallSlot(StallSlot) as ICurrencySinkProvider;
            if (sink != null) {
                return sink;
            }


            sink = SellingEntity.GetComponent<ICurrencySinkProvider>();
            if (sink != null)
            {
                return sink;
            }
            return ParentEntity?.GetComponent<IShopInventoryProvider>();
        }

        public ICouponSinkProvider GetCouponSink()
        {

            ICouponSinkProvider sink = SellingEntity.GetStallSlot(StallSlot) as ICouponSinkProvider;
            if (sink != null)
            {
                return sink;
            }


            sink = SellingEntity.GetComponent<ICouponSinkProvider>();
            if (sink != null)
            {
                return sink;
            }
            return ParentEntity?.GetComponent<ICouponSinkProvider>();
        }

        public TradeRequest Build()
        {
            NumPurchases = GetAffordablePurchases();
            return this;
        }

        /// <summary>
        /// Returns the number of trades that can be afforded based on the slots in CurrencySourceSlots, ProductSourceSlots
        /// and whether or not tools are needed
        /// </summary>
        /// <returns></returns>
        public int GetAffordablePurchases()
        {
            int totalTrades = RequestedPurchases;

            int currencyNeeded = GetFinalCurrencyNeededPerPurchase();
            if (currencyNeeded > 0)
                totalTrades = Math.Min(totalTrades, CurrencySourceSlots.TotalCount / currencyNeeded);

            int productNeeded = GetFinalProductNeededPerPurchase();
            if (productNeeded > 0 && !IsAdminShop)
                totalTrades = Math.Min(totalTrades, ProductSourceSlots.TotalCount / productNeeded);

            /*
            if (ToolSourceSlots != null)
            {

                if (ToolSourceSlots is LiquidCapacityAggregatedSlots liquid)
                {
                    float neededCapacity = LiquidTradeHandler.ConvertStackToLiters(this.ProductNeeded, productNeeded);
                    totalTrades = Math.Min(totalTrades, (int)(liquid.TotalCapacity / neededCapacity));
                }
                else if (ToolUsesNeededPerTrade > 0)
                    totalTrades = Math.Min(totalTrades, ToolSourceSlots.TotalCount / ToolUsesNeededPerTrade);
            }
            */

            if (CouponSourceSlots != null)
            {
                //Need 1 coupon per every trade.
                if (ConsumeCoupon)
                {
                    totalTrades = Math.Min(totalTrades, CouponSourceSlots.TotalCount);
                }
                // Need only 1 coupon to trade for any number of purchases, so if we dont have *any*, we cant trade.
                else if (CouponSourceSlots.TotalCount == 0)
                {
                    return 0;
                }
            }
            return totalTrades;

        }
    }

    public class TradeResult
    {
        public string ErrorMsg { get; set; }
        public TradeRequest Request { get; set; }
        public AggregatedStacks ProductStacks { get; set; }
        public AggregatedStacks CurrencyStacks { get; set; }
        public AggregatedStacks CouponStacks { get; set; }
        public Dictionary<string, object> CustomData { get; set; }

        public int TotalProductAmount => Request.GetFinalProductNeededPerPurchase() * Request.NumPurchases;
        public int TotalCurrencyAmount => Request.GetFinalCurrencyNeededPerPurchase() * Request.NumPurchases;
        public int FinalPurchases => Request.NumPurchases;

        public BaseStallSlot Stall => Request.SellingEntity.GetStallSlot(Request.StallSlot);

        public TradeResult(TradeRequest req)
        {
            Request = req;
            ProductStacks = new();
            CurrencyStacks = new();
            CouponStacks = new();
            CustomData = [];

        }
    }
}
