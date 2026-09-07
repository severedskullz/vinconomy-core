using Commercially.Common.Inventory.Slots;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.ModSystems;
using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.Trading.Processor
{
    public class GenericPurchaseProcessor
    {

        public static bool CanFitPaymentIntoParent(PurchaseRequest request)
        {
            ICurrencySinkProvider currencySinkProvider = request.StallSlot.GetCurrencySink(request);
            if (currencySinkProvider == null)
            {
                return false;
            }

            ItemSlot[] currencySlots = currencySinkProvider.CurrencySlots;
            int maxStackSize = request.CurrencyNeeded.Collectible.MaxStackSize;
            int qntyLeft = request.CurrencyNeeded.StackSize * request.NumPurchases;
            foreach (ItemSlot itemSlot in currencySlots)
            {
                if (itemSlot.Itemstack == null)
                {
                    qntyLeft -= maxStackSize;
                }
                else
                {
                    if (TradingUtil.IsMatchingItem(itemSlot.Itemstack, request.CurrencyNeeded, request.Api.World, false))
                    {
                        qntyLeft -= maxStackSize - itemSlot.StackSize;
                    }
                }

                if (qntyLeft <= 0)
                {
                    break;
                }
            }

            return (qntyLeft <= 0);
        }

        public static bool HasEnoughStock(PurchaseRequest request)
        {
            if (request.IsAdminShop) return true;
            int productQuantity = request.StallSlot.GetTotalProductAvailable();
            return (productQuantity / request.GetFinalProductNeededPerPurchase()) > 0;
        }

        public static bool HasEnoughContainerCapacity(PurchaseRequest req)
        {
            if (req.ContainerSourceSlots == null)
            {
                return true;
            }
            else if (req.ContainerSourceSlots is ServingCapacityAggregatedSlots servings)
            {
                return servings.TotalCapacity / req.GetFinalProductNeededPerPurchase() > 0;
            }

            else if (req.ContainerSourceSlots is LiquidCapacityAggregatedSlots capacity)
            {
                float litersNeeded = ConvertStackToLiters(req.ProductNeeded, req.GetFinalProductNeededPerPurchase());
                return (int)(capacity.TotalCapacity / litersNeeded) > 0;
            }
            
            return true;
        }

        public static bool CanPlayerAfford(PurchaseRequest request)
        {
            int currencyRequired = request.GetFinalCurrencyNeededPerPurchase();
            int totalCurrecny = request.CurrencySourceSlots.TotalCount;

            return totalCurrecny >= currencyRequired;

        }



        public static bool HasEnoughDurability(PurchaseRequest request)
        {
            if (request.ToolSourceSlots == null)
            {
                return true;
            }
            else if (request.ToolSourceSlots is DurabilityAggregatedSlots durability)
            {
                return durability.TotalDurability / request.GetFinalProductNeededPerPurchase() > 0;
            }
            /*
            else
            {
                return request.ToolSourceSlots.TotalCount / request.GetFinalProductNeededPerPurchase() > 0;
            }*/

            return true;
        }

        public static bool HasRequiredTradePass(PurchaseRequest req)
        {
            return !(req.TradePassNeeded != null && req.TradePassSourceSlots.TotalCount <= 0);
        }

        public static float ConvertStackToLiters(ItemStack stack, int amount)
        {
            WaterTightContainableProps contentProps = BlockLiquidContainerBase.GetContainableProps(stack);
            if (contentProps == null)
            {
                return 0;
            }
            return amount / contentProps.ItemsPerLitre;
        }

        public static void AuditLogError(PurchaseResult res, string message)
        {
            res.Request.Api.ModLoader.GetModSystem<VinconomyCoreSystem>().Mod.Logger.Error(message);
        }
        public static void AuditLogDebug(PurchaseResult res, string message)
        {
            res.Request.Api.ModLoader.GetModSystem<VinconomyCoreSystem>().Mod.Logger.Debug(message);
        }
    }
}
