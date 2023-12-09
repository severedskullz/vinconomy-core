
using System;
using System.Collections.Generic;
using Viconomy.Inventory;
using Viconomy.Registry;
using Viconomy.src.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Viconomy.Trading
{
    public class TradingUtil
    {
        public ItemStack GetChangeFor(ItemStack inputCurrency, ViconRegister register)
        {
            string owner = register.Owner;
            return null;
        }

        public List<CurrencyConversion> GetConversionsFor(string itemName)
        {
            return null;
        }

        public void CommitPurchase(PurchaseResult purchaseResult)
        {

        }


        public PurchaseResult TryPurchaseItem(PurchaseRequest request)
        {
            PurchaseResult purchaseResult = new PurchaseResult();
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
        public bool CanSell(PurchaseRequest request, PurchaseResult purchaseResult)
        {
            int quantityNeeded = request.stallSlot.itemsPerPurchase * request.numTryPurchase;
            int totalItemsForSale = 0;
            foreach (ItemSlot slot in request.stallSlot.slots)
            {
                totalItemsForSale += slot.StackSize;
            }
            // Has enough stock
            if (totalItemsForSale < quantityNeeded)
            {
                purchaseResult.error = TradingConstants.NOT_ENOUGH_STOCK;
                return false;
            }

            // Has enough space in register
            if (request.shopRegister != null)
            {
                int maxStackSize = request.stallSlot.currency.MaxSlotStackSize;
                int qntyLeft = request.stallSlot.currency.StackSize * request.numTryPurchase;
                IInventory inv = request.shopRegister.Inventory;
                foreach (ItemSlot itemSlot in inv)
                {
                    if (itemSlot.Itemstack == null)
                    {
                        qntyLeft -= maxStackSize;
                    } else if (itemSlot.Itemstack.Equals(request.stallSlot.currency))
                    {
                        qntyLeft -= maxStackSize - itemSlot.StackSize;
                    }

                    if (qntyLeft < 0)
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

        public bool CanAfford(PurchaseRequest request, PurchaseResult purchaseResult)
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

        private List<ItemSlot> GetAllValidCurrencyFor(IPlayer customer, ViconCurrencySlot currency)
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
                if (handItem == itemSlot) { continue; }
                if (isMatchingCurrency(currency, itemSlot))
                {
                    validSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = customer.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
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

        private bool isMatchingCurrency(ItemSlot source, ItemSlot payment)
        {
            return source != null && source.Itemstack != null && payment.Itemstack.Satisfies(source.Itemstack) && payment.StackSize >= source.StackSize;
        }
    }
}
