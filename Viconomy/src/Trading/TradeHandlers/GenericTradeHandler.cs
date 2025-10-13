using System;
using Vintagestory.API.Common;

namespace Viconomy.Trading.TradeHandlers
{
    public static class GenericTradeHandler
    {
        public static bool HasRequiredTools(GenericTradeRequest req)
        {
            if (req.ToolSourceSlots == null)
            {
                return true;
            }
            //TODO: Possibly switch this from specific "total" variables to just use TotalCount instead?
            else if (req.ToolSourceSlots is ServingCapacityAggregatedSlots servings) {
                return servings.TotalCapacity / req.GetFinalProductNeededPerPurchase() > 0;
            } else if (req.ToolSourceSlots is DurabilityAggregatedSlots durability)
            {
                return durability.TotalDurability / req.GetFinalProductNeededPerPurchase() > 0;
            } else
            {
                return req.ToolSourceSlots.TotalCount / req.GetFinalProductNeededPerPurchase() > 0;
            }
        }

        public static bool StallHasRequiredProduct(GenericTradeRequest req)
        {
            if (req.ProductSourceSlots != null)
                return req.ProductSourceSlots.TotalCount / req.GetFinalProductNeededPerPurchase() > 0;
            return false;
        }

        public static bool PlayerHasRequiredCurrency(GenericTradeRequest req)
        {
            if (req.GetFinalCurrencyNeededPerPurchase() == 0)
                return true;

            if (req.CurrencySourceSlots != null)
                return req.CurrencySourceSlots.TotalCount / req.GetFinalCurrencyNeededPerPurchase() > 0;
            return false;
        }

        public static bool HasRequiredTradePass(GenericTradeRequest req)
        {
            return !(req.TradePassNeeded != null && req.TradePassSlots.TotalCount <= 0);
        }

        public static GenericTradeResult TryPurchaseItem(GenericTradeRequest request)
        {
            bool isAdminShop = request.SellingEntity.IsAdminShop;

            VinconomyCoreSystem core = request.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            GenericTradeResult res = new GenericTradeResult(request, core);
            if (request.ShopRegister == null && !isAdminShop)
                return SetErrorAndReturn(res, TradingConstants.NOT_REGISTERED);

            if (request.CurrencyStackNeeded == null)
                return SetErrorAndReturn(res, TradingConstants.NO_PRICE);

            if (request.ProductStackNeeded == null)
                return SetErrorAndReturn(res, TradingConstants.NO_PRODUCT);

            if (!isAdminShop && !StallHasRequiredProduct(request))
                return SetErrorAndReturn(res, TradingConstants.NOT_ENOUGH_STOCK);

            if (!PlayerHasRequiredCurrency(request))
                return SetErrorAndReturn(res, TradingConstants.NOT_ENOUGH_MONEY);

            if (request.NumPurchases <= 0)
                return SetErrorAndReturn(res, TradingConstants.PURCHASED_ZERO);

            if (!HasRequiredTools(request))
                return SetErrorAndReturn(res, TradingConstants.NO_TOOL);

            if (!HasRequiredTradePass(request))
                return SetErrorAndReturn(res, TradingConstants.NO_PASS);

            if (request.ShopRegister != null && !CanFitPaymentInRegister(request))
                return SetErrorAndReturn(res, TradingConstants.NO_REGISTER_SPACE);

            // Extract all relevant items from their containers
            TryConsumeTools(res);
            TryConsumeCoupons(res);
            TryExtractProductSlots(res);
            TryExtractCurrencySlots(res);


            // Send TradeResult to mod system to take taxes, log sale in ledger, etc.
            core.PurchasedItem(res);

            // Add payment to stall and give player product.
            if (res.Request.ShopRegister != null)
            {
                TryAddPaymentToRegister(res);
                TryAddCouponsToRegister(res);
            }

            TryAddProductToPlayer(res);



            VinconomyCoreSystem.PrintClientMessage(res.Request.Customer, TradingConstants.PURCHASED_ITEMS, new object[] {
                res.TransferedProductTotal,
                res.Request.ProductStackNeeded.GetName(),
                res.TransferedCurrencyTotal,
                res.Request.CurrencyStackNeeded.GetName()
            });

            return res;
        }

        public static void TryAddCouponsToRegister(GenericTradeResult res)
        {
            if (res.Request.SellingEntity.IsAdminShop && res.Request.SellingEntity.DiscardProduct)
                return;

            // Add the payment to the register
            if (res.Request.ShopRegister != null)
            {
                foreach (ItemStack couponStack in res.TransferedCoupons)
                {
                    if (!res.Request.ShopRegister.AddItem(couponStack, couponStack.StackSize))
                    {
                        AuditLogError(res, $"Failed to add coupons of {couponStack.StackSize} {couponStack.GetName()} to Shop Register");
                    }
                }
            }
            else
            {
                AuditLogError(res, $"Failed to add coupons to Register. ShopRegister was null");
            }
        }

        public static void TryAddProductToPlayer(GenericTradeResult res)
        {
            if (res.TransferedProduct?.Count == 0)
            {
                AuditLogError(res, "Tried to give player products, but has nothing to give");
            }

            IPlayer customer = res.Request.Customer;

            foreach (ItemStack item in res.TransferedProduct)
            {
                res.Request.Customer.InventoryManager.TryGiveItemstack(item, true);
                if (item.StackSize > 0)
                {
                    res.Request.Api.World.SpawnItemEntity(item, customer.Entity.Pos.XYZ.AddCopy(0.5, 0.5, 0.5), null);
                }
            }

            Block block = res.Request.ProductStackNeeded.Block;
            AssetLocation assetLocation = null;
            if (block != null)
            {
                BlockSounds sounds = block.Sounds;
                assetLocation = ((sounds != null) ? sounds.Place : null);
            }
            AssetLocation sound = assetLocation;
            res.Request.Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), customer.Entity, customer, true, 16f, 1f);

        }

        public static int TryConsumeCoupons(GenericTradeResult res)
        {
            if (res.Request.CouponSlots == null || res.Request.ConsumeCoupon == false) {
                return 0; 
            }

            // Take the product from the stall
            int couponsNeeded = res.Request.NumPurchases;
            foreach (ItemSlot itemSlot in res.Request.CouponSlots.Slots)
            {
                if (itemSlot.Itemstack == null) continue;
                ItemStack takenStack = itemSlot.TakeOut(couponsNeeded);
                couponsNeeded -= takenStack.StackSize;
                res.TransferedCoupons.Add(takenStack);
                res.TransferedCouponsTotal += takenStack.StackSize;
                itemSlot.MarkDirty();

                if (couponsNeeded <= 0) break;
            }


            if (couponsNeeded > 0)
            {
                AuditLogError(res, $"Error collecting coupons for trade. Needed {res.Request.NumPurchases} but is missing {couponsNeeded}");
            }

            return couponsNeeded;
        }

        public static void TryConsumeTools(GenericTradeResult res)
        {
            if (res.Request.SellingEntity.ConsumeTool(res))
            {
                // Not implemented yet
            } else
            {
                AuditLogError(res, "Error consuming tools for trade.");
            }
        }

        public static void TryAddPaymentToRegister(GenericTradeResult res)
        {
            if (res.Request.SellingEntity.IsAdminShop && res.Request.SellingEntity.DiscardProduct)
                return;

            // Add the payment to the register
            if (res.Request.ShopRegister != null)
            {
                foreach (ItemStack paymentStack in res.TransferedCurrency)
                {
                    if (!res.Request.ShopRegister.AddItem(paymentStack, paymentStack.StackSize))
                    {
                        AuditLogError(res, $"Failed to add payment of {paymentStack.StackSize} {paymentStack.GetName()} to Shop Register");
                    }
                }
                
            } else
            {
                AuditLogError(res, $"Failed to add payments to Register. ShopRegister was null");
            }
        }

        public static int TryExtractCurrencySlots(GenericTradeResult res)
        {
            int requestedCurrency = res.Request.NumPurchases * res.Request.GetFinalCurrencyNeededPerPurchase();

            // Take the product from the stall
            int currencyLeft = requestedCurrency;
           
            foreach (ItemSlot itemSlot in res.Request.CurrencySourceSlots.Slots)
            {
                if (itemSlot.Itemstack == null) continue;
                ItemStack takenStack = itemSlot.TakeOut(currencyLeft);
                currencyLeft -= takenStack.StackSize;
                res.TransferedCurrency.Add(takenStack);
                res.TransferedCurrencyTotal += takenStack.StackSize;
                itemSlot.MarkDirty();

                if (currencyLeft <= 0) break;
            }
            

            if (currencyLeft > 0)
            {
                AuditLogError(res, $"Error collecting payment for trade. Needed {requestedCurrency} but is missing {currencyLeft}");
            }

            return currencyLeft;
        }

        /// <summary>
        /// Removes the product from the Product Source Slots and adds any stacks meant to be given to the user
        /// to result.TransferedProduct. Update result.TransferedProductTotal accordingly
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static int TryExtractProductSlots(GenericTradeResult res)
        {
            int requestedProduct = res.Request.NumPurchases * res.Request.GetFinalProductNeededPerPurchase();
            // Take the product from the stall
            int productLeft = requestedProduct;
            if (res.Request.IsAdminShop)
            {
                int maxStackSize = res.Request.ProductStackNeeded.Collectible.MaxStackSize;
                while (productLeft > 0)
                {
                    ItemStack transferStack = res.Request.ProductStackNeeded.Clone();
                    int stackSize = Math.Min(productLeft, maxStackSize);
                    transferStack.StackSize = stackSize;
                    res.TransferedProduct.Add(transferStack);
                    res.TransferedProductTotal += stackSize;
                    productLeft -= stackSize;
                }
            }
            else
            {
                foreach (ItemSlot itemSlot in res.Request.ProductSourceSlots.Slots)
                {
                    if (itemSlot.Itemstack == null) continue;
                    ItemStack takenStack = itemSlot.TakeOut(productLeft);
                    productLeft -= takenStack.StackSize;
                    res.TransferedProduct.Add(takenStack);
                    res.TransferedProductTotal += takenStack.StackSize;
                    itemSlot.MarkDirty();

                    if (productLeft <= 0) break;
                }
            }

            if (productLeft > 0)
            {
                AuditLogError(res, $"Error collecting product for trade. Needed {requestedProduct} but is missing {productLeft}");
            }

            return productLeft;
        }



        public static void AuditLogError(GenericTradeResult res, string message)
        {
            res.Request.Api.ModLoader.GetModSystem<VinconomyCoreSystem>().Mod.Logger.Error(message);
        }

        public static bool CanFitPaymentInRegister(GenericTradeRequest request)
        {
            if (request.SellingEntity.IsAdminShop && request.SellingEntity.DiscardProduct)
                return true;

            int maxStackSize = request.CurrencyStackNeeded.Collectible.MaxStackSize;
            int qntyLeft =  request.NumPurchases * request.GetFinalCurrencyNeededPerPurchase();
            IInventory inv = request.ShopRegister.Inventory;
            foreach (ItemSlot itemSlot in inv)
            {
                if (itemSlot.Itemstack == null)
                {
                    qntyLeft -= maxStackSize;
                }
                else
                {
                    if (TradingUtil.isMatchingItem(itemSlot.Itemstack, request.CurrencyStackNeeded, request.Api.World))
                    {
                        qntyLeft -= maxStackSize - itemSlot.StackSize;
                    }
                }

                if (qntyLeft <= 0)
                {
                    return true;
                }
            }

            return qntyLeft <= 0;
        }

        public static GenericTradeResult SetErrorAndReturn(GenericTradeResult result, string error)
        {
            result.Error = error;
            result.Request.NumPurchases = 0;
            return result;
        }


    }
}
