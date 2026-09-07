using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.Slots;
using Commercially.Vinconomy.Trading;
using System;
using System.Collections.Generic;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Inventory.StallSlots
{
    public class GachaStallSlot : BaseStallSlot, IGeneratedProductStall
    {
        public override int InternalSlotCount => (GetInternalSlots()?.Length ?? 0) + 1; // + Product slot (Currency Slot is in the Inventory, not here)

        public override bool IsInitialized => Stock != null;

        public GachaProductSlot[] GachaContents;
        public ItemSlot[] Stock;
        public int Weight;

        // The currency will always be the same slot for the entire stall, since one of the slots will be selected at random to be the product.
        public override CurrencySlot Currency { get { return (CurrencySlot)Inventory.InternalSlots[0]; } protected set { Inventory.InternalSlots[0] = value; } }

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId == 0)
                    return Product;

                int index = slotId - 1;
                if (index < GachaContents?.Length)
                    return GachaContents[index];

                index -= GachaContents?.Length ?? 0;
                return Stock[index];

            }
            set
            {
                if (slotId == 0)
                    Product = (ProductSlot)value;

                int index = slotId - 1;
                if (index < GachaContents?.Length)
                    GachaContents[index] = (GachaProductSlot)value;

                index -= GachaContents?.Length ?? 0;
                Stock[index] = value;
            }
        }

        public GachaStallSlot(VinconBaseInventory inventory, int stallSlot, int contentSlots, int stockSlots) : base(inventory, stallSlot)
        {
            GachaContents = new GachaProductSlot[contentSlots];
            for (int i = 0; i < contentSlots; i++)
            {
                GachaContents[i] = new GachaProductSlot(inventory, stallSlot, i);
            }
            Stock = new StockItemSlot[stockSlots];
            for (int i = 0; i < stockSlots; i++)
            {
                Stock[i] = new GachaStockItemSlot(inventory, stallSlot, i);
            }

        }

        public override ItemSlot[] GetInternalSlots()
        {
            return GachaContents;
        }


        public override ItemSlot[] GetStockSlots()
        {
            return Stock;
        }

        public void SetGachaStackCount(int slot, int amount)
        {
            GachaContents[slot].Itemstack?.StackSize = amount;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            // We do NOT want Currency to tree, as these come from the Inventory itself.
            tree.SetItemstack(PRODUCT, Product.Itemstack);

            tree.SetInt("weight", Weight);
            tree.SetInt("numSlots", Stock.Length);
            for (int j = 0; j < Stock.Length; j++)
            {
                if (Stock[j].Itemstack != null)
                {
                    tree.SetItemstack("slot" + j, Stock[j].Itemstack);
                }
            }

            tree.SetInt("numGachaSlots", GachaContents.Length);
            for (int j = 0; j < GachaContents.Length; j++)
            {
                if (GachaContents[j].Itemstack != null)
                {
                    tree.SetItemstack("gachaSlot" + j, GachaContents[j].Itemstack);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            // We do NOT want Currency from tree, as these come from the Inventory itself.
            ItemStack productStack = tree.GetItemstack(PRODUCT);
            Product.Itemstack = productStack;
            if (Inventory.Api?.World != null)
            {
                productStack?.ResolveBlockOrItem(Inventory.Api.World);
            }

            int numSlots = tree.GetInt("numSlots");
            int numGachaSlots = tree.GetInt("numGachaSlots");
            Weight = Math.Max(1, tree.GetInt("weight", 1));

            if (!IsInitialized)
            {
                Stock = new ItemSlot[numGachaSlots];
                for (int i = 0; i < numGachaSlots; i++)
                {
                    GachaContents[i] = new GachaProductSlot(Inventory, StallSlot, i);
                    ItemStack itemStack = tree.GetItemstack("gachaSlot" + i);
                    GachaContents[i].Itemstack = itemStack;
                    if (Inventory.Api?.World != null)
                    {
                        itemStack?.ResolveBlockOrItem(Inventory.Api.World);
                    }
                }

                Stock = new ItemSlot[numSlots];
                for (int i = 0; i < numSlots; i++)
                {
                    Stock[i] = new StockItemSlot(Inventory, StallSlot, i);
                    ItemStack itemStack = tree.GetItemstack("slot" + i);
                    Stock[i].Itemstack = itemStack;
                    if (Inventory.Api?.World != null)
                    {
                        itemStack?.ResolveBlockOrItem(Inventory.Api.World);
                    }
                }
            }
            else
            {
                for (int i = 0; i < numGachaSlots; i++)
                {
                    ItemStack itemStack = tree.GetItemstack("gachaSlot" + i);
                    GachaContents[i].Itemstack = itemStack;
                    if (Inventory.Api?.World != null)
                    {
                        itemStack?.ResolveBlockOrItem(Inventory.Api.World);
                    }
                }

                for (int i = 0; i < numSlots; i++)
                {
                    ItemStack itemStack = tree.GetItemstack("slot" + i);
                    Stock[i].Itemstack = itemStack;
                    if (Inventory.Api?.World != null)
                    {
                        itemStack?.ResolveBlockOrItem(Inventory.Api.World);
                    }
                }
            }

        }

        public override void Initialize(VinconBaseInventory inventory, int stallSlot, int numSlotsPerStall)
        {
            base.Initialize(inventory, stallSlot, numSlotsPerStall);

            if (!IsInitialized)
            {
                Stock = new ItemSlot[numSlotsPerStall];
                for (int i = 0; i < numSlotsPerStall; i++)
                {
                    Stock[i] = new StockItemSlot(inventory, StallSlot, i); ;
                }
            }
        }

        public override AggregatedStacks ExtractProduct(int amount, int numPurchases, bool isAdminOwned)
        {
            List<ItemStack> neededItems = new List<ItemStack>(GachaContents.Length);
            //Check for duplicate items and combine them if players have 2 of the same items in 2 or more slots
            for (int i = 0; i < GachaContents.Length; i++)
            {
                ItemStack contents = GachaContents[i].Itemstack;
                if (contents == null) continue;

                ItemStack existing = null;
                foreach (ItemStack desired in neededItems)
                {
                    if (TradingUtil.IsMatchingItem(contents, desired, this.Inventory.Api.World, IsFuzzyMatching))
                    {
                        existing = desired;
                        break;
                    }
                }

                if (existing != null)
                {
                    existing.StackSize += contents.StackSize;
                }
                else
                {
                    neededItems.Add(contents);
                }
            }

            AggregatedStacks aggregatedStacks = new AggregatedStacks();
            for (int i = 0; i < amount; i++)
            {
                aggregatedStacks.Add(CondenseContentsToGacha(neededItems));
            }
            return aggregatedStacks;
        }

        public ItemStack CondenseContentsToGacha(List<ItemStack> neededItems)
        {
            List<ItemStack> removedItems = new List<ItemStack>(neededItems.Count);
            foreach (ItemStack desired in neededItems)
            {
                int numDesired = desired.StackSize;
                foreach (ItemSlot slot in Stock)
                {
                    if (TradingUtil.IsMatchingItem(slot.Itemstack, desired, this.Inventory.Api.World, IsFuzzyMatching))
                    {
                        ItemStack takenStack = slot.TakeOut(numDesired);
                        removedItems.Add(takenStack);
                        numDesired -= takenStack.StackSize;
                        if (numDesired <= 0) break;
                    }
                }

                if (numDesired > 0) {
                    throw new AggregateException($"Tried to condense a Gacha Ball with {desired.StackSize} {desired.GetName()} but wound up being {numDesired} short");
                }
            }

            ItemStack ball = GenStubbedProduct();
            ball.Attributes["Contents"] = SetGachaContents(removedItems);
            return ball;
        }

        public override int GetTotalProductAvailable()
        {


            List<ItemStack> neededItems = new List<ItemStack>(GachaContents.Length);
            //Check for duplicate items and combine them if players have 2 of the same items in 2 or more slots
            for (int i = 0; i < GachaContents.Length; i++)
            {
                ItemStack contents = GachaContents[i].Itemstack;
                if (contents == null) continue;

                ItemStack existing = null;
                foreach (ItemStack desired in neededItems)
                {
                    if (TradingUtil.IsMatchingItem(contents, desired, this.Inventory.Api.World, IsFuzzyMatching))
                    {
                        existing = desired;
                        break;
                    }
                }

                if (existing != null)
                {
                    existing.StackSize += contents.StackSize;
                }
                else
                {
                    neededItems.Add(contents);
                }
            }

            if (neededItems.Count == 0) return 0;

            int amount = Int32.MaxValue;
            // Once we have all of the condensed items, check if we have enough for a trade
            foreach (ItemStack desired in neededItems)
            {
                amount = Math.Min(amount, GetGachaContentQuantity(desired));
            }

            return amount;
        }

        private int GetGachaContentQuantity(ItemStack contents)
        {
            int amount = 0;
            
            if (contents == null) return 0;

            foreach (ItemSlot item in Stock)
            {
                if (TradingUtil.IsMatchingItem(contents, item.Itemstack, this.Inventory.Api.World, IsFuzzyMatching))
                {
                    amount += item.Itemstack.StackSize;
                }
            }
            return amount / contents.StackSize;
        }

        public int GetStallWeight()
        {
            int productCount = GetTotalProductAvailable();

            if (productCount <= 0)
                return 0;

            GachaShopInventory gachaInventory = Inventory as GachaShopInventory;
            if (gachaInventory?.IsCountBasedRandomizer == true)
            {
                return productCount * Weight;
            }

            return Weight;
        }

        public ItemStack GenStubbedProduct()
        {
            ItemStack stack = new ItemStack(Inventory.Api.World.GetItem(new AssetLocation("vinconomy:gachaball")), 1);          
            return stack;
        }

        public ItemStack GenProduct()
        {
            ItemStack stack = GenStubbedProduct();

            TreeAttribute contents = new TreeAttribute();

            //ITreeAttribute contents = (TreeAttribute)treeAttr.GetTreeAttribute("Contents");
            int numItems = 0;
            foreach (var itemSlot in GachaContents)
            {
                if (itemSlot.Itemstack != null)
                {
                    contents.SetItemstack("Item" + numItems, itemSlot.Itemstack);
                    numItems++;
                }
            }
            TreeAttribute treeAttr = stack.Attributes as TreeAttribute;

            contents.SetLong("NumContents", numItems);
            treeAttr.SetAttribute("Contents", contents);
            return stack;
        }

        private TreeAttribute SetGachaContents(List<ItemStack> gachaContents)
        {
            TreeAttribute contents = new TreeAttribute();
            int numItems = 0;
            foreach (var itemStack in gachaContents)
            {
                if (itemStack != null)
                {
                    contents.SetItemstack("Item" + numItems, itemStack);
                    numItems++;
                }
            }

            contents.SetLong("NumContents", numItems);
            return contents;
        }

        public void RegenProduct()
        {
            int slot = StallSlot;
            if (GetTotalProductAvailable() == 0)
            {
                Product.Itemstack = null;
            } else
            {
                Product.Itemstack = GenProduct();
            }
            
            Product.MarkDirty();
        }

        /*
        public override TradeRequest CreateTradeRequest(IPlayer player, int numPurchases, IShopComponent shop, IStallComponent stall)
        {
            TradeRequest request = new TradeRequest(Inventory.Api, player);
            IOwnable ownable = stall.Ownable;
            ItemStack currencyStack = Currency.Itemstack;
            ItemStack productStack = Product.Itemstack;
            request.WithShop(shop, stall, StallSlot, ownable?.IsAdminOwned ?? false);
            request.WithPurchases(numPurchases);
            request.WithCurrency(currencyStack, TradingUtil.GetAllValidSlotsFor(player, currencyStack), currencyStack.StackSize);

            AggregatedSlots slots = new AggregatedSlots(Inventory.Api);
            int quantity = GetProductQuantity();
            for (int i = 0; i < quantity; i++)
            {
                slots.Add(productStack.Clone());   
            }


            request.WithProduct(productStack, GetProducts(), productStack.StackSize);

            AggregatedSlots coupons = TradingUtil.GetCouponsSlotsFor(player, request.ProductNeeded, shop);
            if (coupons.Slots.Count > 0)
            {
                request.WithCoupons(coupons.Slots[0]);
            }

            request.WithContainers(GetRequiredContainers(player));

            if (shop != null)
            {
                ITradePassProvider inv = shop.GetComponent<IInventoryProvider>()?.Inventory as ITradePassProvider;
                if (inv != null)
                {
                    ItemStack tradePass = inv.GetTradePass();
                    if (tradePass != null)
                    {
                        request.WithTradePass(tradePass, TradingUtil.GetAllValidSlotsFor(player, tradePass));
                    }
                }
            }
            return request.Build();
        }
        */

    }
}
