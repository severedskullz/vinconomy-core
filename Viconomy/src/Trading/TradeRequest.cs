
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Vintagestory.API.Common;

namespace Viconomy.Trading
{
    public class TradeRequest
    {
        public ICoreAPI coreApi;
        public IPlayer customer;
        public BEVinconRegister shopRegister;
        public BEVinconBase sellingEntity;

        //public Product product;
        public ItemStack productNeeded;
        public ItemSlot[] productSourceSlots;

        

        public ItemStack currencyNeeded;
        //public ItemSlot[] currencySourceSlots;

        public int numPurchases;

        public int discountedPrice;
        public bool isAdminShop;

        //public ToolType requiredToolType;
        //public ItemSlot tool;
        //public bool shouldConsumeTool;

        //public ItemStack couponNeeded;
        //public bool shouldConsumeCoupon;

    }

    public class TradeResult : TradeRequest
    {
        public string error;
        public List<ItemSlot> currencySourceSlots;
        public ItemStack purchasedItems;
        public ItemStack purchasedCurrencyUsed;

        public TradeResult() { }// Until I refactor

        public TradeResult(TradeRequest request)
        {

            currencySourceSlots = new List<ItemSlot>();

            coreApi = request.coreApi;
            customer = request.customer;
            currencyNeeded = request.currencyNeeded;
            productNeeded = request.productNeeded;
            productSourceSlots = request.productSourceSlots;
            shopRegister = request.shopRegister;
            isAdminShop = request.isAdminShop;
            discountedPrice = request.discountedPrice;
            sellingEntity = request.sellingEntity;

            //requiredToolType = request.requiredToolType;
            //tool = request.tool;
            //shouldConsumeTool = request.shouldConsumeTool;

        }
    }
}