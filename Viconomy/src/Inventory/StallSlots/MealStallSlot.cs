using System;
using System.Collections.Generic;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.StallSlots
{
    public class MealStallSlot : StallSlotBase
    {
        public ItemSlot[] slots;
        //public string RecipeCode;
        public int Capacity;

        public MealStallSlot(InventoryBase inventory, int stallSlot, int numSlots) : base(inventory, numSlots)
        {
            slots = new ItemSlot[numSlots];
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

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < ProductStacksPerStall)
                {
                    return slots[slotId];
                }
                else
                    return Currency;

            }
            set
            {
                if (slotId < ProductStacksPerStall)
                {
                    slots[slotId] = (ViconItemSlot)value;
                }
                else
                    Currency = (ViconCurrencySlot)value;
            }
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
                    if (foodSlot.StackSize <= 0)
                    {
                        foodSlot.Itemstack = null;
                    }
                    foodSlot.MarkDirty();
                }
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("purchaseQuantity", ItemsPerPurchase);
            if (Currency.Itemstack != null)
            {
                tree.SetItemstack("currency", Currency.Itemstack);
            }

            for (int j = 0; j < ProductStacksPerStall; j++)
            {
                if (slots[j].Itemstack != null)
                {
                    tree.SetItemstack("slot" + j, slots[j].Itemstack);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            ItemsPerPurchase = tree.GetInt("purchaseQuantity", 1);

            Currency.Itemstack = tree.GetItemstack("currency");
            for (int j = 0; j < ProductStacksPerStall; j++)
            {
                slots[j].Itemstack = tree.GetItemstack("slot" + j);

            }
        }

        public ItemStack GenerateMealStack(ICoreAPI api)
        {
            Block mealblock = api.World.GetBlock("game:claypot-gray-cooked");
            IBlockMealContainer meal = mealblock as IBlockMealContainer;
            ItemStack stack = new ItemStack(mealblock);
            meal.SetContents(GetRecipeCode(api), stack, GetMealStacks(), ItemsPerPurchase);
            stack.StackSize = ItemsPerPurchase;
            return stack;
        }
    }
}