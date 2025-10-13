using System;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Slots;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.Impl
{
    public class ViconLiquidInventory : ViconBaseInventory
    {
        public ViconLiquidInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(stall, inventoryID, api, numStalls, numStacksPerStall)
        {
            PutLocked = true;
            TakeLocked = true;
            InitializeStalls();
        }

        protected override void InitializeStalls()
        {
            StallSlots = new LiquidStallSlot[NumStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new LiquidStallSlot(this, i, 50);
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
                LiquidStallSlot stall = (LiquidStallSlot)StallSlots[i];
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
                LiquidStallSlot stall = (LiquidStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.ToTreeAttributes(stallTree);
            }
            if (ChiselDecoSlot.Itemstack != null)
            {
                tree.SetItemstack("decoBlock", ChiselDecoSlot.Itemstack.Clone());
            }
        }

        public bool AddLiquidToStall(int stall, ItemSlot mealSlot, int amount)
        {
            // Look, I dont feel like trying to manually manipulate item stacks just so players cant add/remove from the inventory manually.
            // There really needs to be a way to distinguish between PLAYERS adding/removing items and CODE.
            PutLocked = false;
            bool result = AddLiquidToStallRaw(stall, mealSlot, amount);
            PutLocked = true;
            return result;
        }

        private bool AddLiquidToStallRaw(int stall, ItemSlot sourceSlot, int amount)
        {
            ItemStack sourceStack = sourceSlot.Itemstack;
            BlockLiquidContainerBase container = sourceStack.Block as BlockLiquidContainerBase;
            if (container == null)
            {
                return false;
            }

            ItemStack[] contents = container.GetNonEmptyContents(Api.World, sourceStack);
            float currentLiters = container.GetCurrentLitres(sourceStack);
            int toTransfer = Math.Min(amount, (int)currentLiters);
            if (contents != null)
            {
                LiquidStallSlot slot = GetStall<LiquidStallSlot>(stall);

                ItemStack containerStack = slot.GetSlot(0).Itemstack;
                // check if ingredient count matches
         
                // check if recipe code matches         
                if (contents.Length != 1)
                {
                    return false;
                }

                ItemStack bowlStack = contents[0];

                if (containerStack != null && bowlStack.Id != containerStack.Id)
                {
                    return false;
                }
                    
                
                   

                ItemStack ing = new ItemStack(contents[0].Item);
                ing.StackSize = toTransfer;
                ItemSlot fromSlot = new DummySlot(ing);
                ItemSlot toSlot = slot.GetSlot(0);
                int amountMoved = fromSlot.TryPutInto(Api.World, toSlot, ing.StackSize);
                // In almost every case, this should *ALWAYS* be the same because you cant have a meal with different amounts of ingredients, but *JUST* to be sure.
                int litersTransfered = Math.Max(0, amountMoved);
                toSlot.MarkDirty();
                        
                
                currentLiters = currentLiters - litersTransfered;
                if (currentLiters > 0)
                {
                    container.SetCurrentLitres(sourceStack, currentLiters);
                } else
                {
                    container.SetContents(sourceStack, null);
                }
                sourceSlot.MarkDirty();
                return true;
            }
            
            return false;
        }
    }
}
