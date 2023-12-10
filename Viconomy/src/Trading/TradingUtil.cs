
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

        public static void CommitPurchase(PurchaseResult purchaseResult)
        {
            // Take the product from the stall
            int productLeft = purchaseResult.numPurchases * purchaseResult.stallSlot.itemsPerPurchase;
            ItemStack productStackClone = purchaseResult.stallSlot.FindFirstNonEmptyStockSlot().Itemstack.Clone();
            productStackClone.StackSize = productLeft;
            List<ItemStack> soldItems = new List<ItemStack>();
            if (purchaseResult.stall.isAdminShip)
            {
                soldItems.Add(productStackClone.Clone());
            } else {
                
                foreach (ItemSlot itemSlot in purchaseResult.stallSlot.slots)
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
            int currencyLeft = purchaseResult.numPurchases * purchaseResult.stallSlot.currency.StackSize;
            foreach (ItemSlot itemSlot in purchaseResult.paymentSlots)
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
                purchaseResult.shopRegister.PurchasedItem(purchaseResult.customer, purchaseResult.stall, productStackClone, paymentStack);
                purchaseResult.shopRegister.AddItem(paymentStack, paymentStack.StackSize);
            }

            purchaseResult.purchasedItems = productStackClone;
            purchaseResult.purchasedCurrencyUsed = paymentStackClone;

            // Give the product to the player
            GivePlayerProduct(purchaseResult, soldItems);

            ViconomyCore.PrintClientMessage(purchaseResult.customer, TradingConstants.PURCHASED_ITEMS, new object[] { 
                productStackClone.StackSize, 
                productStackClone.GetName(),
                paymentStackClone.StackSize,
                paymentStackClone.GetName()
            });


        }

        private static void GivePlayerProduct(PurchaseResult result, List<ItemStack> productStacks)
        {
            if (productStacks.Count == 0) return;

            foreach (ItemStack item in productStacks)
            {
                result.customer.InventoryManager.TryGiveItemstack(item, false);
                if (item.StackSize > 0)
                {
                    result.coreApi.World.SpawnItemEntity(item, result.stall.Pos.ToVec3d().Add(0.5, 0.5, 0.5), null);
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


        public static PurchaseResult TryPurchaseItem(PurchaseRequest request)
        {
            PurchaseResult purchaseResult = new PurchaseResult(request);
            if (request.stallSlot.currency.Itemstack == null)
            {
                purchaseResult.error = TradingConstants.NO_PRICE;
                return purchaseResult;
            }

            if (request.stallSlot.FindFirstNonEmptyStockSlot() == null)
            {
                purchaseResult.error = TradingConstants.NO_PRODUCT;
                return purchaseResult;
            }



            if (CanAfford(request, purchaseResult) && CanSell(request, purchaseResult))
            {
                CommitPurchase(purchaseResult);
            }



            return purchaseResult;

        }
        public static bool CanSell(PurchaseRequest request, PurchaseResult purchaseResult)
        {
            int quantityNeeded = request.stallSlot.itemsPerPurchase * request.numTryPurchase;
            int totalItemsForSale = 0;
            foreach (ItemSlot slot in request.stallSlot.slots)
            {
                totalItemsForSale += slot.StackSize;
            }
            // Has enough stock
            if (totalItemsForSale < request.stallSlot.itemsPerPurchase)
            {
                purchaseResult.error = TradingConstants.NOT_ENOUGH_STOCK;
                return false;
            } else if (totalItemsForSale < quantityNeeded)
            {
                request.numTryPurchase = Math.Min(request.numTryPurchase, totalItemsForSale / request.stallSlot.itemsPerPurchase);

            }

            // Has enough space in register
            if (request.shopRegister != null)
            {
                int maxStackSize = request.stallSlot.currency.Itemstack.Collectible.MaxStackSize;
                int qntyLeft = request.stallSlot.currency.StackSize * request.numTryPurchase;
                IInventory inv = request.shopRegister.Inventory;
                foreach (ItemSlot itemSlot in inv)
                {
                    if (itemSlot.Itemstack == null)
                    {
                        qntyLeft -= maxStackSize;
                    }
                    else
                    {
                        if (itemSlot.Itemstack.Satisfies(request.stallSlot.currency.Itemstack))
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

        public static bool CanAfford(PurchaseRequest request, PurchaseResult purchaseResult)
        {
            bool populatePurchaseResult = purchaseResult != null;
            int currencyRequired = request.stallSlot.currency.Itemstack.StackSize;

            //TODO: Figure out a way to give discounts to the player for groups. This is a placeholder for now
            if (request.discountedPrice != 0)
            {
                currencyRequired = request.discountedPrice;
            }
            //int totalCurrencyRequired = request.numTryPurchase * currencyRequired;

            List<ItemSlot> validCurrency = GetAllValidCurrencyFor(request.customer, request.stallSlot.currency);

            // Count the amount of currency that the player has to see if it covers the cost
            int totalCurrency = 0;            
            foreach (var currencySlot in validCurrency)
            {
                int currAmount = currencySlot.Itemstack.StackSize;
                if (populatePurchaseResult)
                {
                    purchaseResult.paymentSlots.Add(currencySlot);
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
            request.numTryPurchase = Math.Min(request.numTryPurchase, totalCurrency / currencyRequired);
            if (populatePurchaseResult)
            {
                purchaseResult.paymentSlots = validCurrency;
                purchaseResult.numPurchases = request.numTryPurchase;
            }
            return true;
        }

        private static List<ItemSlot> GetAllValidCurrencyFor(IPlayer customer, ViconCurrencySlot currency)
        {
            List<ItemSlot> validSlots = new List<ItemSlot>();

            ItemSlot handItem = customer.InventoryManager.ActiveHotbarSlot;
            if (isMatchingCurrency(currency, handItem))
            {
                validSlots.Add(handItem);
            }

            IInventory hotbarInv = customer.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (isMatchingCurrency(currency, itemSlot))
                {
                    validSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = customer.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (isMatchingCurrency(currency, itemSlot))
                {
                    validSlots.Add(itemSlot);
                }
            }
            return validSlots;
        }

        private static bool isMatchingCurrency(ItemSlot source, ItemSlot payment)
        {
            return source != null && source.Itemstack != null 
                && payment != null && payment.Itemstack != null 
                && payment.Itemstack.Satisfies(source.Itemstack);
        }
    }
}
