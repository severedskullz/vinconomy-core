using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
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
}