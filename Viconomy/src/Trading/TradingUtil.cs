using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Trading
{
    public class TradingUtil
    {
        public static void CommitPurchase(TradeResult purchaseResult)
        {
            // Take the product from the stall
            int productLeft = purchaseResult.numPurchases * purchaseResult.productNeeded.StackSize;
            ItemStack productStackClone = purchaseResult.productNeeded.Clone();
            productStackClone.StackSize = productLeft;
            List<ItemStack> soldItems = new List<ItemStack>();
            if (purchaseResult.isAdminShop)
            {
                soldItems.Add(productStackClone.Clone());
            } else {
                
                foreach (ItemSlot itemSlot in purchaseResult.productSourceSlots)
                {
                    if (itemSlot.Itemstack == null) continue;
                    ItemStack takenStack = itemSlot.TakeOut(productLeft);
                    productLeft -= takenStack.StackSize;
                    soldItems.Add(takenStack);
                    itemSlot.MarkDirty();

                    if (productLeft <= 0) break;
                }

                if (productLeft > 0)
                {
                    //IDK Do something as an error?
                }
            }

            /*
            //Take the tool from the player, if needed
            if (purchaseResult.requiredToolType != ToolType.NONE)
            {
                if (purchaseResult.shouldConsumeTool)
                {
                    purchaseResult.tool.TakeOut(1);
                    purchaseResult.tool.MarkDirty();
                }
            }
           */

            //Take the money from the player.
            ItemStack paymentStack = null;
            int currencyLeft = purchaseResult.numPurchases * purchaseResult.currencyNeeded.StackSize;
            foreach (ItemSlot itemSlot in purchaseResult.currencySourceSlots)
            {
                if (paymentStack == null)
                {
                    paymentStack = itemSlot.TakeOut(currencyLeft);
                    currencyLeft -= paymentStack.StackSize;
                } else
                {
                    ItemStack takenStack = itemSlot.TakeOut(currencyLeft);
                    currencyLeft -= takenStack.StackSize;
                    paymentStack.StackSize += takenStack.StackSize;
                }
                itemSlot.MarkDirty();
                if (currencyLeft <= 0) break;
            }

            ItemStack paymentStackClone = paymentStack.Clone();
            // Add the payment to the register
            if (purchaseResult.shopRegister != null)
            {
                purchaseResult.shopRegister.AddItem(paymentStack, paymentStack.StackSize);
            }

            purchaseResult.purchasedItems = productStackClone;
            purchaseResult.purchasedCurrencyUsed = paymentStackClone;

            VinconomyCoreSystem core = purchaseResult.coreApi.ModLoader.GetModSystem<VinconomyCoreSystem>();
            core.PurchasedItem(purchaseResult, productStackClone, paymentStackClone);

            // Give the product to the player
            GivePlayerProduct(purchaseResult, soldItems);

            //Tell the player they purchased items
            VinconomyCoreSystem.PrintClientMessage(purchaseResult.customer, TradingConstants.PURCHASED_ITEMS, new object[] { 
                productStackClone.StackSize, 
                productStackClone.GetName(),
                paymentStackClone.StackSize,
                paymentStackClone.GetName()
            });


        }

        private static void GivePlayerProduct(TradeResult result, List<ItemStack> productStacks)
        {
            if (productStacks.Count == 0) return;

            foreach (ItemStack item in productStacks)
            {
                result.customer.InventoryManager.TryGiveItemstack(item, true);
                if (item.StackSize > 0)
                {
                    result.coreApi.World.SpawnItemEntity(item, result.sellingEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
                }
            }
          
            Block block = productStacks[0].Block;
            AssetLocation assetLocation = null;
            if (block != null)
            {
                BlockSounds sounds = block.Sounds;
                assetLocation = ((sounds != null) ? sounds.Place : null);
            }
            AssetLocation sound = assetLocation;
            result.coreApi.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), result.customer.Entity, result.customer, true, 16f, 1f);
        }

        public static TradeResult TryPurchaseItem(TradeRequest request)
        {
            TradeResult purchaseResult = new TradeResult(request);
            if (request.currencyNeeded == null)
            {
                purchaseResult.error = TradingConstants.NO_PRICE;
                return purchaseResult;
            }

            if (!request.isAdminShop)
            {
                bool hasAtleastOne = false;
                foreach (ItemSlot productSlot in request.productSourceSlots)
                {
                    if (productSlot.Itemstack != null)
                    {
                        hasAtleastOne = true;
                        break;
                    }
                }
                if (!hasAtleastOne)
                {
                    purchaseResult.error = TradingConstants.NO_PRODUCT;
                    return purchaseResult;
                }
            }

            /*
            if (request.requiredToolType != ToolType.NONE && request.tool != null)
            {
                purchaseResult.error = TradingConstants.NO_TOOL;
                return purchaseResult;
            }
            */

            if (CanAfford(request, purchaseResult) && CanSell(request, purchaseResult))
            {
                CommitPurchase(purchaseResult);
            }

            return purchaseResult;

        }

        public static bool CanSell(TradeRequest request, TradeResult purchaseResult)
        {
            if (!request.isAdminShop) { 
                int quantityNeeded = request.productNeeded.StackSize * request.numPurchases;
                int totalItemsForSale = 0;
                foreach (ItemSlot slot in request.productSourceSlots)
                {
                    totalItemsForSale += slot.StackSize;
                }
                // Has enough stock
                if (totalItemsForSale < request.productNeeded.StackSize)
                {
                    purchaseResult.error = TradingConstants.NOT_ENOUGH_STOCK;
                    return false;
                } else if (totalItemsForSale < quantityNeeded)
                {
                    request.numPurchases = Math.Min(request.numPurchases, totalItemsForSale / request.productNeeded.StackSize);

                }
            }

            // Has enough space in register
            if (request.shopRegister != null)
            {
                int maxStackSize = request.currencyNeeded.Collectible.MaxStackSize;
                int qntyLeft = request.currencyNeeded.StackSize * request.numPurchases;
                IInventory inv = request.shopRegister.Inventory;
                foreach (ItemSlot itemSlot in inv)
                {
                    if (itemSlot.Itemstack == null)
                    {
                        qntyLeft -= maxStackSize;
                    }
                    else
                    {
                        if (isMatchingItem(itemSlot.Itemstack, request.currencyNeeded, request.coreApi.World))
                        {
                            qntyLeft -= maxStackSize - itemSlot.StackSize;
                        }
                    }

                    if (qntyLeft <= 0)
                    {
                        break;
                    }
                }

                if (qntyLeft > 0)
                {
                    purchaseResult.error = TradingConstants.NO_REGISTER_SPACE;
                    return false;
                }

            }
           

            return true;
        }

        public static bool CanAfford(IPlayer player, ItemStack currencyNeeded)
        {
            AggregatedSlots validCurrency = GetAllValidSlotsFor(player, currencyNeeded);
            return validCurrency.TotalCount < currencyNeeded.StackSize; ;
        }

        public static bool CanAfford(IPlayer player, ItemStack currencyNeeded, TradeResult purchaseResult = null)
        {
            AggregatedSlots validCurrency = GetAllValidSlotsFor(player, currencyNeeded);
            if (purchaseResult != null)
            {
                purchaseResult.currencySourceSlots = validCurrency.Slots;
            }

            // If they havent covered the cost, tell them they dont have enough money
            if (validCurrency.TotalCount < currencyNeeded.StackSize)
            {
                if (purchaseResult != null)
                {
                    purchaseResult.error = TradingConstants.NOT_ENOUGH_MONEY;
                }

                //TODO: Auto Currency Conversion!
                return false;
            }
            return true;
        }


        public static bool CanAfford(TradeRequest request, TradeResult purchaseResult)
        {
            bool populatePurchaseResult = purchaseResult != null;
            int currencyRequired = request.currencyNeeded.StackSize;

            //TODO: Figure out a way to give discounts to the player for groups. This is a placeholder for now
            if (request.discountedPrice != 0)
            {
                currencyRequired = request.discountedPrice;
            }
            //int totalCurrencyRequired = request.numTryPurchase * currencyRequired;


            // Count the amount of currency that the player has to see if it covers the cost
            AggregatedSlots validCurrency = GetAllValidSlotsFor(request.customer, request.currencyNeeded);
            if (populatePurchaseResult)
            {
                purchaseResult.currencySourceSlots = validCurrency.Slots;
            }

            

            // If they havent covered the cost, tell them they dont have enough money
            if (validCurrency.TotalCount < currencyRequired)
            {
                if (populatePurchaseResult)
                {
                    purchaseResult.error = TradingConstants.NOT_ENOUGH_MONEY;
                }

                //TODO: Auto Currency Conversion!
                return false;
            }
            // Set the slots applicable for payment, override the amount we are trying to purchase, and set it on the result too
            request.numPurchases = Math.Min(request.numPurchases, validCurrency.TotalCount / currencyRequired);
            int totalStock = 0;
            foreach (ItemSlot slot in request.productSourceSlots)
            {
                if (slot.Itemstack != null)
                {
                    totalStock += slot.Itemstack.StackSize;
                }
            }
            request.numPurchases = Math.Min(request.numPurchases, totalStock / request.productNeeded.StackSize);
            if (populatePurchaseResult)
            {
                purchaseResult.currencySourceSlots = validCurrency.Slots;
                purchaseResult.numPurchases = request.numPurchases;
            }
            return true;
        }

        public static AggregatedSlots GetAllValidSlotsFor(IPlayer customer, ItemSlot desiredItem)
        {
            return GetAllValidSlotsFor(customer, desiredItem.Itemstack);
        }

        public static AggregatedSlots GetAllValidSlotsFor(IPlayer customer, ItemStack desiredItem)
        {
            AggregatedSlots aggregatedSlots = new AggregatedSlots();
            if (desiredItem == null)
            {
                return aggregatedSlots;
            }

            ItemSlot handItem = customer.InventoryManager.ActiveHotbarSlot;
            if (isMatchingItem(desiredItem, handItem.Itemstack, customer.Entity.World))
            {
                aggregatedSlots.Add(handItem);
            }

            IInventory hotbarInv = customer.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (isMatchingItem(desiredItem, itemSlot.Itemstack, customer.Entity.World))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = customer.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (isMatchingItem(desiredItem, itemSlot.Itemstack, customer.Entity.World))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }
            return aggregatedSlots;
        }

        public static bool isMatchingItem(ItemStack source, ItemStack payment, IWorldAccessor world)
        {
            return source != null
                && payment != null
                && source.Equals(world, payment, new string[] { "transitionstate", "temperature" });
        }


        public static ItemStack GetItemStackClone(ItemStack stack, int stackSize = 0)
        {
            if (stack == null) return null;

            ItemStack newStack = stack.Clone();
            if (stackSize > 0)
            {
                newStack.StackSize = stackSize;
            }
            return newStack;
        }

        public static ItemStack GetItemStackClone(ItemSlot slot, int stackSize = 0)
        {
            if (slot?.Itemstack == null ) return null;

            ItemStack stack = slot.Itemstack.Clone();
            if (stackSize > 0)
            {
                stack.StackSize = stackSize;
            }
            return stack;
        }
    }


    //TODO: Figure out how to get durabilty for tools. For now we can just manually set it
    public class DurabilityAggregatedSlots : AggregatedSlots
    {
        public int TotalDurability { get; set; }
        public override void Add(ItemSlot item)
        {
            Slots.Add(item);
            TotalCount += item.StackSize;
            //TotalDurability += item.Durability ????
        }
    }

    public class ServingCapacityAggregatedSlots : AggregatedSlots
    {
        public int TotalCapacity { get; set; }
        public override void Add(ItemSlot item)
        {
            Slots.Add(item);
            TotalCount += item.StackSize;

            IBlockMealContainer meal = item.Itemstack.Block as IBlockMealContainer;
            int curServings = (int) Math.Ceiling(meal.GetQuantityServings(Api.World, item.Itemstack)); //We assume it can merge, anyway...
            JsonObject attr = item.Itemstack.Block.Attributes;

            int capacity = 0;
            if (attr.KeyExists("servingCapacity"))
            {
                capacity = attr["servingCapacity"].AsInt();
            }
            TotalCapacity += Math.Max(0, capacity - curServings);
        }
    }

    public class MealAggregatedSlots : AggregatedSlots
    {
        public override void Add(ItemSlot item)
        {
            Slots.Add(item);
            if (Slots.Count == 0)
            {
                TotalCount = item.StackSize;
            } else
            {
                TotalCount = Math.Min(TotalCount, item.StackSize);
            }
        }
    }

    public class AggregatedSlots
    {
        public ICoreAPI Api; // This is rediculous Tyron - just to get meal contents?

        public List<ItemSlot> Slots { get; set; } = new List<ItemSlot>();
        public int TotalCount { get; set; }

        public virtual void Add(ItemSlot item)
        {
            Slots.Add(item);
            TotalCount += item.StackSize;
        }
    }
}
