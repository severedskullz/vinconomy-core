using Commercially.Common.Interfaces;
using Commercially.Common.Inventory.Slots;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Trading;
using System;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Commercially.Vinconomy.Inventory.StallSlots
{
    public abstract class BaseStallSlot : IStallSlot
    {
        public const string PRODUCT = "product";
        public const string CURRENCY = "currency";
        public const string FUZZY_MATCHING = "fuzzyMatching";


        public VinconBaseInventory Inventory { get; protected set; }

        /// <summary>
        /// The Currency used for the purchase. The StackSize should be representitive of how much an item costs. For example, if something were to cost 6 Rusty Gears, its stack size would be 6. 
        /// </summary>
        public virtual CurrencySlot Currency { get; protected set; }

        /// <summary>
        /// The Product given to the customer. The StackSize should be representitive of how much of an item is given to the customer. For example, if the shop were to be selling a stack of 64 Dirt, then 
        /// the stack size would be 64.
        /// </summary>
        public virtual ProductSlot Product { get; protected set; }

        /// <summary>
        /// How many slots are considered "Product" that the player can fill in to sell items from.
        /// </summary>
        public virtual int StockSlotCount => GetStockSlots()?.Length ?? 0;

        /// <summary>
        /// How many slots are considered "Internal" and not a part of the Product slots. At the very least each stall should have 2 slots: Currency and Product.
        /// If your implementation needs more slots, simply increase this number to account for all the slots added. This field is used when enumerating over the entire inventory
        /// as well as accessing individual item slots for the GUI components
        /// </summary>
        public virtual int InternalSlotCount => (GetInternalSlots()?.Length ?? 0) + 2; // Currency + Product slot

        /// <summary>
        /// The total number of slots that this stall should take up - inclusive of both Internal (Currency, Product, etc.) and Product (Items for sale) slots
        /// </summary>
        public virtual int TotalItemSlots => InternalSlotCount + StockSlotCount;
        public int ProductPerPurchase { get => Math.Max(1,Product.StackSize); set => Product.Itemstack?.StackSize = value; }
        public int CurrencyPerPurchase { get => Math.Max(1, Currency.StackSize); set => Currency.Itemstack?.StackSize = value; }

        public bool IsFuzzyMatching {  get; set; }

        public abstract bool IsInitialized { get; }

        public int StallSlot { get; protected set; }

        /// <summary>
        /// Accessor for the slots provided by the given stall. For the sake of consistency in implementations: Internal slots should be first, followed by Product slots.
        /// </summary>
        /// <param name="slotId"></param>
        /// <returns></returns>
        public abstract ItemSlot this[int slotId] { get; set; }

        public BaseStallSlot(VinconBaseInventory inventory, int stallSlot) {
            this.Inventory = inventory;
            this.StallSlot = stallSlot;
            Currency = new CurrencySlot(inventory);
            Product = new ProductSlot(inventory);
        }

        public virtual ItemSlot GetInternalSlot(int slotId)
        {
            if (slotId == 0) return Currency;
            else return Product;
        }

        public virtual void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetItemstack(CURRENCY, Currency.Itemstack);
            tree.SetItemstack(PRODUCT, Product.Itemstack);
            tree.SetBool(FUZZY_MATCHING, IsFuzzyMatching);

        }

        public virtual void FromTreeAttributes(ITreeAttribute tree)
        {

            //This technically gets called BEFORE Initialize() does, so Currency and Product might be null
            ItemStack currencyStack = tree.GetItemstack(CURRENCY);
            
            Currency.Itemstack = currencyStack;


            ItemStack productStack = tree.GetItemstack(PRODUCT);
            
            Product.Itemstack = productStack;

            if (Inventory.Api?.World != null)
            {
                currencyStack?.ResolveBlockOrItem(Inventory.Api.World);
                productStack?.ResolveBlockOrItem(Inventory.Api.World);
            }

            IsFuzzyMatching = tree.GetBool(FUZZY_MATCHING);
        }




        /// <summary>
        /// Lazily Initialize the given Stall with the parent inventory as the provided stall slot. Does not instantiate the stall slots to prepare the stall for FromTreeAttributes or manual resolution
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="stallSlot"></param>
        public virtual void PreInitialize(VinconBaseInventory inventory, int stallSlot)
        {
            Inventory = inventory;
            StallSlot = stallSlot;
        }

        public virtual void Initialize(VinconBaseInventory vinconBaseInventory, int stallSlot, int numSlotsPerStall)
        {
            PreInitialize(vinconBaseInventory, stallSlot);
        }   

        public virtual ItemSlot GetStockSlot(int itemSlot)
        {
            ItemSlot[] slots = GetStockSlots();
            if (itemSlot < 0 || itemSlot >= slots.Length) throw new ArgumentOutOfRangeException($"Cannot get Product Slot {itemSlot} of stall with {slots.Length} slots");
            return slots[itemSlot];
        }

        public T GetStockSlot<T>(int itemSlot) where T : ItemSlot
        {
            return GetStockSlot(itemSlot) as T;
        }

        /// <summary>
        /// Returns the slots that are considered "Product" slots for this stall. These are the slots that the player can fill in to sell items from. In most cases the contents of these slots will match the Product slot.
        /// An exceptions would be the Sculpture stalls where the slots are used to hold the individual items that make up the final product, but the Product slot is a single item that represents the finished product.
        /// </summary>
        /// <returns></returns>
        public abstract ItemSlot[] GetStockSlots();

        /// <summary>
        /// Returns the slots that are considered "Internal" slots for this stall. These are slots that might be needed for the function of the stall (Such as the setting Gacha Contents) but not part of the stock that should be
        /// used as part of the product given to the player. These should typically not include the Currency or Product slots, as they have dedicated getters/setters.
        /// </summary>
        /// <returns></returns>
        public virtual ItemSlot[] GetInternalSlots()
        {
            return null;
        }


        public virtual bool MatchesProduct(ItemStack itemStack)
        {
            return TradingUtil.IsMatchingItem(Product?.Itemstack, itemStack, this.Inventory.Api.World, IsFuzzyMatching);
        }



        public virtual void SetStallFilter(Vintagestory.API.Common.Func<ItemSlot, bool> stallFilter)
        {
            Product.Filter = stallFilter;

            ItemSlot[] slots = GetStockSlots();
            foreach (var item in slots)
            {
                if (item is FilteredItemSlot)
                {
                    ((FilteredItemSlot)item).Filter = stallFilter;
                }
            }
        }

        public virtual void SetStallBackground(string background)
        {
            ItemSlot[] slots = GetStockSlots();
            Product.BackgroundIcon = background;
            foreach (var item in slots)
            {
                item.BackgroundIcon = background;
            }
        }

        public virtual void DropInventory(Vec3d pos, int maxStackSize, bool markDirty = false)
        {
            int i = 0;
            ItemSlot[] slots = GetStockSlots();
            foreach (var slot in slots)
            {
                if (maxStackSize > 0)
                {
                    while (slot.StackSize > 0)
                    {
                        ItemStack itemstack = slot.TakeOut(GameMath.Clamp(slot.StackSize, 1, maxStackSize));
                        this.Inventory.Api.World.SpawnItemEntity(itemstack, pos);
                    }
                }
                else
                {
                    this.Inventory.Api.World.SpawnItemEntity(slot.Itemstack, pos);
                }
                slot.Itemstack = null;
                i++;

                if (markDirty) slot.MarkDirty();
            }
        }



        public abstract AggregatedStacks ExtractProduct(int amount, int numPurchases, bool isAdminOwned);

        public virtual int GetTotalProductAvailable()
        {
            if (Product?.Itemstack == null) return 0;

            ItemSlot[] items = GetStockSlots();
            int amount = 0;
            foreach (ItemSlot item in items)
            {
                if (MatchesProduct(item.Itemstack))
                {
                    amount += item.StackSize;
                }
            }

            return amount;
        }

        public virtual int GetNumPurchasesRemaining()
        {
            return GetTotalProductAvailable() / ProductPerPurchase;
        }

        public virtual ItemStack GetOfferedProduct()
        {
            return Product?.Itemstack;
        }

        public virtual ItemStack GetRequiredCurrency()
        {
            return Currency?.Itemstack;
        }

        public virtual ICurrencySinkProvider GetCurrencySink(PurchaseRequest req)
        {
            ICurrencySinkProvider sink = this as ICurrencySinkProvider;
            if (sink != null)
            {
                return sink;
            }


            sink = req.SellingEntity.GetComponent<ICurrencySinkProvider>();
            if (sink != null)
            {
                return sink;
            }
            return req.ParentEntity?.GetComponent<IShopInventoryProvider>();
        }

        public virtual ICouponSinkProvider GetCouponSink(PurchaseRequest req)
        {
            ICouponSinkProvider sink = this as ICouponSinkProvider;
            if (sink != null)
            {
                return sink;
            }


            sink = req.SellingEntity.GetComponent<ICouponSinkProvider>();
            if (sink != null)
            {
                return sink;
            }
            return req.ParentEntity?.GetComponent<ICouponSinkProvider>();
        }

        public virtual PurchaseRequest CreatePurchaseRequest(IPlayer player, int numPurchases, IShopComponent shop, IStallComponent stall)
        {
            PurchaseRequest request = new PurchaseRequest(Inventory.Api, player);

            IOwnable ownable = stall.Ownable;
            ItemStack currencyStack = Currency.Itemstack;
            ItemStack productStack = Product.Itemstack;
            request.WithShop(shop);
            request.WithShopProduct(stall, StallSlot, ownable?.IsAdminOwned ?? false);
            request.WithPurchases(numPurchases);
            request.WithCurrencyFromCustomer();
            request.WithCouponsFromCustomer();
            request.WithRequiredTradePass();
            request.WithRequiredContainersFromCustomer();


            return request.Build();
        }

        public virtual int AddProductToSlot(IPlayer byPlayer, ItemSlot sourceSlot, bool bulk)
        {
            return AddProductToSlot(byPlayer, sourceSlot, bulk ? sourceSlot.StackSize : 1);
        }


        /// <summary>
        /// Adds the product from the specified stall slot and places it into the destination slot if it is provided.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="sourceSlot"></param>
        /// <returns>True if any product was successfully added to the stall slot, false otherwise</returns>        
        public virtual int AddProductToSlot(IPlayer byPlayer, ItemSlot sourceSlot, int amount)
        {
            if (!MatchesProduct(sourceSlot.Itemstack)) return 0;

            ItemSlot[] slots = GetStockSlots();

            int amountItem = amount;
            int movedItems = 0;

            foreach (var slot in slots)
            {
                if (sourceSlot.Itemstack != null)
                {
                    int moved = sourceSlot.TryPutInto(Inventory.Api.World, slot, amountItem);
                    amountItem -= moved;
                    if (moved > 0)
                    {
                        movedItems += moved;
                        sourceSlot.MarkDirty();
                        slot.MarkDirty();
                    }

                    if (amountItem <= 0)
                    {
                        break;
                    }
                }
            }
            return movedItems;
        }
    }
}
