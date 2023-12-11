using System.Collections;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Viconomy.src.BlockEntities;
using Vintagestory.API.Common;

namespace Viconomy.Trading
{
    public class PurchaseResult
    {
        public ICoreAPI coreApi;
        public IPlayer customer;
        public BEVRegister shopRegister;
        
        public BEViconStall stall;
        public StallSlot stallSlot;
        public List<ItemSlot> paymentSlots;
        public int numPurchases;

        public ItemStack purchasedItems;
        public ItemStack purchasedCurrencyUsed;

        public string error;

        public PurchaseResult(PurchaseRequest request)
        {
            coreApi = request.coreApi;
            customer = request.customer;
            shopRegister = request.shopRegister;
            stall = request.stall;
            stallSlot = request.stallSlot;
            paymentSlots = new List<ItemSlot>();
        }

    }

    public class PurchaseRequest
    {
        public ICoreAPI coreApi;
        public IPlayer customer;
        public BEVRegister shopRegister;

        public BEViconStall stall;
        public StallSlot stallSlot;

        public int numTryPurchase;
        public int discountedPrice;

        public bool requiresContainer;
        public bool requiresTool;
        public ItemStack tool;
    }

    public class TradeRequest
    {
        public ICoreAPI coreApi;
        public IPlayer customer;
        public BEVRegister shopRegister;

        public ItemStack currencyNeeded;
        //public ItemSlot[] currencySourceSlots;

        public ItemStack productNeeded;
        public ItemSlot[] productSourceSlots;

        public int numPurchases;

        public int discountedPrice;
        public bool isAdminShop;
        public bool requiresContainer;
        public bool requiresTool;
        public ItemStack tool;
        internal BEViconBase sellingEntity;
    }

    public class TradeResult : TradeRequest
    {
        public string error;
        public List<ItemSlot> currencySourceSlots;
        public ItemStack purchasedItems;
        public ItemStack purchasedCurrencyUsed;

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
            requiresContainer = request.requiresContainer;
            requiresTool = request.requiresTool;
            tool = request.tool;
            discountedPrice = request.discountedPrice;


        }
    }
}