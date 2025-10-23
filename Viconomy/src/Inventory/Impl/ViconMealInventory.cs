using System;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Slots;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.Impl
{
    public class ViconMealInventory : ViconBaseInventory
    {
        public ViconMealInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(stall, inventoryID, api, numStalls, numStacksPerStall)
        {
            PutLocked = true;
            TakeLocked = true;
            InitializeStalls();
        }

        protected override void InitializeStalls()
        {
            StallSlots = new MealStallSlot[NumStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new MealStallSlot(this, i, ProductStacksPerStall);
            }
        }


        protected override ItemSlot NewSlot(int slotId)
        {
            if (slotId == 0)
            {
                return new ViconDecoBlockSlot(this, 0);
            }

            int index = slotId - 1;
            int itemSlot = index % StallSlotSize;
            if (itemSlot == ProductStacksPerStall)
            {
                return new ViconCurrencySlot(this);
            }
            return new ItemSlot(this);
        }



        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                MealStallSlot stall = (MealStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.FromTreeAttributes(stallTree);
            }
            ChiselDecoSlot.Itemstack = tree.GetItemstack("decoBlock");

            ResolveBlocksOrItems();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            //base.SlotsToTreeAttributes(this.slots, tree);
            for (int i = 0; i < NumStalls; i++)
            {
                MealStallSlot stall = (MealStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.ToTreeAttributes(stallTree);
            }
            if (ChiselDecoSlot.Itemstack != null)
            {
                tree.SetItemstack("decoBlock", ChiselDecoSlot.Itemstack.Clone());
            }
        }

        public bool AddMealToStall(int stall, ItemSlot mealSlot, int amount)
        {
            // Look, I dont feel like trying to manually manipulate item stacks just so players cant add/remove from the inventory manually.
            // There really needs to be a way to distinguish between PLAYERS adding/removing items and CODE.
            PutLocked = false;
            bool result = AddMealToStallRaw(stall, mealSlot, amount);
            PutLocked = true;
            return result;
        }

        private bool AddMealToStallRaw(int stall, ItemSlot mealSlot, int amount)
        {
            ItemStack meal = mealSlot.Itemstack;
            IBlockMealContainer container = meal.Block as IBlockMealContainer;
            if (container == null)
            {
                return false;
            }

            ItemStack[] contents = container.GetNonEmptyContents(Api.World, meal);
            float servings = container.GetQuantityServings(Api.World, meal);
            int toTransfer = Math.Min(amount, (int)servings);
            if (contents != null)
            {
                MealStallSlot slot = GetStall< MealStallSlot>(stall);

                ItemStack[] ingSlots = slot.GetMealStacks();
                // check if ingredient count matches
         
                // check if recipe code matches
                if (ingSlots.Length != 0) {
                    if (ingSlots.Length != contents.Length)
                    {
                        return false;
                    }

                    for (int i = 0; i < contents.Length; i++)
                    {
                        ItemStack bowlStack = contents[i];
                        ItemStack containerStack = ingSlots[i];

                        if (bowlStack.Id != containerStack.Id)
                        {
                            return false;
                        }
                    }
                }
                   

                int servingsTransfered = 0;
                for (int i = 0; i < contents.Length; i++)
                {
                    ItemStack ing = new ItemStack(contents[i].Item);
                    ing.StackSize = toTransfer;
                    ItemSlot fromSlot = new DummySlot(ing);
                    ItemSlot toSlot = slot.GetSlot(i);
                    int amountMoved = fromSlot.TryPutInto(Api.World, toSlot, ing.StackSize);
                    // In almost every case, this should *ALWAYS* be the same because you cant have a meal with different amounts of ingredients, but *JUST* to be sure.
                    servingsTransfered = Math.Max(servingsTransfered, amountMoved);
                    toSlot.MarkDirty();
                        
                }
                servings = servings - servingsTransfered;
                if (servings > 0)
                {
                    container.SetQuantityServings(Api.World, meal, servings);
                    meal.Attributes?.RemoveAttribute("sealed");
                } else
                {
                    // Check if its a BlockCookedContainer (AKA Cooking Pot) first, because cooking pot does not have `eatenBlock` attribute
                    //TODO: Could probably reverse this logic and just check for eatenBlock first and convert item, otherwise just set contents
                    if ( meal.Block is BlockCookedContainerBase)
                    {
                        container.SetContents(null, meal, null, 0);
                        meal.Attributes?.RemoveAttribute("sealed");
                    } else
                    {
                        string code = meal.Block.Attributes["eatenBlock"].AsString();
                        if (code == null) return false;
                        Block mealblock = Api.World.GetBlock(new AssetLocation(code));
                        ItemStack stack = new ItemStack(mealblock);
                        mealSlot.Itemstack = stack;
                    }
                    
                }
                mealSlot.MarkDirty();
                return true;
            }
            
            return false;
        }

        public ItemStack[] GetMealContents(int stallSlot)
        {
            StallSlotBase stall = GetStall<MealStallSlot >(stallSlot);
            ItemStack[] stacks = new ItemStack[4];

            for (int i = 0; i < stacks.Length; i++)
            {
                stacks[i] = stall[i].Itemstack?.Clone();
            }

            return stacks;
        }

    }
}
