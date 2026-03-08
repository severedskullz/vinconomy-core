using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vinconomy.Trading
{
    public class GenericTradeResult
    {
        public GenericTradeRequest Request;
        public string Error;

        //TODO: IDK if I want to deal with DummySlot wrappers around the item stack or not for now.
        //public AggregatedSlots TransferedCurrency;
        //public AggregatedSlots TransferedProduct;

        public List<ItemStack> TransferedCurrency;
        public int TransferedCurrencyTotal;

        public List<ItemStack> TransferedProduct;
        public int TransferedProductTotal;

        public List<ItemStack> TransferedCoupons;
        public int TransferedCouponsTotal;

        public VinconomyCoreSystem modSystem;

        public GenericTradeResult(GenericTradeRequest req, VinconomyCoreSystem core)
        {
            Request = req;
            //TransferedCurrency = new AggregatedSlots();
            //TransferedProduct = new AggregatedSlots();
            TransferedCurrency = new List<ItemStack>();
            TransferedProduct = new List<ItemStack>();
            TransferedCoupons = new List<ItemStack>();
            modSystem = core;
        }

        public ItemStack GetTotalTransferedProduct()
        {
            if (TransferedProduct.Count > 0)
            {
                ItemStack clone = TransferedProduct[0].Clone();
                clone.StackSize = TransferedProductTotal;
                return clone;
            }
            return null;
        }

        public ItemStack GetTotalTransferedCurrency()
        {
            if (TransferedCurrency.Count > 0)
            {
                ItemStack clone = TransferedCurrency[0].Clone();
                clone.StackSize = TransferedCurrencyTotal;
                return clone;
            }
            return null;
        }
    }
}
