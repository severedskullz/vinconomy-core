using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Vintagestory.API.Common;

namespace Viconomy.Trading
{
    public class GenericTradeRequest
    {
        public ICoreAPI Api;
        public IPlayer Customer;

        public BEVinconRegister ShopRegister;
        public BEVinconBase SellingEntity;
        public bool IsAdminShop;

        public int NumPurchases;
        public int RequestedPurchases;

        public int ProductBonusPerPurchase; // Applied via Group or Coupon set in Register
        public int ProductNeededPerPurchase;
        public ItemStack ProductStackNeeded;
        public AggregatedSlots ProductSourceSlots;

        public int CurrencyDiscountPerPurchase; // Applied via Group or Coupon set in Register
        public int CurrencyNeededPerPurchase;
        public ItemStack CurrencyStackNeeded;
        public AggregatedSlots CurrencySourceSlots;

        public bool ConsumeCoupon;
        public bool CouponPerTrade;
        public ItemStack CouponStackNeeded;
        public AggregatedSlots CouponSlots;

        public ItemStack TradePassNeeded;
        public AggregatedSlots TradePassSlots;

        public AggregatedSlots ToolSourceSlots;
        public int ToolUsesNeededPerTrade;

        public GenericTradeRequest(ICoreAPI api, IPlayer player)
        {
            Api = api;
            Customer = player;
        }

        public GenericTradeRequest WithShop(BEVinconRegister register, BEVinconBase shop, bool adminShop)
        {
            ShopRegister = register;
            SellingEntity = shop;
            IsAdminShop = adminShop;  
            return this;
        }

        public GenericTradeRequest WithProduct(ItemStack productNeeded, AggregatedSlots slots, int productPerPurchase)
        {
            ProductStackNeeded = productNeeded;
            ProductSourceSlots = slots;
            ProductNeededPerPurchase = productPerPurchase;
            return this;
        }

        public GenericTradeRequest WithCurrency(ItemStack currencyNeeded, AggregatedSlots slots, int productPerPurchase)
        {
            CurrencyStackNeeded = currencyNeeded;
            CurrencySourceSlots = slots;
            CurrencyNeededPerPurchase = productPerPurchase;
            
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
        public GenericTradeRequest WithCoupons(ItemStack couponNeeded, AggregatedSlots slots, bool perTradeBasis, bool consumeCoupon, int bonusPerPurchase, int discountPerPurchase)
        {
            CouponStackNeeded = couponNeeded;
            CouponSlots = slots;
            CouponPerTrade = perTradeBasis;
            ConsumeCoupon = consumeCoupon;
            CurrencyDiscountPerPurchase = discountPerPurchase;
            ProductBonusPerPurchase = bonusPerPurchase;
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
                if (CouponPerTrade)
                {
                    totalTrades = Math.Min(totalTrades, CouponSlots.TotalCount);
                } 
                // Need only 1 coupon to trade for any number of purchases, so if we dont have *any*, we cant trade.
                else if (CouponSlots.TotalCount == 0)
                {
                    return 0;
                }
            }

            // If we never called WithCoupons(), discount and bonus should still be 0
            // Also, I wish Math.Min was a var-arg... :/
            totalTrades = Math.Min(totalTrades, CurrencySourceSlots.TotalCount / (CurrencyNeededPerPurchase - CurrencyDiscountPerPurchase));
            totalTrades = Math.Min(totalTrades, ProductSourceSlots.TotalCount / (ProductNeededPerPurchase + ProductBonusPerPurchase));

            return totalTrades;

        }

    }

    public class GenericTradeResult {
        public GenericTradeRequest Request;
        public string Error;

        //TODO: IDK if I want to deal with DummySlot wrappers around the item stack or not for now.
        //public AggregatedSlots TransferedCurrency;
        //public AggregatedSlots TransferedProduct;
       
        public List<ItemStack> TransferedCurrency;
        public int TransferedCurrencyTotal;
        public List<ItemStack> TransferedProduct;
        public int TransferedProductTotal;

        public VinconomyCoreSystem modSystem;

        public GenericTradeResult(GenericTradeRequest req, VinconomyCoreSystem core)
        {
            Request = req;
            //TransferedCurrency = new AggregatedSlots();
            //TransferedProduct = new AggregatedSlots();
            TransferedCurrency = new List<ItemStack>();
            TransferedProduct = new List<ItemStack>();
            modSystem = core;
        }
    }
}