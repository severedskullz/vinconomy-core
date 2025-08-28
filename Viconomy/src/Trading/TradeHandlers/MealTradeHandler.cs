using System;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Common;

namespace Viconomy.Trading.TradeHandlers
{
    public static class MealTradeHandler
    {
        public static bool StallHasRequiredProduct(GenericTradeRequest req)
        {
            ViconMealInventory inv = (ViconMealInventory)req.SellingEntity.Inventory;
            return inv.GetStall<MealStallSlot>(req.StallSlot).GetProductQuantity() >= req.GetFinalProductNeededPerPurchase();

        }

        public static GenericTradeResult TryPurchaseItem(GenericTradeRequest request)
        {
            bool isAdminShop = request.SellingEntity.IsAdminShop;

            VinconomyCoreSystem core = request.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            GenericTradeResult res = new GenericTradeResult(request, core);
            if (request.ShopRegister == null && !isAdminShop)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NOT_REGISTERED);

            if (request.CurrencyStackNeeded == null)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_PRICE);

            if (request.ProductStackNeeded == null)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_PRODUCT);

            if (!isAdminShop && !StallHasRequiredProduct(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NOT_ENOUGH_STOCK);

            if (!GenericTradeHandler.PlayerHasRequiredCurrency(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NOT_ENOUGH_MONEY);

            if (request.NumPurchases <= 0)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.PURCHASED_ZERO);

            if (!GenericTradeHandler.HasRequiredTools(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_TOOL);

            if (!GenericTradeHandler.HasRequiredTradePass(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_PASS);

            if (request.ShopRegister != null && !GenericTradeHandler.CanFitPaymentInRegister(request))
                return GenericTradeHandler. SetErrorAndReturn(res, TradingConstants.NO_REGISTER_SPACE);

            // Extract all relevant items from their containers
            GenericTradeHandler.TryConsumeCoupons(res);
            TryExtractProductSlots(res);
            GenericTradeHandler.TryExtractCurrencySlots(res);


            // Send TradeResult to mod system to take taxes, log sale in ledger, etc.
            core.PurchasedItem(res);

            // Add payment to stall and give player product.
            if (res.Request.ShopRegister != null)
            {
                GenericTradeHandler.TryAddPaymentToRegister(res);
                GenericTradeHandler.TryAddCouponsToRegister(res);
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

        public static void TryGiveMeals(GenericTradeResult res)
        {

            // At this point we know can "afford" something

            // How many servings did we purchase from the stall
            int totalServingsPurchased = res.Request.NumPurchases * res.Request.GetFinalProductNeededPerPurchase();
            MealStallSlot mealStallSlot = ((ViconMealInventory) res.Request.SellingEntity.Inventory).GetStall<MealStallSlot>(res.Request.StallSlot);
            string recipeCode = mealStallSlot.GetRecipeCode(res.Request.Api);


            int totalServingsLeftToTransfer = totalServingsPurchased;
            // loop through player's containers and convert to meal blocks
            foreach (ItemSlot containerSlot in res.Request.ToolSourceSlots.Slots)
            {
                // Save stacksize as variable. We will be taking items OUT of this stack, so it would exit the loop early.
                // Eg. Had 2 bowls, loop ran, took one out, 'i' is now 1, and stack size is 1, so loop terminates and doesnt run on second bowl.
                int numAttempts = containerSlot.StackSize;
                for (int i = 0; i < numAttempts; i++)
                {
                    int capacity = containerSlot.Itemstack.Block.Attributes["servingCapacity"].AsInt();
                    int servingsToTransfer = Math.Min(totalServingsLeftToTransfer, capacity);
                    int left = ((BEVinconFoodContainer)res.Request.SellingEntity).TransferToMealBlock(res.Request.Customer, containerSlot, recipeCode, mealStallSlot.GetMealStacks(), totalServingsLeftToTransfer);
                    totalServingsLeftToTransfer -= left;
                    if (totalServingsLeftToTransfer <= 0)
                        break;
                }
                if (totalServingsLeftToTransfer <= 0)
                    break;

            }

            if (totalServingsLeftToTransfer > 0)
            {
                GenericTradeHandler.AuditLogError(res, "Somehow allowed purchase of " + totalServingsLeftToTransfer + " extra servings even though we didnt have enough containers");
            }
        }


        public static void TryAddProductToPlayer(GenericTradeResult res)
        {
            if (res.TransferedProduct?.Count == 0)
            {
                GenericTradeHandler.AuditLogError(res, "Tried to give player products, but has nothing to give");
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
                GenericTradeHandler.AuditLogError(res, $"Error collecting payment for trade. Needed {requestedProduct} but is missing {productLeft}");
            }

            return productLeft;
        }

    }
}
