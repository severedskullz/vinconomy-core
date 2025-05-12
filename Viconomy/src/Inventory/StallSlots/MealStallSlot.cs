using System;
using System.Collections.Generic;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.StallSlots
{
    public class MealStallSlot : StallSlotBase
    {
        public ItemSlot[] slots;
        //public string RecipeCode;
        public int Capacity;

        public MealStallSlot(InventoryBase inventory, int stallSlot, int numSlots) : base(inventory)
        {
            slots = new ItemSlot[4];
            for (int i = 0; i < numSlots; i++)
            {
                slots[i] = new ViconItemSlot(inventory, stallSlot, i);

            }
        }

        public override ItemSlot FindFirstNonEmptyStockSlot()
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.Itemstack != null)
                    return slot;
            }
            return null;
        }

        public override ItemSlot GetSlot(int itemSlot)
        {
            return slots[itemSlot];
        }

        public override ItemSlot[] GetSlots()
        {
            return slots;
        }

        public override void SetSlot(int itemSlot, ItemSlot value)
        {
            slots[itemSlot] = value;
        }

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

        public override int GetProductQuantity()
        {
            ItemSlot[] items = GetSlots();
            int amount = 0;
            bool found = false;
            foreach (ItemSlot item in items)
            {
                if (item?.Itemstack != null)
                {
                    if (found) {
                        amount = Math.Min(amount, item.Itemstack.StackSize);
                    } else
                    {
                        found = true;
                        amount = item.Itemstack.StackSize;
                    }
                   
                }
            }

            return amount;
        }

        public override string GetProductName(ICoreAPI api)
        {
            return GetOutputName(api);
        }

        public string GetOutputName(ICoreAPI api)
        {
            
            ItemStack[] stacks = GetMealStacks();
            CookingRecipe recipe = GetMatchingCookingRecipe(api);
            if (recipe == null)
                return "Unkown Meal";
            return recipe.GetOutputName(api.World, stacks);
        }

        public string GetRecipeCode(ICoreAPI api)
        {
            CookingRecipe rec = GetMatchingCookingRecipe(api);
            return rec?.Code;
        }

        public CookingRecipe GetMatchingCookingRecipe(ICoreAPI api)
        {
            List<CookingRecipe> recipes = api.GetCookingRecipes();
            if (recipes == null) return null;

            ItemStack[] stacks = GetMealStacks();

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
            foreach (ItemSlot foodSlot in slots)
            {
                if (foodSlot.Itemstack != null)
                {
                    foodSlot.Itemstack.StackSize -= amount;
                    if (foodSlot.StackSize < 0)
                    {
                        foodSlot.Itemstack = null;
                    }
                }
            }
        }
    }
}