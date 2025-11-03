using System;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Viconomy.Trading.TradeHandlers
{
    public class LiquidTradeHandler
    {
        public static bool StallHasRequiredProduct(GenericTradeRequest req)
        {
            ViconLiquidInventory inv = (ViconLiquidInventory)req.SellingEntity.Inventory;
            return inv.GetStall<LiquidStallSlot>(req.StallSlot).GetProductQuantity() >= req.GetFinalProductNeededPerPurchase();

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
            GenericTradeHandler.TryExtractProductSlots(res);


            LiquidStallSlot stall = ((ViconBaseInventory)res.Request.SellingEntity.Inventory).GetStall<LiquidStallSlot>(res.Request.StallSlot);
            ItemStack ingredientStacks = stall.FindFirstNonEmptyStockSlot()?.Itemstack;

            // Send TradeResult to mod system to take taxes, log sale in ledger, etc.
            core.PurchasedItem(res);

            // Add payment to stall and give player product.
            if (res.Request.ShopRegister != null)
            {
                GenericTradeHandler.TryAddPaymentToRegister(res);
                GenericTradeHandler.TryAddCouponsToRegister(res);
            }

            TryGiveLiquids(res);

            VinconomyCoreSystem.PrintClientMessage(res.Request.Customer, TradingConstants.PURCHASED_ITEMS, [
                res.TransferedProductTotal,
                res.Request.ProductStackNeeded.GetName(),
                res.TransferedCurrencyTotal,
                res.Request.CurrencyStackNeeded.GetName()
            ]);

            return res;
        }

        public static void TryGiveLiquids(GenericTradeResult res)
        {
            IPlayer player = res.Request.Customer;
            foreach (ItemStack item in res.TransferedProduct)
            {
                foreach (ItemSlot toolSlot in res.Request.ToolSourceSlots.Slots)
                {
                    //We need to loop again for stacks of buckets!
                    int stackCount = toolSlot.StackSize;
                    for (int i = 0; i < stackCount; i++)
                    {
                        ItemStack tool = toolSlot.Itemstack;
                        int moved = TransferToLiquidContainer(player, toolSlot, item);
                        if (moved > 0)
                            toolSlot.MarkDirty();

                        item.StackSize -= moved;

                        // Break the StackSize loop
                        if (item.StackSize <= 0)
                            break;
                    }

                    //Break the Tool Slot loop
                    if (item.StackSize <= 0)
                        break;

                }
            }
        }

        public static int TransferLiquidToItemStack(ItemStack containerStack, ItemStack liquidStacks)
        {
            Block block = containerStack?.Block;
            if (block == null)
            {
                return 0;
            }

            if (block is BlockLiquidContainerBase container)
            {

                if (container.GetCurrentLitres(containerStack) >= container.CapacityLitres)
                    return 0;

                int moved = container.TryPutLiquid(containerStack, liquidStacks, ConvertStackToLiters(liquidStacks));
                return moved;

            }
            return 0;
        }

        public static int TransferToLiquidContainer(IPlayer player, ItemSlot containerSlot, ItemStack liquidStacks)
        {
            ICoreAPI api = player.Entity.Api;
            if (containerSlot == null)
                return 0;

            if (liquidStacks == null)
                return 0;

            if (containerSlot.StackSize == 1)
            {
                return TransferLiquidToItemStack(containerSlot.Itemstack, liquidStacks);
            } else
            {
                ItemStack containerStack = containerSlot.TakeOut(1);
                int moved = TransferLiquidToItemStack(containerStack, liquidStacks);
                if (!player.InventoryManager.TryGiveItemstack(containerStack))
                {
                    api.World.SpawnItemEntity(containerStack, player.Entity.ServerPos.XYZ.AddCopy(0.5, 0.5, 0.5), null);
                }
                return moved;

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

        public static float ConvertStackToLiters(ItemStack stack)
        {
            WaterTightContainableProps contentProps = BlockLiquidContainerBase.GetContainableProps(stack);
            if (contentProps == null)
            {
                return 0;
            }
            return stack.StackSize / contentProps.ItemsPerLitre;
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

        public static int ConvertLitersToStack(ItemStack stack, float liters)
        {
            WaterTightContainableProps contentProps = BlockLiquidContainerBase.GetContainableProps(stack);
            if (contentProps == null)
            {
                return 0;
            }
            return (int)(liters * contentProps.ItemsPerLitre);
        }

        public static bool IsLiquidContainer(ItemStack stack)
        {
            return stack?.Block is BlockLiquidContainerBase container;
        }

        public static bool IsEmptyLiquidContainer(ItemStack stack)
        {
            return IsLiquidContainer(stack) && ((BlockLiquidContainerBase)stack.Block).GetCurrentLitres(stack) == 0;
        }

        public static bool CanHoldLiquid(ItemStack sourceStack, ItemStack targetStack)
        {
            if (sourceStack == null || targetStack == null)
                return false;

            BlockLiquidContainerBase container = sourceStack.Block as BlockLiquidContainerBase;
            if (container == null)
                return false;

            if (container.GetContent(sourceStack) == null)
                return true;

            if (container.GetCurrentLitres(sourceStack) >= container.CapacityLitres)
                return false;

            return targetStack.Satisfies(container.GetContent(sourceStack));
        }
    }
}
