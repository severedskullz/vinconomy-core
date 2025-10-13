using System;
using System.Collections;
using System.Collections.Generic;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.StallSlots;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVinconLiquidContainer : BEVinconContainer
    {

        public override int ProductStacksPerSlot => 1;
        public override int BulkPurchaseAmount => 5;

        public override void ConfigureInventory()
        {
            inventory = new ViconLiquidInventory(this, null, Api, StallSlotCount, ProductStacksPerSlot);
        }

        public MeshData GenMesh(int stallSlot)
        {
            if (Block == null) return null;
            LiquidStallSlot stall = (LiquidStallSlot)inventory.StallSlots[stallSlot];

            ItemStack stacks = stall.GetSlot(0).Itemstack;

            return null;
        }

        protected override void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
            float y = inventory.ChiselDecoSlot.Itemstack != null ? 1 - (4.79f / 16f) : 0;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -.25f : .25f;
                float z = (i >= 2) ? .25f : -.25f;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f);

                MeshData data = GenMesh(i);
                if (data != null)
                {
                    data = data.Clone(); //TODO: Figure out why this affects pot meshes when I dont clone.
                    data.Rotate(origin, 0, GameMath.DEG2RAD * i * 90, 0);
                    mesher.AddMeshData(data, matrix.Values);
                }

            }
        }

        private bool CanMergeContents(ItemSlot handSlot, int stallSlot)
        {
            ItemStack stallStack = inventory.StallSlots[stallSlot].GetSlot(0).Itemstack;

            if (handSlot?.Itemstack == null) return false;
            if (stallStack == null) return true;

            if (handSlot.Itemstack.Block is ILiquidSource liquid)
            {
                return TradingUtil.isMatchingItem(liquid.GetContent(handSlot.Itemstack), stallStack, Api.World);
            }

            return false;
        }

        private int TransferContentsFromStall(ItemSlot source, int stallSlot, int amount)
        {
            Block block = source?.Itemstack?.Block;
            if (block == null) {
                return 0;
            }

            if (block is BlockLiquidContainerBase container)
            {
                float curLiters = (float)Math.Ceiling( container.GetCurrentLitres(source.Itemstack)); // Only transfer whole liters, so count 1.3 servings as 2, for sake of math
                float capacity = container.CapacityLitres;

                int toTransfer = Math.Min(amount, (int)(capacity - curLiters));
                ((LiquidStallSlot)inventory.StallSlots[stallSlot]).RemoveLiters(toTransfer);

                ItemStack item = source.Itemstack;

                container.SetCurrentLitres(source.Itemstack, curLiters + toTransfer);

                return toTransfer;
            }
            return 0;
        }

        
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            int slotIndex = GetStallSlotForSelectionIndex(blockSel.SelectionBoxIndex);

            bool sneakMod = byPlayer.Entity.Controls.Sneak;
            bool sprintMod = byPlayer.Entity.Controls.Sprint;


            ItemSlot hotbarslot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (byPlayer.PlayerUID == Owner && !VinconomyCoreSystem.ShouldForceCustomerScreen)
            {
                // If we are holding an empty food container, and sneaking => take from stall
                if (VinUtils.IsEmptyLiquidContainer(hotbarslot.Itemstack) && sneakMod)
                {
                    TransferContentsFromStall(hotbarslot, slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                }

                // If we are holding a filled container, and it matches
                else if (CanMergeContents(hotbarslot, slotIndex)) {
                    // if we are holding Sneak, take from stall
                    if (sneakMod)
                    {
                        TransferContentsFromStall(hotbarslot, slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                    } else
                    {
                        TryPut(hotbarslot, slotIndex, sprintMod);
                    }
                } else if (sneakMod && TradingUtil.isMatchingItem(inventory.GetCurrencyForStallSlot(slotIndex).Itemstack, hotbarslot.Itemstack, Api.World)) {
                    RequestPurchaseItem(slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                } else {
                    OpenShopForPlayer(byPlayer, slotIndex);
                }
            }
            else
            {
                if (sneakMod)
                {
                    //Purchase the items.
                    RequestPurchaseItem(slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                }
                else
                {
                    // Open the shop inventory for that block selection
                    OpenShopForPlayer(byPlayer, slotIndex);
                }
            }
            return true;
        }

        protected override bool TryAddItemToStall(ItemSlot activeSlot, int stallSlot, bool bulk)
        {

            if (activeSlot.Itemstack?.Block is BlockLiquidContainerBase)
            {
                return ((ViconLiquidInventory)inventory).AddLiquidToStall(stallSlot, activeSlot, bulk ? BulkPurchaseAmount : 1);
            }
            return false;
        }

        public override void PurchaseItem(IPlayer player, int stallSlot, int numPurchases, BEVinconRegister shopRegister)
        {
            LiquidStallSlot stall = inventory.GetStall<LiquidStallSlot>(stallSlot);
            if (stall.Currency.Itemstack == null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NO_PRICE);
                return;
            }

            int numActualTrades = Math.Min(numPurchases, stall.GetNumPurchasesRemaining());

            // if NumPurchasesRemaining is < 1, return error
            if (numActualTrades < 1)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NOT_ENOUGH_STOCK);
                return;
            }

            // Get all empty bowls and cooking pots in inventory
            ItemSlot[] containerSlots = GetContainerSlots(player);
            int carryCapacity = 0;
            foreach (ItemSlot containerSlot in containerSlots) {
                // add up max servings
                carryCapacity += containerSlot.StackSize * containerSlot.Itemstack.Block.Attributes["servingCapacity"].AsInt();
            }
            // if max servings is less than desired amount, reduce desired amount
            numActualTrades = Math.Min(numActualTrades, carryCapacity / stall.ItemsPerPurchase);

            // if max servings is < 1, return error
            if (numActualTrades < 1)
            {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NOT_ENOUGH_CAPACITY);
                return;
            }

            // Check if player has enough currency
            AggregatedSlots currencySlots = TradingUtil.GetAllValidSlotsFor(player, stall.Currency);
            // if not enough, reduce desired amount to what they can afford
            numActualTrades = Math.Min(numActualTrades, currencySlots.TotalCount / stall.ItemsPerPurchase);

            // If purchaseAmount > 0, continue
            if (numActualTrades < 1) {
                VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.NOT_ENOUGH_MONEY);
                return;
            }

            // At this point we know can "afford" something

            // How many servings did we purchase from the stall
            int totalServingsPurchased = numActualTrades * stall.ItemsPerPurchase;
            //string recipeCode = stall.GetRecipeCode(Api);

            int totalServingsLeftToTransfer = totalServingsPurchased;
            // loop through player's containers and convert to meal blocks
            foreach (ItemSlot containerSlot in containerSlots)
            {
                // Save stacksize as variable. We will be taking items OUT of this stack, so it would exit the loop early.
                // Eg. Had 2 bowls, loop ran, took one out, 'i' is now 1, and stack size is 1, so loop terminates and doesnt run on second bowl.
                int numAttempts = containerSlot.StackSize; 
                for (int i = 0; i < numAttempts; i++)
                {
                    int capacity = containerSlot.Itemstack.Block.Attributes["servingCapacity"].AsInt();
                    int servingsToTransfer = Math.Min(totalServingsLeftToTransfer, capacity);
                    //int left = TransferToMealBlock(player, containerSlot, recipeCode, stall.GetMealStacks(), totalServingsLeftToTransfer);
                    //totalServingsLeftToTransfer -= left;
                    if (totalServingsLeftToTransfer <= 0)
                        break;
                }
                if (totalServingsLeftToTransfer <= 0)
                    break;

            }

            if (totalServingsLeftToTransfer > 0)
            {
                modSystem.Mod.Logger.Error("Somehow allowed purchase of " + totalServingsLeftToTransfer + " extra servings even though we didnt have enough containers");
            }

            //Take food out of the stall
            //stall.RemoveServings(totalServingsLeftToTransfer);


            //Take the money from the player.
            ItemStack paymentStack = null;
            int currencyLeft = numActualTrades * stall.Currency.StackSize;
            foreach (ItemSlot itemSlot in currencySlots.Slots)
            {
                if (paymentStack == null)
                {
                    paymentStack = itemSlot.TakeOut(currencyLeft);
                    currencyLeft -= paymentStack.StackSize;
                }
                else
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
            if (shopRegister != null)
            {
                shopRegister.AddItem(paymentStack, paymentStack.StackSize);
            }

            //purchaseResult.purchasedItems = productStackClone;
            //purchaseResult.purchasedCurrencyUsed = paymentStackClone;
            TradeResult res = new TradeResult();
            res.sellingEntity = this;
            res.customer = player;
            res.coreApi = Api;


            ItemStack fakeStack = VinUtils.ResolveBlockOrItem(Api, "game:bowl-meal", numActualTrades);
            //(fakeStack.Block as BlockMeal).SetContents(recipeCode, fakeStack, stall.GetMealStacks(),1);

            res.purchasedItems = fakeStack;
            res.purchasedCurrencyUsed = paymentStackClone;

            modSystem.PurchasedItem(res, fakeStack, paymentStackClone);

            //Tell the player they purchased items
            VinconomyCoreSystem.PrintClientMessage(player, TradingConstants.PURCHASED_ITEMS, new object[] {
                fakeStack.StackSize,
                fakeStack.GetName(),
                paymentStackClone.StackSize,
                paymentStackClone.GetName()
            });

            this.MarkDirty(true, null);
            this.UpdateMeshes();
        }

        private ItemSlot[] GetContainerSlots(IPlayer player)
        {
            List<ItemSlot> aggregatedSlots = new List<ItemSlot>();


            ItemSlot handItem = player.InventoryManager.ActiveHotbarSlot;
            if (VinUtils.IsEmptyLiquidContainer(handItem.Itemstack))
            {
                aggregatedSlots.Add(handItem);
            }

            IInventory hotbarInv = player.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (VinUtils.IsEmptyLiquidContainer(itemSlot.Itemstack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (VinUtils.IsEmptyLiquidContainer(itemSlot.Itemstack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }
            return aggregatedSlots.ToArray();
        }

        public override int GetStallSlotForSelectionIndex(int index)
        {
            if (index > 0 && index < 5) return index-1;
            return 0;
        }

        private int TransferToMealBlock(IPlayer player, ItemSlot containerSlot, string recipe, ItemStack[] mealStacks, int servings)
        {

            JsonObject attr = containerSlot.Itemstack.Block.Attributes;
            string code = attr["mealBlockCode"]?.AsString();
            if (code == null) return 0;

            int capacity = 0;
            if (attr.KeyExists("servingCapacity"))
            {
                capacity = attr["servingCapacity"].AsInt(); ;
            }
            if (capacity <= 0)
            {
                return 0;
            }
           

            Block mealblock = Api.World.GetBlock(new AssetLocation(code));
            IBlockMealContainer meal = mealblock as IBlockMealContainer;
            int servingsToTransfer = Math.Min(servings, capacity);
            ItemStack stack = new ItemStack(mealblock);
            meal.SetContents(recipe, stack, mealStacks, servingsToTransfer);

            containerSlot.TakeOut(1);
            containerSlot.MarkDirty();

            player.InventoryManager.TryGiveItemstack(stack, true);
            if (!player.InventoryManager.TryGiveItemstack(stack, true))
            {
                Api.World.SpawnItemEntity(stack, player.Entity.ServerPos.XYZ.AddCopy(0.5, 0.5, 0.5), null);
            }
            return servingsToTransfer;
        }





    }
}
