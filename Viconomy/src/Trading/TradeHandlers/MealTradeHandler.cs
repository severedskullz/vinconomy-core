using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.StallSlots;
using Viconomy.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

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

            if (!GenericTradeHandler.HasRequiredTools(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NOT_ENOUGH_CAPACITY);

            if (request.NumPurchases <= 0)
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.PURCHASED_ZERO);

            if (!GenericTradeHandler.HasRequiredTradePass(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_PASS);

            if (request.ShopRegister != null && !GenericTradeHandler.CanFitPaymentInRegister(request))
                return GenericTradeHandler.SetErrorAndReturn(res, TradingConstants.NO_REGISTER_SPACE);

            // Extract all relevant items from their containers
            GenericTradeHandler.TryConsumeCoupons(res);
            GenericTradeHandler.TryExtractCurrencySlots(res);


            MealStallSlot stall = ((ViconBaseInventory)res.Request.SellingEntity.Inventory).GetStall<MealStallSlot>(res.Request.StallSlot);
            ItemStack[] ingredientStacks = stall.GetMealStacks();
            string recipeCode = stall.GetRecipeCode(res.Request.Api);
            ItemStack outputStack = stall.GenerateMealStack(res.Request.Api);
            int requestedServings = res.Request.NumPurchases * res.Request.GetFinalProductNeededPerPurchase();
            if (outputStack.Block == null)
            {
                GenericTradeHandler.AuditLogError(res, "Could not resolve meal game code for sale for some reason...");
            }
            else
            {
                res.TransferedProduct.Add(outputStack);
                res.TransferedProductTotal = requestedServings;
            }

            TryExtractIngredientSlots(res);

            // Send TradeResult to mod system to take taxes, log sale in ledger, etc.
            core.PurchasedItem(res);

            // Add payment to stall and give player product.
            if (res.Request.ShopRegister != null)
            {
                GenericTradeHandler.TryAddPaymentToRegister(res);
                GenericTradeHandler.TryAddCouponsToRegister(res);
            }

            TryGiveMeals(res, recipeCode, ingredientStacks, requestedServings);

            VinconomyCoreSystem.PrintClientMessage(res.Request.Customer, TradingConstants.PURCHASED_ITEMS, [
                res.TransferedProductTotal,
                res.Request.ProductStackNeeded.GetName(),
                res.TransferedCurrencyTotal,
                res.Request.CurrencyStackNeeded.GetName()
            ]);

            return res;
        }

        public static void TryGiveMeals(GenericTradeResult res, string recipeCode, ItemStack[] mealStacks, int requestedServings)
        {

            int totalServingsLeftToTransfer = requestedServings;
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
                    int left = TransferToMealBlock(res.Request.Customer, containerSlot, recipeCode, mealStacks, totalServingsLeftToTransfer);
                    totalServingsLeftToTransfer -= left;
                    if (totalServingsLeftToTransfer <= 0)
                        break;
                }

                if (totalServingsLeftToTransfer <= 0)
                    return;

            }

            if (totalServingsLeftToTransfer > 0)
            {
                GenericTradeHandler.AuditLogError(res, "Somehow allowed purchase of " + totalServingsLeftToTransfer + " extra servings even though we didnt have enough containers");
            }
        }

        public static int TransferToMealBlock(IPlayer player, ItemSlot containerSlot, string recipe, ItemStack[] mealStacks, int servings)
        {
            int servingsToTransfer = 0;
            int capacity = 0;
            
            ICoreAPI api = player.Entity.Api;

            // Why the fuck isnt the servingCapacity also on the meal block code?
            // I have to be missing something here.
            JsonObject attr = containerSlot.Itemstack.Block.Attributes;
            if (attr.KeyExists("servingCapacity"))
            {
                capacity = attr["servingCapacity"].AsInt();
            }
            if (capacity <= 0)
            {
                return 0;
            }

            if (containerSlot.Itemstack.Block is IBlockMealContainer meal)
            {              
                int currentServings = (int)Math.Ceiling(meal.GetQuantityServings(api.World, containerSlot.Itemstack));
                if (currentServings >= capacity)
                    return 0;

                servingsToTransfer = Math.Min(servings, capacity - currentServings);
                meal.SetContents(recipe, containerSlot.Itemstack, mealStacks, currentServings + servingsToTransfer);
                containerSlot.Itemstack.Attributes.RemoveAttribute("sealed");

                player.InventoryManager.NotifySlot(player, containerSlot);
                containerSlot.MarkDirty();
            }
            else
            {
                ItemStack mealStack = ConvertToMealContainer(api, containerSlot.Itemstack);
                if (mealStack != null)
                {
                    if (mealStack.Block is not IBlockMealContainer mealBlock)
                    {
                        throw new Exception("Somehow got a meal stack that wasn't a meal container");
                    }

                    servingsToTransfer = Math.Min(servings, capacity);
                    mealBlock.SetContents(recipe, mealStack, mealStacks, servingsToTransfer);
                    containerSlot.TakeOut(1);
                    containerSlot.MarkDirty();

                    if (!player.InventoryManager.TryGiveItemstack(mealStack, true))
                    {
                        api.World.SpawnItemEntity(mealStack, player.Entity.ServerPos.XYZ.AddCopy(0.5, 0.5, 0.5), null);
                    }
                }
            }

            return servingsToTransfer;
        }

        public static ItemStack ConvertToMealContainer(ICoreAPI api, ItemStack stack)
        {
            if (stack.Block is IBlockMealContainer)
                return stack;

            // Cooking Pot - always empty, block type changes when it is turned into claypot-cooked
            if (!(stack.Block is BlockCookingContainer || stack.Block is BlockContainer))
                return null;

            JsonObject attr = stack.Block.Attributes;
            if (attr == null)
                return null;

            string code = attr["mealBlockCode"]?.AsString();
            if (code == null)
                return null;

            int capacity = 0;
            if (attr.KeyExists("servingCapacity"))
            {
                capacity = attr["servingCapacity"].AsInt(); ;
            }
            if (capacity <= 0)
            {
                return null;
            }

            Block mealblock = api.World.GetBlock(code);
            if (mealblock == null)
                return null;
            
            return new ItemStack(mealblock);
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
        public static void TryExtractIngredientSlots(GenericTradeResult res)
        {
            if (!res.Request.IsAdminShop)
            {
                BEVinconFoodContainer container = res.Request.SellingEntity as BEVinconFoodContainer;
                ViconBaseInventory inv = (ViconBaseInventory)container.Inventory;
                MealStallSlot stall = inv.GetStall<MealStallSlot>(res.Request.StallSlot);
                int requestedServings = res.Request.NumPurchases * res.Request.GetFinalProductNeededPerPurchase();
                stall.RemoveServings(requestedServings);
            }
        }

    }
}
