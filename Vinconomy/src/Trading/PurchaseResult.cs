using Commercially.Common.Inventory.Slots;
using System.Collections.Generic;

namespace Commercially.Vinconomy.Trading
{
    public class PurchaseResult
    {
        public PurchaseRequest Request { get; set; }
        public string ErrorMsg { get; set; }
        public AggregatedStacks ProductStacks { get; set; }
        public AggregatedStacks CurrencyStacks { get; set; }
        public AggregatedStacks CouponStacks { get; set; }
        public Dictionary<string, object> CustomData { get; set; }

        public int TotalProductAmount => Request.GetFinalProductNeededPerPurchase() * Request.NumPurchases;
        public int TotalCurrencyAmount => Request.GetFinalCurrencyNeededPerPurchase() * Request.NumPurchases;
        public int FinalPurchases => Request.NumPurchases;

        public PurchaseResult(PurchaseRequest req)
        {
            Request = req;
            ProductStacks = new();
            CurrencyStacks = new();
            CouponStacks = new();
            CustomData = [];

        }
    }
}
