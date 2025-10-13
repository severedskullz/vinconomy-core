using System;
using Viconomy.BlockEntities;
using Viconomy.ItemTypes;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Trading
{
    public class GenericTradeRequest
    {
        public ICoreAPI Api;
        public IPlayer Customer;

        public BEVinconRegister ShopRegister;
        public int StallSlot;
        public BEVinconBase SellingEntity;
        public bool IsAdminShop;

        public int NumPurchases;
        public int RequestedPurchases;

        public int ProductNeededPerPurchase;
        public ItemStack ProductStackNeeded;
        public AggregatedSlots ProductSourceSlots;

        
        public int CurrencyNeededPerPurchase;
        public ItemStack CurrencyStackNeeded;
        public AggregatedSlots CurrencySourceSlots;

        public bool ConsumeCoupon;
        public ItemStack CouponStackNeeded;
        public AggregatedSlots CouponSlots;
        public int CouponValue;
        public string CouponBonusType;
        public string CouponDiscountType;

        public ItemStack TradePassNeeded;
        public AggregatedSlots TradePassSlots;

        public AggregatedSlots ToolSourceSlots;
        public int ToolUsesNeededPerTrade;

        public GenericTradeRequest(ICoreAPI api, IPlayer player)
        {
            Api = api;
            Customer = player;
        }

        public GenericTradeRequest WithShop(BEVinconRegister register, BEVinconBase shop, int stallSlot, bool adminShop)
        {
            ShopRegister = register;
            SellingEntity = shop;
            IsAdminShop = adminShop;
            StallSlot = stallSlot;
            return this;
        }

        public GenericTradeRequest WithProduct(ItemStack productNeeded, AggregatedSlots slots, int productPerPurchase)
        {
            ProductStackNeeded = productNeeded;
            ProductSourceSlots = slots;
            ProductNeededPerPurchase = productPerPurchase;
            return this;
        }

        public GenericTradeRequest WithCurrency(ItemStack currencyNeeded, AggregatedSlots slots, int currencyPerPurchase)
        {
            CurrencyStackNeeded = currencyNeeded;
            CurrencySourceSlots = slots;
            CurrencyNeededPerPurchase = currencyPerPurchase;
            
            return this;
        }

        public GenericTradeRequest WithTools(AggregatedSlots slots, int usesPerTrade)
        {
            ToolSourceSlots = slots;
            ToolUsesNeededPerTrade = usesPerTrade;
            return this;
        }

        /// <summary>
        /// Sets the honored coupon for this trade.
        /// </summary>
        /// 
        /// <param name="couponNeeded"> The ItemStack for the coupon to be used</param>
        /// <param name="slots"> The aggregated slots to be considered for this trade </param>
        /// <param name="perTradeBasis"> Whether or not you need 1 coupon for each purchase (true), or 1 coupon per trade interaction (false)</param>
        /// <param name="consumeCoupon"> Whether or not to take the coupon from the player and place it inside the register</param>
        /// <param name="bonusPerPurchase"> The amount of extra product the customer will recieve</param>
        /// <param name="discountPerPurchase"> The discount on the price paid</param>
        public GenericTradeRequest WithCoupons(ItemSlot couponSlot)
        {
            if (couponSlot.Itemstack != null && couponSlot.Itemstack.Class == EnumItemClass.Item)
            {
                if (couponSlot.Itemstack.Item.Code == "vinconomy:coupon")
                {
                    CouponStackNeeded = couponSlot.Itemstack;
                    //TODO: Kinda pointless if we only want to ever have 1 coupon applied at at time. Should see about refactoring this.
                    AggregatedSlots couponSlots = new AggregatedSlots(Api);
                    couponSlots.Add(couponSlot);
                    CouponSlots = couponSlots;


                    ITreeAttribute attrs = couponSlot.Itemstack.Attributes;
                    ConsumeCoupon = attrs.GetBool(ItemCoupon.CONSUME_COUPON);
                    CouponDiscountType = attrs.GetString(ItemCoupon.DISCOUNT_TYPE);
                    CouponBonusType = attrs.GetString(ItemCoupon.BONUS_TYPE);
                    CouponValue = attrs.GetInt(ItemCoupon.VALUE);
                }
            }
                return this;
        }

        public GenericTradeRequest WithTradePass(ItemStack pass, AggregatedSlots slots)
        {
            TradePassNeeded = pass;
            TradePassSlots = slots;
            return this;
        }

        public GenericTradeRequest WithPurchases(int numPurchases)
        {
            RequestedPurchases = numPurchases;
            return this;
        }

        public GenericTradeRequest Build()
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
            if (ToolSourceSlots != null && ToolUsesNeededPerTrade > 0)
            {
                totalTrades = Math.Min(totalTrades, ToolSourceSlots.TotalCount / ToolUsesNeededPerTrade);
            }

            if (CouponSlots != null)
            {
                //Need 1 coupon per every trade.
                if (ConsumeCoupon)
                {
                    totalTrades = Math.Min(totalTrades, CouponSlots.TotalCount);
                } 
                // Need only 1 coupon to trade for any number of purchases, so if we dont have *any*, we cant trade.
                else if (CouponSlots.TotalCount == 0)
                {
                    return 0;
                }
            }

            int currencyNeeded = GetFinalCurrencyNeededPerPurchase();
            if (currencyNeeded > 0)
                totalTrades = Math.Min(totalTrades, CurrencySourceSlots.TotalCount / currencyNeeded);

            int productNeeded = GetFinalProductNeededPerPurchase();
            if (productNeeded > 0 && !IsAdminShop)
                totalTrades = Math.Min(totalTrades, ProductSourceSlots.TotalCount / productNeeded);

            return totalTrades;

        }

        public int GetFinalProductNeededPerPurchase()
        {
            int bonusProduct = 0;
            if (CouponBonusType == ItemCoupon.BONUS_TYPE_PRODUCT)
            {
                if (CouponDiscountType == ItemCoupon.DISCOUNT_TYPE_PERCENT)
                    bonusProduct = (int)((CouponValue / 100.0f) * ProductNeededPerPurchase);
                else if (CouponDiscountType == ItemCoupon.DISCOUNT_TYPE_UNIT)
                    bonusProduct = CouponValue;
            }
            return ProductNeededPerPurchase + bonusProduct;
        }

        public int GetFinalCurrencyNeededPerPurchase()
        {
            int priceDiscount = 0;
            if (CouponBonusType == ItemCoupon.BONUS_TYPE_DISCOUNT)
            {
                if (CouponDiscountType == ItemCoupon.DISCOUNT_TYPE_PERCENT)
                    priceDiscount = (int)((CouponValue / 100.0f) * CurrencyNeededPerPurchase);
                else if (CouponDiscountType == ItemCoupon.DISCOUNT_TYPE_UNIT)
                    priceDiscount = CouponValue;
            }

            return Math.Max(0, CurrencyNeededPerPurchase - priceDiscount);
        }
    }

    

}