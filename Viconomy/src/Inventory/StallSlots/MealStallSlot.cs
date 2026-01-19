using System;
using System.Collections.Generic;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.StallSlots
{
    public class MealStallSlot : StallSlotBase
    {
        public ItemSlot Meal;
        //public ItemSlot[] slots;
        public string RecipeCode;
        public int Capacity;

        public MealStallSlot(InventoryBase inventory, int stallSlot, int numSlots) : base(inventory, 1)
        {
            Meal = new ViconLockedSlot(inventory, stallSlot);
            Capacity = 64;
        }

        public override ItemSlot FindFirstNonEmptyStockSlot()
        {
            if (Meal.Itemstack == null) return null;
            return Meal;
        }

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId == 0)
                {
                    return Meal;
                }
                else
                    return Currency;

            }
            set
            {
                if (slotId == 0)
                {
                    Meal = (ViconItemSlot)value;
                }
                else
                    Currency = (ViconCurrencySlot)value;
            }
        }

        public override ItemSlot GetSlot(int itemSlot)
        {
            return Meal;
        }

        public override ItemSlot[] GetSlots()
        {
            return [Meal];
        }

        public override void SetSlot(int itemSlot, ItemSlot value)
        {
            Meal = value;
        }

        /*
        public ItemStack[] GetMealStacks()
        {
            List<ItemStack> stacks = new List<ItemStack>();
            foreach (ItemSlot slot in slots)
            {
                if (slot.Itemstack != null)
                {
                    ItemStack stack = slot.Itemstack.Clone();
                    stack.StackSize = 1;
                    stacks.Add(stack);
                }
                    
            }
            return stacks.ToArray();
        }
        */

        public ItemStack[] GetMealContents()
        {
            ItemStack meal = Meal.Itemstack;
            IBlockMealContainer containerFrom = meal?.Block as IBlockMealContainer;
            if (containerFrom == null)
            {
                return [];
            }

            return containerFrom.GetNonEmptyContents(this.Inventory.Api.World, meal);
        }

        public override int GetProductQuantity()
        {
            return Meal.StackSize;
        }

        public override string GetProductName(ICoreAPI api)
        {
            return GetOutputName(api);
        }

        public string GetOutputName(ICoreAPI api)
        {
            
            ItemStack[] stacks = GetMealContents();
            CookingRecipe recipe = api.GetCookingRecipe(RecipeCode); //GetMatchingCookingRecipe(api);
            if (recipe == null)
                return "Unkown Meal";
            return recipe.GetOutputName(api.World, stacks);
        }

        public CookingRecipe GetMatchingCookingRecipe(ICoreAPI api)
        {
            List<CookingRecipe> recipes = api.GetCookingRecipes();
            if (recipes == null) return null;

            ItemStack[] stacks = GetMealContents();

            foreach (var recipe in recipes)
            {
                //if (recipe.CooksInto == null) continue;   // Prevent normal food from being cooked in a dirty pot
                if (recipe.Matches(stacks))
                {
                    //if (recipe.GetQuantityServings(stacks) > MaxServingSize) continue;

                    return recipe;
                }
            }

            return null;
        }

        public void RemoveServings(int amount)
        {
            if (Meal.Itemstack != null)
            {
                Meal.Itemstack.StackSize -= amount;
                if (Meal.StackSize <= 0)
                {
                    Meal.Itemstack = null;
                    RecipeCode = null;
                }
            }
            Meal.MarkDirty();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("purchaseQuantity", ItemsPerPurchase);
            tree.SetItemstack("currency", Currency.Itemstack);
            tree.SetItemstack("mealslot", Meal.Itemstack);
            tree.SetString("recipe", RecipeCode);
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            ItemsPerPurchase = tree.GetInt("purchaseQuantity", 1);
            Currency.Itemstack = tree.GetItemstack("currency");
            Meal.Itemstack = tree.GetItemstack("mealslot");
            RecipeCode = tree.GetString("recipe");
        }

        public bool AddMeal(ItemSlot sourceSlot, int amount)
        {
            ItemStack sourceMeal = sourceSlot.Itemstack;
            if (sourceMeal == null)
                return false;

            IBlockMealContainer sourceMealBlock = sourceMeal.Block as IBlockMealContainer;
            if (sourceMealBlock == null) return false;

            IWorldAccessor world = Inventory.Api.World;


            float servings = sourceMealBlock.GetQuantityServings(world, sourceMeal);
            int servingsToTransfer = Math.Min(Math.Min(amount, (int)servings), Capacity - Meal.StackSize);
            float remainingServings = servings - servingsToTransfer;

            if (servingsToTransfer <= 0)
                return false;

            if (Meal.Itemstack != null)
            {
                if (!CanMergeMeal(sourceMeal, Meal.Itemstack))
                {
                    return false;
                }

                //Disabling for now. Not sure why this is causing MarkDirty() below to throw a NRE for the transitionables...
                /*
                IBlockMealContainer stallMealBlock = mealSlot.Itemstack.Block as IBlockMealContainer;
                ItemStack[] contents = stallMealBlock.GetContents(world, mealSlot.Itemstack);
                for (int i = 0; i < contents.Length; i++)
                {
                    if (contents[i] != null)
                        contents[i].Attributes.GetTreeAttribute("transitionstate")?.SetFloat("transitionedHours", 0);
                }

                //transitionstate.transitionedHours
                stallMealBlock.SetContents(stallMealBlock.GetRecipeCode(world,mealSlot.Itemstack), mealSlot.Itemstack, contents);
                */

                Meal.Itemstack.StackSize += servingsToTransfer;
            }
            else
            {

                RecipeCode = sourceMealBlock.GetRecipeCode(world, sourceMeal);

                Block generatedMealBlock = world.GetBlock("game:claypot-gray-cooked");
                IBlockMealContainer genMeal = generatedMealBlock as IBlockMealContainer; // While 9 out of 10 times this is probably going to have the same implementation, better safe than sorry.
                ItemStack stack = new ItemStack(generatedMealBlock);
                genMeal.SetContents(RecipeCode, stack, sourceMealBlock.GetContents(world, sourceMeal), 1);
                stack.StackSize = servingsToTransfer;
                Meal.Itemstack = stack;
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

            Meal.MarkDirty();
            sourceSlot.MarkDirty();
            return true;
        }

        public bool RemoveMeal(ItemSlot targetSlot, int amount)
        {
            if (Meal.Itemstack == null)
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
                IBlockMealContainer stallMealBlock = Meal.Itemstack.Block as IBlockMealContainer;

                int targetCapacity = targetMeal.Block.Attributes["servingCapacity"].AsInt(0);
                int servingsToTransfer = Math.Min(Math.Min(amount, Meal.StackSize), (int)(targetCapacity));
                ItemStack[] stallContents = stallMealBlock.GetContents(world, Meal.Itemstack);
                mealStackBlock.SetContents(RecipeCode, mealStack,stallContents, servingsToTransfer);
                targetSlot.Itemstack = mealStack;
                Meal.Itemstack.StackSize -= servingsToTransfer;
                if (Meal.StackSize <= 0)
                {
                    Meal.Itemstack = null;
                    RecipeCode = null;
                }
            } 
            else
            {
                IBlockMealContainer targetMealBlock = targetMeal.Block as IBlockMealContainer;
                if (targetMealBlock == null) return false;



                float targetServings = targetMealBlock.GetQuantityServings(world, targetMeal);
                int targetCapacity = targetMeal.Block.Attributes["servingCapacity"].AsInt(0);
                int servingsToTransfer = Math.Min(Math.Min(amount, Meal.StackSize), (int)(targetCapacity - targetServings));


                if (servingsToTransfer <= 0)
                    return false;

                //If container is not empty
                if (targetMealBlock.GetNonEmptyContents(world, targetMeal).Length > 0)
                {
                    if (!CanMergeMeal(targetMeal, Meal.Itemstack))
                    {
                        return false;
                    }
                    targetMealBlock.SetQuantityServings(world, targetMeal, targetServings + servingsToTransfer);
                }
                else
                {
                    IBlockMealContainer stallMealBlock = Meal.Itemstack.Block as IBlockMealContainer;
                    targetMealBlock.SetContents(RecipeCode, targetMeal, stallMealBlock.GetContents(world, Meal.Itemstack), servingsToTransfer);
                }

                Meal.Itemstack.StackSize -= servingsToTransfer;
                if (Meal.StackSize <= 0)
                {
                    Meal.Itemstack = null;
                    RecipeCode = null;
                }

                targetMeal.Attributes?.RemoveAttribute("sealed");
            }

            Meal.MarkDirty();
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

            // check if recipe code matches
            if (RecipeCode != recipeCodeFrom)
            {
                return false;
            }


            // check if ingredients match
            IBlockMealContainer sourceMeal = target.Block as IBlockMealContainer;
            ItemStack[] sourceContents = sourceMeal.GetContents(world, target);
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
    }
}