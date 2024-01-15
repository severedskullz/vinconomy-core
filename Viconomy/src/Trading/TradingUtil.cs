
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.src.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Viconomy.Trading
{
    public class TradingUtil
    {
        public static ItemStack GetChangeFor(ItemStack inputCurrency, ViconRegister register)
        {
            string owner = register.Owner;
            return null;
        }

        public static List<CurrencyConversion> GetConversionsFor(string itemName)
        {
            return null;
        }

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
            }
           

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
                purchaseResult.shopRegister.PurchasedItem(purchaseResult.customer, purchaseResult.sellingEntity, productStackClone, paymentStack);
                purchaseResult.shopRegister.AddItem(paymentStack, paymentStack.StackSize);
            }

            purchaseResult.purchasedItems = productStackClone;
            purchaseResult.purchasedCurrencyUsed = paymentStackClone;

            ViconomyCore core = purchaseResult.coreApi.ModLoader.GetModSystem<ViconomyCore>();
            core.PurchasedItem(purchaseResult.customer, purchaseResult.sellingEntity, purchaseResult.shopRegister, productStackClone, paymentStack);

            // Give the product to the player
            GivePlayerProduct(purchaseResult, soldItems);

            //Tell the player they purchased items
            ViconomyCore.PrintClientMessage(purchaseResult.customer, TradingConstants.PURCHASED_ITEMS, new object[] { 
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
                result.customer.InventoryManager.TryGiveItemstack(item, false);
                if (item.StackSize > 0)
                {
                    result.coreApi.World.SpawnItemEntity(item, result.sellingEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
                }
            }
          
            Block block = productStacks[0].Block;
            AssetLocation assetLocation;
            if (block == null)
            {
                assetLocation = null;
            }
            else
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
                        if (itemSlot.Itemstack.Satisfies(request.currencyNeeded))
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

            List<ItemSlot> validCurrency = GetAllValidCurrencyFor(request.customer, request.currencyNeeded);

            // Count the amount of currency that the player has to see if it covers the cost
            int totalCurrency = 0;
            foreach (var currencySlot in validCurrency)
            {
                int currAmount = currencySlot.Itemstack.StackSize;
                if (populatePurchaseResult)
                {
                    purchaseResult.currencySourceSlots.Add(currencySlot);
                }
                totalCurrency += currAmount;
            }

            // If they havent covered the cost, tell them they dont have enough money
            if (totalCurrency < currencyRequired)
            {
                if (populatePurchaseResult)
                {
                    purchaseResult.error = TradingConstants.NOT_ENOUGH_MONEY;
                }

                //TODO: Auto Currency Conversion!
                return false;
            }
            // Set the slots applicable for payment, override the amount we are trying to purhcase, and set it on the result too
            request.numPurchases = Math.Min(request.numPurchases, totalCurrency / currencyRequired);
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
                purchaseResult.currencySourceSlots = validCurrency;
                purchaseResult.numPurchases = request.numPurchases;
            }
            return true;
        }

        public static List<ItemSlot> GetAllValidCurrencyFor(IPlayer customer, ItemSlot currency)
        {
            return GetAllValidCurrencyFor(customer, currency.Itemstack);
        }

        public static List<ItemSlot> GetAllValidCurrencyFor(IPlayer customer, ItemStack currency)
        {
            List<ItemSlot> validSlots = new List<ItemSlot>();

            ItemSlot handItem = customer.InventoryManager.ActiveHotbarSlot;
            if (isMatchingCurrency(currency, handItem.Itemstack))
            {
                validSlots.Add(handItem);
            }

            IInventory hotbarInv = customer.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (isMatchingCurrency(currency, itemSlot.Itemstack))
                {
                    validSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = customer.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (isMatchingCurrency(currency, itemSlot.Itemstack))
                {
                    validSlots.Add(itemSlot);
                }
            }
            return validSlots;
        }

        public static bool isMatchingCurrency(ItemStack source, ItemStack payment)
        {
            return source != null 
                && payment != null 
                && payment.Satisfies(source);
        }
        public static bool isMatchingCurrency(ItemSlot source, ItemSlot payment)
        {
            return source != null
                && payment != null
                && isMatchingCurrency(source.Itemstack, payment.Itemstack);
        }

        public static ItemStack GetItemStackClone(ItemSlot slot, int stackSize = 0)
        {
            ItemStack stack = null;
            if (slot == null) return null;

            stack = slot.Itemstack.Clone();
            if (stackSize > 0)
            {
                stack.StackSize = stackSize;
            }
            return stack;
        }
    }
}
