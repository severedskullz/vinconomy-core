using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Trading;
using Commercially.Vinconomy.Trading.Processor;
using Commercially.Vinconomy.Util;
using System;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.Inventory.StallSlots
{
    public class MealStallSlot : BaseStallSlot, IContainedStallSlot
    {
        public override int StockSlotCount => 1;

        public override bool IsInitialized => MealSlot != null;

        public ItemSlot MealSlot;
        public int ServingCapacity { get; protected set; } = 64;

        public string RecipeCode { get; set; }

        public override ItemSlot this[int slotId] {
            get 
            {
                if (slotId == 0) return Currency;
                else if (slotId == 1) return Product;
                else return MealSlot;
            }
            set {
                if (slotId == 0) Currency = (CurrencySlot) value;
                else if (slotId == 1) Product = (ProductSlot) value;
                else MealSlot = value;
            } 
        }

        public MealStallSlot(VinconBaseInventory inventory, int stallSlot) : base(inventory, stallSlot)
        {
            StockItemSlot meal = new StockItemSlot(inventory, stallSlot, 0);
            meal.IsLocked = true;
            MealSlot = meal;
        }

        public override ItemSlot[] GetStockSlots()
        {
            return [MealSlot];
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("recipeCode", RecipeCode);
            tree.SetInt("numSlots", 1);
            tree.SetItemstack("slot0", MealSlot.Itemstack);
           
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            base.FromTreeAttributes(tree);
            RecipeCode = tree.GetString("recipeCode");
            ItemStack itemStack = tree.GetItemstack("slot0");
            MealSlot.Itemstack = itemStack;
            if (Inventory.Api?.World != null)
            {
                itemStack?.ResolveBlockOrItem(Inventory.Api.World);
            }
        }

        public AggregatedSlots GetProducts()
        {
            ICoreAPI api = Inventory.Api;
            AggregatedSlots slots = new GenericAggregatedSlots(api);

            if (MealSlot.Itemstack != null && TradingUtil.IsMatchingItem(Product.Itemstack, MealSlot.Itemstack, api.World))
            {
                slots.Add(MealSlot);
            }

            return slots;
        }


        public override int AddProductToSlot(IPlayer byPlayer, ItemSlot sourceSlot, bool bulk)
        {
            return AddProductToSlot(byPlayer, sourceSlot, bulk ? sourceSlot.StackSize : 1);
        }

        public override int AddProductToSlot(IPlayer byPlayer, ItemSlot source, int amount)
        {
            //TODO: Figure out how to convert this to an Int later on.
            return AddContents(source, amount) ? amount : 0;
        }

        public ItemStack[] GetProductContents()
        {
            BlockCookedContainerBase block = MealSlot?.Itemstack?.Block as BlockCookedContainerBase;
            if (block != null)
            {
                return block.GetContents(this.Inventory.Api.World, MealSlot.Itemstack);
            }
            return null;
        }

        public bool CanAcceptFrom(ItemSlot sourceSlot)
        {
            if (sourceSlot?.Itemstack == null) return false;

            if (VinUtils.IsEmptyContainer(sourceSlot.Itemstack, Inventory.Api) || !VinUtils.IsMealContainer(sourceSlot.Itemstack, Inventory.Api))
            {
                return false;
            }

            ItemStack[] sourceContents = VinUtils.GetContainerContents(sourceSlot.Itemstack, Inventory.Api);
            ItemStack[] productContents = GetProductContents();

            return VinUtils.IsMergableContents(Inventory.Api.World, sourceContents, productContents);
        }

        public bool AddContents(ItemSlot sourceSlot, int amount)
        {
            ItemStack sourceMeal = sourceSlot.Itemstack;
            if (sourceMeal == null)
                return false;

            IBlockMealContainer sourceMealBlock = sourceMeal.Block as IBlockMealContainer;
            if (sourceMealBlock == null) return false;

            IWorldAccessor world = Inventory.Api.World;


            float servings = sourceMealBlock.GetQuantityServings(world, sourceMeal);
            int servingsToTransfer = Math.Min(Math.Min(amount, (int)servings), ServingCapacity - MealSlot.StackSize);
            float remainingServings = servings - servingsToTransfer;

            if (servingsToTransfer <= 0)
                return false;

            if (MealSlot.Itemstack != null)
            {
                if (!CanMergeMeal(sourceMeal, MealSlot.Itemstack))
                {
                    return false;
                }
                MealSlot.Itemstack.StackSize += servingsToTransfer;
            }
            else
            {

                RecipeCode = sourceMealBlock.GetRecipeCode(world, sourceMeal);
                ItemStack productStack = GenerateMealProduct(sourceMeal, servingsToTransfer);
                MealSlot.Itemstack = productStack;
                Product.Itemstack = productStack.Clone();
                Product.MarkDirty();
            }

            

            //Remove the meal contents from the source block, converting it to the Eaten Block if neccessary
            sourceMeal.Attributes?.RemoveAttribute("sealed");
            if (remainingServings > 0)
            {
                sourceMealBlock.SetQuantityServings(world, sourceMeal, remainingServings);
            }
            else
            {
                // Check if we need to switch item stacks if its a BlockCookedContainer (AKA Cooking Pot) first, because cooking pot does not have `eatenBlock` attribute
                string code = sourceMeal.Block.Attributes["eatenBlock"].AsString();
                if (code == null)
                {
                    sourceMealBlock.SetContents(null, sourceMeal, null, 0);
                }
                else
                {
                    Block mealblock = world.GetBlock(new AssetLocation(code));
                    ItemStack stack = new ItemStack(mealblock);
                    sourceSlot.Itemstack = stack;
                }

            }

            MealSlot.OnItemSlotModified(MealSlot.Itemstack);
            MealSlot.MarkDirty();
            sourceSlot.MarkDirty();
            this.Inventory.BlockEntity.MarkDirty(true);
            return true;
        }

        public bool RemoveContents(ItemSlot targetSlot, int amount)
        {
            if (MealSlot.Itemstack == null)
            {
                return false;
            }

            ItemStack targetMeal = targetSlot.Itemstack;
            if (targetMeal == null)
                return false;

            IWorldAccessor world = Inventory.Api.World;

            // Fuck you Tyron for changing this shit yet again. As if dealing with Bowls wasnt bad enough...
            if (targetMeal.Block is BlockCookingContainer emptyPot)
            {
                Block block = world.GetBlock(targetMeal.Block.CodeWithVariant("type", "cooked"));
                ItemStack mealStack = new ItemStack(block);
                IBlockMealContainer mealStackBlock = mealStack.Block as IBlockMealContainer;
                IBlockMealContainer stallMealBlock = MealSlot.Itemstack.Block as IBlockMealContainer;

                int targetCapacity = targetMeal.Block.Attributes["servingCapacity"].AsInt(0);
                int servingsToTransfer = Math.Min(Math.Min(amount, MealSlot.StackSize), (int)(targetCapacity));
                ItemStack[] stallContents = stallMealBlock.GetContents(world, MealSlot.Itemstack);
                RecipeCode = stallMealBlock.GetCookingRecipe(world, MealSlot.Itemstack).Code;
                mealStackBlock.SetContents(RecipeCode, mealStack, stallContents, servingsToTransfer);
                targetSlot.Itemstack = mealStack;
                MealSlot.Itemstack.StackSize -= servingsToTransfer;
                if (MealSlot.StackSize <= 0)
                {
                    MealSlot.Itemstack = null;
                    RecipeCode = null;
                }
            }
            else
            {
                IBlockMealContainer targetMealBlock = targetMeal.Block as IBlockMealContainer;
                if (targetMealBlock == null) return false;



                float targetServings = targetMealBlock.GetQuantityServings(world, targetMeal);
                int targetCapacity = targetMeal.Block.Attributes["servingCapacity"].AsInt(0);
                int servingsToTransfer = Math.Min(Math.Min(amount, MealSlot.StackSize), (int)(targetCapacity - targetServings));


                if (servingsToTransfer <= 0)
                    return false;

                //If container is not empty
                if (targetMealBlock.GetNonEmptyContents(world, targetMeal).Length > 0)
                {
                    if (!CanMergeMeal(targetMeal, MealSlot.Itemstack))
                    {
                        return false;
                    }
                    targetMealBlock.SetQuantityServings(world, targetMeal, targetServings + servingsToTransfer);
                }
                else
                {
                    IBlockMealContainer stallMealBlock = MealSlot.Itemstack.Block as IBlockMealContainer;
                    targetMealBlock.SetContents(RecipeCode, targetMeal, stallMealBlock.GetContents(world, MealSlot.Itemstack), servingsToTransfer);
                }

                MealSlot.Itemstack.StackSize -= servingsToTransfer;
                if (MealSlot.StackSize <= 0)
                {
                    MealSlot.Itemstack = null;
                    RecipeCode = null;
                }

                targetMeal.Attributes?.RemoveAttribute("sealed");
            }

            MealSlot.OnItemSlotModified(MealSlot.Itemstack);
            MealSlot.MarkDirty();
            targetSlot.MarkDirty();
            return true;
        }

        public bool CanMergeMeal(ItemStack source, ItemStack target)
        {
            IWorldAccessor world = Inventory.Api.World;

            if (target == null)
                return true;


            IBlockMealContainer containerFrom = source.Block as IBlockMealContainer;
            if (containerFrom == null)
            {
                return false;
            }

            ItemStack[] contentsFrom = containerFrom.GetNonEmptyContents(world, source);
            string recipeCodeFrom = containerFrom.GetRecipeCode(world, source);




            // check if ingredients match
            IBlockMealContainer sourceMeal = target.Block as IBlockMealContainer;
            ItemStack[] sourceContents = sourceMeal.GetContents(world, target);
            string recipeCodeTo = sourceMeal.GetRecipeCode(world, target);

            // check if recipe code matches
            if (recipeCodeTo != recipeCodeFrom)
            {
                return false;
            }


            if (sourceContents.Length != 0)
            {
                if (sourceContents.Length != contentsFrom.Length)
                {
                    return false;
                }

                for (int i = 0; i < contentsFrom.Length; i++)
                {
                    ItemStack bowlStack = contentsFrom[i];
                    ItemStack containerStack = sourceContents[i];

                    if (bowlStack.Id != containerStack.Id)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool MatchesProduct(ItemStack itemStack)
        {
            return CanMergeMeal(itemStack, MealSlot.Itemstack);
        }

        public override int GetNumPurchasesRemaining()
        {
            return base.GetNumPurchasesRemaining();
        }


        public override int GetTotalProductAvailable()
        {
            if (Product?.Itemstack == null) return 0;

            ItemSlot[] items = GetStockSlots();
            int amount = 0;
            foreach (ItemSlot item in items)
            {
                if (MatchesProduct(item.Itemstack))
                {
                    amount += item.StackSize;
                }
            }

            return amount;
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

        public CapacityAggregatedSlots GetRequiredContainers(IPlayer player)
        {
            ServingCapacityAggregatedSlots aggregatedSlots = new ServingCapacityAggregatedSlots(Inventory.Api);
            ItemStack[] mealStacks = GetProductContents();


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

        private bool CanHoldMeal(ItemStack[] mealStacks, ItemStack dest)
        {
            return VinUtils.IsMealContainer(dest, Inventory.Api)
                && VinUtils.IsMergableContents(Inventory.Api.World, mealStacks, VinUtils.GetContainerContents(dest, Inventory.Api));
        }

        public string GetRecipeCode()
        {
            return VinUtils.GetRecipeCode(Product.Itemstack, Inventory.Api);
        }

        public override void DropInventory(Vec3d pos, int maxStackSize, bool markDirty = false)
        {
            // DO NOTHING. Meals go bye-bye! Dont wanna duplicate the cooking pots I use to hold the ingredients. Pretend they spilled on the floor and got dirty or something, I don't care.
        }

        public ItemStack TransferToItemStack(ItemSlot containerSlot, string recipe, ItemStack[] mealStacks, int servings, out int moved)
        {
            int capacity = 0;
            moved = 0;

            ICoreAPI api = Inventory.Api;

            // Why the fuck isnt the servingCapacity also on the meal block code?
            // I have to be missing something here.
            JsonObject attr = containerSlot.Itemstack.Block.Attributes;
            if (attr.KeyExists("servingCapacity"))
            {
                capacity = attr["servingCapacity"].AsInt();
            }
            if (capacity <= 0)
            {
                return null;
            }

            if (containerSlot.Itemstack.Block is IBlockMealContainer meal)
            {
                int currentServings = (int)Math.Ceiling(meal.GetQuantityServings(api.World, containerSlot.Itemstack));
                if (currentServings >= capacity)
                    return null;

                moved = Math.Min(servings, capacity - currentServings);
                meal.SetContents(recipe, containerSlot.Itemstack, mealStacks, currentServings + moved);
                containerSlot.Itemstack.Attributes.RemoveAttribute("sealed");
                containerSlot.MarkDirty();
                return containerSlot.TakeOut(1);
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

                    moved = Math.Min(servings, capacity);
                    mealBlock.SetContents(recipe, mealStack, mealStacks, moved);
                    containerSlot.TakeOut(1);
                    containerSlot.MarkDirty();
                    return mealStack;
                    
                }
            }

            return null;
        }

        public AggregatedStacks ExtractProduct(int totalProductNeeded, int numPurchases, CapacityAggregatedSlots containerSourceSlots, bool isAdminShop)
        {
            AggregatedStacks result = new AggregatedStacks();

            IBlockMealContainer mealContainer = Product.Itemstack.Block as IBlockMealContainer;
            if (mealContainer == null)
                return result;

            string recipeCode = mealContainer.GetRecipeCode(Inventory.Api.World, Product.Itemstack);
            ItemStack[] mealStacks = mealContainer.GetContents(Inventory.Api.World, Product.Itemstack);

            int totalServingsLeftToTransfer = totalProductNeeded;
            foreach (ItemSlot containerSlot in containerSourceSlots)
            {
                // Save stacksize as variable. We will be taking items OUT of this stack, so it would exit the loop early.
                // Eg. Had 2 bowls, loop ran, took one out, 'i' is now 1, and stack size is 1, so loop terminates and doesnt run on second bowl.
                int numAttempts = containerSlot.StackSize;
                for (int i = 0; i < numAttempts; i++)
                {
                    int capacity = containerSlot.Itemstack.Block.Attributes["servingCapacity"].AsInt();
                    int servingsToTransfer = Math.Min(totalServingsLeftToTransfer, capacity);
                    ItemStack mealStack = TransferToItemStack(containerSlot, recipeCode, mealStacks, totalServingsLeftToTransfer, out int moved);
                    ExtractProduct(moved, numPurchases, isAdminShop);
                    totalServingsLeftToTransfer -= moved;



                    result.Add(mealStack);
                    if (totalServingsLeftToTransfer <= 0)
                        break;
                }

                if (totalServingsLeftToTransfer <= 0)
                    return result;

            }

            if (totalServingsLeftToTransfer > 0)
            {
                GenericTradingProcessor.AuditLogError(null, "Somehow allowed purchase of " + totalServingsLeftToTransfer + " extra servings even though we didnt have enough containers");
            }
            return result;
        }

        public override AggregatedStacks ExtractProduct(int amount, int numPurchases, bool isAdminOwned)
        {
            AggregatedStacks result = new AggregatedStacks();
            if (!isAdminOwned)
            {
                //This is mostly just to follow convention, but the *Typical* case is that the items from the container slots are added to the result stacks instead, not this.
                ItemStack taken = MealSlot.TakeOut(amount);
                if (taken != null)
                    result.Add(taken);
            }
            return result;
        }

        public ItemStack GenerateMealProduct(ItemStack origStack, int desiredServings)
        {
            if (origStack == null) return null;

            IWorldAccessor world = Inventory.Api.World;
            IBlockMealContainer sourceMealBlock = origStack.Block as IBlockMealContainer;

            Block generatedMealBlock = world.GetBlock("game:claypot-black-cooked");
            IBlockMealContainer genMeal = generatedMealBlock as IBlockMealContainer; // While 9 out of 10 times this is probably going to have the same implementation, better safe than sorry.
            ItemStack stack = new ItemStack(generatedMealBlock);
            genMeal.SetContents(RecipeCode, stack, sourceMealBlock.GetContents(world, origStack), 1);
            stack.StackSize = desiredServings;
            return stack;
        }
    }
}
