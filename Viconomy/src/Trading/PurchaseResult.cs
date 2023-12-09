using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Vintagestory.API.Common;

namespace Viconomy.Trading
{
    public class PurchaseResult
    {
        public IPlayer customer;
        public BEVRegister shopRegister;
        public string error;
        public List<ItemSlot> paymentSlots;
        public StallSlot purchaseSlot;
        public int numPurchases;

    }

    public class PurchaseRequest
    {
        public IPlayer customer;
        public BEVRegister shopRegister;
        public bool requiresContainer;
        public bool requiresTool;
        public ItemStack tool;
        public List<ItemSlot> paymentSlots;
        public StallSlot stallSlot;
        public int numTryPurchase;
        public int discountedPrice;
    }
}