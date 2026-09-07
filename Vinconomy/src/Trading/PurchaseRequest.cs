using Commercially.Common.Interfaces;
using Commercially.Common.Inventory.Slots;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using System;
using Vinconomy.ItemTypes;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Trading
{
    public class PurchaseRequest
    {
        public ICoreAPI Api;
        public IPlayer Customer;
        public IShopComponent ParentEntity;
        public IStallComponent SellingEntity;
        public IStallSlot StallSlot;
        public bool IsAdminShop;
        public int StallSlotIndex;

        public ItemStack ProductNeeded;
        public ItemStack CurrencyNeeded;
        public ItemStack TradePassNeeded;

        public bool ConsumeCoupon;
        public int CouponValue;
        public string CouponBonusType;
        public string CouponDiscountType;

        public int RequestedPurchases;
        public int DiscountedPrice;


        public AggregatedSlots CurrencySourceSlots;
        public AggregatedSlots TradePassSourceSlots;
        public ItemSlot CouponSourceSlots;
        public DurabilityAggregatedSlots ToolSourceSlots;
        public CapacityAggregatedSlots ContainerSourceSlots;
        public int ToolUsesNeededPerTrade;


        public int NumPurchases;

        public PurchaseRequest(ICoreAPI api, IPlayer customer) { 
            Api = api;
            Customer = customer;
        }

        public PurchaseRequest(ICoreAPI api, IPlayer customer, IShopComponent shop, IStallComponent stall, int stallSlot, int numRequestedPurchases) : this(api,customer)
        {
            Api = api;
            Customer = customer;
            WithShop(shop);
            WithShopProduct(stall, stallSlot, stall.Ownable?.IsAdminOwned ?? false);
            WithPurchases(numRequestedPurchases);
            WithCurrencyFromCustomer();
            WithCouponsFromCustomer();
            WithRequiredTradePass();
            WithRequiredContainersFromCustomer();
            WithRequiredToolsFromCustomer();

        }

        public PurchaseRequest WithShop(IShopComponent parentShop)
        {
            ParentEntity = parentShop;

            return this;
        }

        public PurchaseRequest WithShopProduct(IStallComponent sellingEntity, int stallSlot, bool adminShop)
        {
            SellingEntity = sellingEntity;
            IsAdminShop = adminShop;
            StallSlotIndex = stallSlot;
            StallSlot = sellingEntity.GetStallSlot(stallSlot) as IStallSlot;

            ProductNeeded = StallSlot.GetOfferedProduct();
            CurrencyNeeded = StallSlot.GetRequiredCurrency();

            return this;
        }

        public PurchaseRequest WithCurrencyFromCustomer()
        {
            CurrencySourceSlots = TradingUtil.GetAllValidSlotsFor(Customer, CurrencyNeeded);
            return this;
        }

        public PurchaseRequest WithCouponsFromCustomer()
        {
            ItemSlot couponSlot = TradingUtil.GetCouponsSlotsForShop(Customer, ProductNeeded, ParentEntity);
            if (couponSlot != null)
            {

                if (couponSlot.Itemstack != null && couponSlot.Itemstack.Class == EnumItemClass.Item)
                {
                    if (couponSlot.Itemstack.Item.Code == "vinconomy:coupon")
                    {
                        CouponSourceSlots = couponSlot;


                        ITreeAttribute attrs = couponSlot.Itemstack.Attributes;
                        ConsumeCoupon = attrs.GetBool(ItemCoupon.CONSUME_COUPON);
                        CouponDiscountType = attrs.GetString(ItemCoupon.DISCOUNT_TYPE);
                        CouponBonusType = attrs.GetString(ItemCoupon.BONUS_TYPE);
                        CouponValue = attrs.GetInt(ItemCoupon.VALUE);
                    }
                }
            }
            return this;
        }

        public PurchaseRequest WithPurchases(int numPurchases)
        {
            RequestedPurchases = numPurchases;
            return this;
        }

        public PurchaseRequest WithRequiredTradePass()
        {
            ITradePassProvider inv = ParentEntity?.GetComponent<IInventoryProvider>()?.Inventory as ITradePassProvider;
            if (inv != null)
            {
                ItemStack tradePass = inv.GetTradePass();
                if (tradePass != null)
                {
                    TradePassNeeded = tradePass;
                    TradePassSourceSlots = TradingUtil.GetAllValidSlotsFor(Customer, tradePass);
                }
            }

            return this;
        }

        public PurchaseRequest WithRequiredContainersFromCustomer()
        {
            IContainedStallSlot containerStall = StallSlot as IContainedStallSlot;
            if (containerStall != null)
            {
                ContainerSourceSlots = containerStall.GetRequiredContainers(Customer);
            }
            return this;
        }

        public PurchaseRequest WithRequiredToolsFromCustomer()
        {
            ITooledStallSlot containerStall = StallSlot as ITooledStallSlot;
            if (containerStall != null)
            {
                ToolSourceSlots = containerStall.GetRequiredTools(Customer);
            }
            return this;
        }

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

        public int GetFinalProductNeeded()
        {
            return GetFinalProductNeededPerPurchase() * NumPurchases;
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

        public int GetFinalCurrencyNeeded()
        {
            return GetFinalCurrencyNeededPerPurchase() * NumPurchases;
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
                totalTrades = Math.Min(totalTrades, StallSlot.GetTotalProductAvailable() / productNeeded);

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
                    totalTrades = Math.Min(totalTrades, CouponSourceSlots.StackSize);
                }
                // Need only 1 coupon to trade for any number of purchases, so if we dont have *any*, we cant trade.
                else if (CouponSourceSlots.StackSize == 0)
                {
                    return 0;
                }
            }
            return totalTrades;

        }

        public PurchaseRequest Build()
        {
            NumPurchases = GetAffordablePurchases();
            return this;
        }
    }
}
