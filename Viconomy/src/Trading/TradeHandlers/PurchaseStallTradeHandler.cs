using System;
using Vinconomy.BlockEntities;
using Vinconomy.Inventory.Impl;
using Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;

namespace Vinconomy.Trading.TradeHandlers
{

    /**
     * To avoid confusion later on when I re-read this, REMEMBER, that...
     * - GenericTradeRequest.*Currency is going to be the DesiredProduct that we want to SELL to the stall.
     * - GenericTradeReqeust.*Product is going to be the Currency that the player gets back from the stall.
     * 
     * The method naming conventions are flipped here because the Product in GenericTradeRequest is going to be more often than not *MONEY*
     */
    public static class PurchaseStallTradeHandler
    {

        public static bool StallHasRequiredCurrency(GenericTradeRequest req)
        {
            //Yes, this is the "DesiredProduct" in the sense of the GenericTradeRequest... dont get confused!
            int productLeft = req.GetFinalProductNeededPerPurchase() * req.RequestedPurchases;
            bool hasEnough = false;
            if (req.ProductSourceSlots != null)
                hasEnough = req.ProductSourceSlots.TotalCount / req.GetFinalProductNeededPerPurchase() > 0;

            //Dont bother checking the register if we dont need to
            //If we have RegisterFallback set, add in the ShopRegister's matching slots to the Agregate and check again
            if (!hasEnough && req.ShopRegister != null && ((BEVinconPurchaseContainer)req.SellingEntity).RegisterFallback) { 
                foreach (ItemSlot slot in req.ShopRegister.Inventory)
                {
                    if (TradingUtil.isMatchingItem(req.ProductStackNeeded, slot.Itemstack, req.Api.World))
                    {
                        req.ProductSourceSlots.Add(slot);
                    }
                }
                hasEnough = req.ProductSourceSlots.TotalCount / req.GetFinalProductNeededPerPurchase() > 0;

                //We modified the amount of currency slots available, so we need to rebuild the request to calculate max number of purchases
                req.Build();

            }

            return hasEnough;
        }

        public static bool PlayerHasRequiredDesiredProduct(GenericTradeRequest req)
        {
            //Yes, this is the "Currency" in the sense of the GenericTradeRequest... dont get confused!
            if (req.GetFinalCurrencyNeededPerPurchase() == 0)
                return true;

            if (req.CurrencySourceSlots != null)
                return req.CurrencySourceSlots.TotalCount / req.GetFinalCurrencyNeededPerPurchase() > 0;
            return false;
        }



        public static GenericTradeResult TryPurchaseItem(GenericTradeRequest request)
        {
            bool isAdminShop = request.SellingEntity.IsAdminShop;

            VinconomyCoreSystem core = request.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            GenericTradeResult res = new GenericTradeResult(request, core);

            BEVinconPurchaseContainer sellingEnt = (BEVinconPurchaseContainer)res.Request.SellingEntity;
            PurchaseStallSlot slot = (PurchaseStallSlot)((VinconItemPurchaseInventory)sellingEnt.Inventory).StallSlots[res.Request.StallSlot];

            // If we have a limited amount of purchases, grab whichever is fewer - the NumTradesLeft or the original amount.
            if (slot.LimitedPurchases)
            {
                res.Request.NumPurchases = Math.Min(res.Request.NumPurchases, slot.NumTradesLeft);
            }

            if (request.ShopRegister == null && !isAdminShop)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NOT_REGISTERED);

            if (request.CurrencyStackNeeded == null)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_PRICE);

            if (request.ProductStackNeeded == null)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_PRODUCT);

            if (!isAdminShop && !StallHasRequiredCurrency(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NOT_ENOUGH_STOCK);

            if (!GenericTradeHandler.PlayerHasRequiredCurrency(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NOT_ENOUGH_MONEY);

            if (request.NumPurchases <= 0)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.PURCHASED_ZERO);

            if (!GenericTradeHandler.HasRequiredTradePass(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_PASS);

            if (!isAdminShop && request.ShopRegister != null && !CanFitPaymentInStall(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_STALL_SPACE);

            // Extract all relevant items from their containers
            GenericTradeHandler.TryConsumeTools(res);
            GenericTradeHandler.TryConsumeCoupons(res);
            GenericTradeHandler.TryExtractProductSlots(res); // Currency to pay the player
            GenericTradeHandler.TryExtractCurrencySlots(res); // Desired Product sold to shop

            // Send TradeResult to mod system to take taxes, log sale in ledger, etc.
            core.PurchasedItem(res);

            TryAddDesiredProductToStall(res);
            if (res.Request.ShopRegister != null)
            {
                GenericTradeHandler.TryAddCouponsToRegister(res);
            }

            GenericTradeHandler.TryAddProductToPlayer(res);

            if (slot.LimitedPurchases)
            {
                slot.NumTradesLeft = Math.Max(0,slot.NumTradesLeft-1);
            }

            VinconomyCoreSystem.PrintClientMessage(res.Request.Customer, TradingConstants.PURCHASED_ITEMS, new object[] {
                res.TransferedProductTotal,
                res.Request.ProductStackNeeded.GetName(),
                res.TransferedCurrencyTotal,
                res.Request.CurrencyStackNeeded.GetName()
            });

            return res;
        }

        public static void TryAddDesiredProductToStall(GenericTradeResult res)
        {
            if (res.Request.SellingEntity.IsAdminShop && res.Request.SellingEntity.DiscardProduct)
                return;

                BEVinconPurchaseContainer stall = (BEVinconPurchaseContainer) res.Request.SellingEntity;
                int amountLeft = res.TransferedCurrencyTotal;
                foreach (ItemStack paymentStack in res.TransferedCurrency)
                {
                    if (amountLeft <= 0)
                    {
                        GenericTradeHandler.AuditLogError(res, $"Somehow still have {paymentStack.StackSize} {paymentStack.GetName()} in Transfered Currency, but already reached required required amount to add to stall");
                        break;
                    }

                    ItemSlot dslot = new ItemSlot(null);
                    dslot.Itemstack = paymentStack;
                    VinconItemPurchaseInventory inv = (VinconItemPurchaseInventory)stall.Inventory;
                    PurchaseStallSlot stallSlot = (PurchaseStallSlot)inv.StallSlots[res.Request.StallSlot];
                    foreach (ItemSlot slot in stallSlot.PurchasedProductSlots)
                    {
                        if (slot.CanHold(dslot))
                        {
                            amountLeft -= dslot.TryPutInto(res.Request.Api.World, slot, amountLeft);
                        }

                        if (amountLeft <= 0)
                        {
                            break;
                        }
                    }
                }

        }

        private static bool CanFitPaymentInStall(GenericTradeRequest request)
        {
            int maxStackSize = request.CurrencyStackNeeded.Collectible.MaxStackSize;
            int qntyLeft =  request.NumPurchases * request.GetFinalCurrencyNeededPerPurchase();
            VinconItemPurchaseInventory inv = ((VinconItemPurchaseInventory)request.SellingEntity.Inventory);
            PurchaseStallSlot slot = (PurchaseStallSlot) inv.StallSlots[request.StallSlot];
            foreach (ItemSlot itemSlot in slot.GetPurchasedProductSlots())
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
    }
}
