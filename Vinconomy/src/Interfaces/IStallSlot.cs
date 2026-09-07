using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Inventory;
using Commercially.Vinconomy.Trading;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public interface IStallSlot
    {
        public VinconBaseInventory Inventory { get; }

        /// <summary>
        /// How many slots are considered "Product" that the player can fill in to sell items from.
        /// </summary>
        public int StockSlotCount { get; }

        /// <summary>
        /// Returns the slots that are considered "Product" for this stall. These are the slots that the player can fill in to sell items from. In most cases the contents of these slots will match the Product slot.
        /// An exceptions would be the Sculpture stalls where the slots are used to hold the individual items that make up the final product, but the Product slot is a single item bundle that represents the finished product.
        /// </summary>
        /// <returns></returns>
        public ItemSlot[] GetStockSlots();

        public ItemSlot[] GetInternalSlots();

        public int ProductPerPurchase { get; }
        public int CurrencyPerPurchase { get; }

        public int StallSlot { get; }

        /// <summary>
        /// The total number of slots that this stall takes up in the given inventory. This must include any and all slots that should be enumerated over
        /// </summary>
        public int TotalItemSlots { get; }

        /// <summary>
        /// Accessor for the slots provided by the given stall. For the sake of consistency in implementations: Internal slots should be first, followed by Product slots.
        /// </summary>
        /// <param name="slotId"></param>
        /// <returns></returns>
        public ItemSlot this[int slotId] { get; set; }

        public AggregatedStacks ExtractProduct(int amount, int numPurchases,  bool isAdminOwned);

        public int GetTotalProductAvailable();

        /// <summary>
        /// The Product given to the customer. The StackSize should be representitive of how much of an item is given to the customer. For example, if the shop were to be selling a stack of 64 Dirt, then 
        /// the stack size would be 64. For non-items such as a Serving of a meal or Liter of a liquid, it should be the number of servings or liters given to the customer.
        /// </summary>
        public ItemStack GetOfferedProduct();

        /// <summary>
        /// The Currency used for the purchase. The StackSize should be representitive of how much an item costs. For example, if something were to cost 6 Rusty Gears, its stack size would be 6. 
        /// </summary>
        public ItemStack GetRequiredCurrency();
        public ICurrencySinkProvider GetCurrencySink(PurchaseRequest req);
        public ICouponSinkProvider GetCouponSink(PurchaseRequest req);

        public PurchaseRequest CreatePurchaseRequest(IPlayer player, int numPurchases, IShopComponent shop, IStallComponent stall);
    }
}