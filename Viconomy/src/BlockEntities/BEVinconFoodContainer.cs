using System;
using System.Collections.Generic;
using System.Numerics;
using Viconomy.GUI;
using Viconomy.Inventory.Impl;
using Viconomy.Inventory.StallSlots;
using Viconomy.Trading;
using Viconomy.Trading.TradeHandlers;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVinconFoodContainer : BEVinconContainer
    {

        public override int ProductStacksPerSlot => 9;
        public override int BulkPurchaseAmount => 6;

        public override void ConfigureInventory()
        {
            inventory = new ViconMealInventory(this, null, Api, StallSlotCount, ProductStacksPerSlot);
        }

        public MeshData GenMesh(int stallSlot)
        {
            if (Block == null) return null;
            MealStallSlot stall = inventory.GetStall<MealStallSlot>(stallSlot);

            ItemStack[] stacks = stall.GetMealStacks();
            
            CookingRecipe recipe = stall.GetMatchingCookingRecipe(Api);
            Block potBlock = Api.World.GetBlock(new AssetLocation("game:claypot-cooked"));
            if (inventory.ChiselDecoSlot.Itemstack != null)
            {
                return Api.ModLoader.GetModSystem<MealMeshCache>().GenMealInContainerMesh(potBlock, recipe, stacks, new Vec3f(0, 2.5f / 16f, 0));
            } else
            {
                return Api.ModLoader.GetModSystem<MealMeshCache>().GenMealMesh(recipe, stacks, new Vec3f(0, 1 - (2.5f / 16f), 0));
            }
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
            if (handSlot?.Itemstack == null) return false;

            if (handSlot.Itemstack.Block is IBlockMealContainer meal)
            {
                return VinUtils.IsMergableContents(meal.GetNonEmptyContents(Api.World,handSlot.Itemstack), inventory.GetStall<MealStallSlot>(stallSlot).GetMealStacks());
            }

            return false;
        }

        private int TransferContentsFromStall(ItemSlot source, int stallSlot, int amount)
        {
            if (source?.Itemstack?.Block is IBlockMealContainer meal)
            {
                int curServings = (int)Math.Ceiling(meal.GetQuantityServings(Api.World,source.Itemstack)); // Only transfer whole servings, so count 1.3 servings as 2, for sake of math
                int capacity = source.Itemstack.Block.Attributes["servingCapacity"].AsInt();

                int toTransfer = Math.Min(amount, capacity - curServings);
                inventory.GetStall<MealStallSlot>(stallSlot).RemoveServings(toTransfer);
                ItemStack item = source.Itemstack;

                meal.SetContents(meal.GetRecipeCode(Api.World, item), item, meal.GetNonEmptyContents(Api.World, item), curServings + toTransfer);
                source.Itemstack.Attributes.RemoveAttribute("sealed");
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
                // Empty pots and crocks dont have the recipe code or contents set, so not the same as TransferContentsToStall()
                if (VinUtils.IsEmptyContainer(hotbarslot.Itemstack,Api) && sneakMod)
                {
                    TakeFromStallSlot(byPlayer, slotIndex, sprintMod, hotbarslot);
                }

                // If we are holding a filled container, and it matches
                // Filled containers already have the contents and recipe code set, so we just update the servings
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

        private void TakeFromStallSlot(IPlayer byPlayer, int slotIndex, bool ctrlMod, ItemSlot hotbarslot)
        {
            MealStallSlot stallSlot = inventory.GetStall<MealStallSlot>(slotIndex);
            ItemSlot firstStack = stallSlot.FindFirstNonEmptyStockSlot();
            if (firstStack == null) return;

            int capacity = hotbarslot.Itemstack.Block.Attributes["servingCapacity"].AsInt();
            int toTake = Math.Min(firstStack.StackSize, ctrlMod ? BulkPurchaseAmount : 1);
            toTake = Math.Min(capacity, toTake);

            int transfered = MealTradeHandler.TransferToMealBlock(byPlayer, hotbarslot, stallSlot.GetRecipeCode(Api), stallSlot.GetMealStacks(), toTake);
            if (transfered != toTake)
            {
                modSystem.Mod.Logger.Error("Somehow generated differing quantity of food. Wanted " + toTake + " but took " + transfered);
            }
            foreach (ItemSlot foodSlot in stallSlot.GetSlots())
            {
                if (foodSlot.Itemstack == null)
                    continue;

                ItemStack stack = foodSlot.TakeOut(toTake);
                if (stack.StackSize != toTake)
                {
                    modSystem.Mod.Logger.Error("Somehow generated differing quantity of food. Wanted " + toTake + " but took " + transfered);
                }
            }
        }

        protected override bool TryAddItemToStall(ItemSlot activeSlot, int stallSlot, bool bulk)
        {

            if (activeSlot.Itemstack?.Block is IBlockMealContainer)
            {
                return ((ViconMealInventory)inventory).AddMealToStall(stallSlot, activeSlot, bulk ? BulkPurchaseAmount : 1);
            }
            return false;
        }

        public override AggregatedSlots GetRequiredTools(IPlayer player, int stallSlot)
        {
            ServingCapacityAggregatedSlots aggregatedSlots = new ServingCapacityAggregatedSlots(Api);
            ItemStack[] mealStacks = inventory.GetStall<MealStallSlot>(stallSlot).GetMealStacks();


            ItemSlot handItem = player.InventoryManager.ActiveHotbarSlot;
            if (CanHoldMeal(mealStacks, handItem.Itemstack))
            {
                aggregatedSlots.Add(handItem);
            }

            IInventory hotbarInv = player.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (CanHoldMeal(mealStacks, itemSlot.Itemstack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (CanHoldMeal(mealStacks, itemSlot.Itemstack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }
            return aggregatedSlots;
        }

        public override GenericTradeResult PurchaseItem(GenericTradeRequest request)
        {
            return MealTradeHandler.TryPurchaseItem(request);
        }

        private bool CanHoldMeal(ItemStack[] mealStacks, ItemStack dest)
        {
            return VinUtils.IsMealContainer(dest, Api) 
                && VinUtils.IsMergableContents(mealStacks, VinUtils.GetContainerContents(dest, Api));
        }

        private ItemSlot[] GetContainerSlots(IPlayer player, int stallSlot)
        {
            List<ItemSlot> aggregatedSlots = new List<ItemSlot>();

            ItemStack[] mealStacks = inventory.GetStall<MealStallSlot>(stallSlot).GetMealStacks();


            ItemSlot handItem = player.InventoryManager.ActiveHotbarSlot;
            if (CanHoldMeal(mealStacks,handItem.Itemstack))
            {
                aggregatedSlots.Add(handItem);
            }

            IInventory hotbarInv = player.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (CanHoldMeal(mealStacks, itemSlot.Itemstack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (CanHoldMeal(mealStacks, itemSlot.Itemstack))
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

        protected override GuiDialogBlockEntity GetCustomerGui(string dialogTitle, int stallSelection)
        {
            return new GuiVinconMealStallCustomer(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
        }

        
        protected override GuiDialogBlockEntity GetOwnerGui(string dialogTitle, bool isOwner, int stallSelection)
        {
            return new GuiVinconMealStallOwner(dialogTitle, this.Inventory, isOwner, this.Pos, this.Api as ICoreClientAPI, stallSelection);
        }

        public override ItemStack GetProductOutputStack(int stallSlot)
        {
            return inventory.GetStall<MealStallSlot>(stallSlot).GenerateMealStack(Api);
        }

        //TODO: These are wrong. Fix them
        /*
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(BlockSelection selection, IPlayer forPlayer)
        {
            List<WorldInteraction> interactions = new List<WorldInteraction>();

            int index = GetStallSlotForSelectionIndex(selection.SelectionBoxIndex);
            ItemStack product = FindFirstNonEmptyStockStack(index);
            ItemSlot currency = GetCurrencyForStall(index);


            if (Owner != forPlayer.PlayerUID || VinconomyCoreSystem.ShouldForceCustomerScreen)
            {
                if (currency.Itemstack != null && product != null)
                {
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = [currency.Itemstack]

                    });

                    ItemStack fiveStack = currency.Itemstack.Clone();
                    fiveStack.StackSize = 5 * fiveStack.StackSize;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase-bulk",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sneak", "sprint"],
                        Itemstacks = [fiveStack]
                    });
                }
            }
            else
            {
                ItemStack firstSlot = product;
                if (firstSlot != null)
                {
                    ItemStack helpSlot = firstSlot.Clone();
                    helpSlot.StackSize = 1;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-add",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = [helpSlot]
                    });

                    ItemStack helpSlotStack = helpSlot.Clone();
                    helpSlotStack.StackSize = helpSlotStack.Collectible.MaxStackSize;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-add",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sneak", "sprint"],
                        Itemstacks = [helpSlotStack]
                    });

                    if (currency.Itemstack != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = [currency.Itemstack]

                        });

                        ItemStack fiveStack = currency.Itemstack.Clone();
                        fiveStack.StackSize = 5 * fiveStack.StackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase-bulk",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = ["sneak", "sprint"],
                            Itemstacks = [fiveStack]
                        });
                    }
                }
                else
                {
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-add",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak"
                    });
                }
            }

            interactions.Add(new WorldInteraction
            {
                ActionLangCode = "vinconomy:stall-open-menu",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = null
            });

            return interactions.ToArray();
        }
        */

    }
}
