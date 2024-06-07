using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.Inventory
{
    public class ViconomyGachaInventory : InventoryBase, ISlotProvider
    {
        BEViconGacha stall;

        private ItemSlot[] slots;

        public ItemSlot[] Slots => slots;

        public override int Count => slots.Length;

        public override ItemSlot this[int slotId] { get => slots[slotId]; set => slots[slotId] = value; }

        //ViconomyCoreSystem modSystem;

        public ViconomyGachaInventory(int numSlots, string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {

            this.slots = new ItemSlot[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                this.slots[i] = NewSlot(i);
            }

        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            //modSystem = Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
        }

        protected override ItemSlot NewSlot(int id)
        {
            if (id == 0)
                return new ViconCurrencySlot(this);
            else
                return new ViconGachaSlot(this, id);
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            return 0;
        }

        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            using IEnumerator<ItemSlot> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                ItemSlot current = enumerator.Current;
                if (current.Itemstack == null || current is ViconCurrencySlot)
                {
                    continue;
                }

                if (maxStackSize > 0)
                {
                    while (current.StackSize > 0)
                    {
                        ItemStack itemstack = current.TakeOut(GameMath.Clamp(current.StackSize, 1, maxStackSize));
                        Api.World.SpawnItemEntity(itemstack, pos);
                    }
                }
                else
                {
                    Api.World.SpawnItemEntity(current.Itemstack, pos);
                }

                current.Itemstack = null;
                current.MarkDirty();
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            this.slots = this.SlotsFromTreeAttributes(tree, slots, null);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.SlotsToTreeAttributes(this.slots, tree);
        }

        public ItemSlot GetCurrency()
        {
            return slots[0];
        }

        public bool HasProduct()
        {
            return true;
        }

        public double GetChanceForSlot(int slot, bool useTotalRandomizer)
        {
            double percent = 0;
            ItemSlot item = slots[slot];
            if (item.StackSize > 0)
            {
                if (useTotalRandomizer)
                {
                    percent = Math.Round(((double)item.StackSize / (double)GetTotalItems()) * 100, 2);
                }
                else
                {
                    percent = Math.Round(((double)1 / (double) GetNonEmptySlotCount()) * 100, 2);
                }
            }
            return percent;
        }

        public ViconGachaSlot GetRandomItem(bool isPickAbsolute)
        {
            ItemSlot[] filled = GetNonEmptySlots();
            if (filled.Length == 0)
            {
                return null;
            }

            ThreadSafeRandom random = new ThreadSafeRandom(DateTime.Now.Millisecond);
            if (isPickAbsolute)
            {
                double percent = random.NextDouble();
                int totalItems = GetTotalItems();
                ViconGachaSlot slot = null;
                for (int i = 0; i < filled.Length; i++)
                {
                    slot = (ViconGachaSlot) filled[i];
                    double curPercent = (double)slot.StackSize / (double)totalItems;
                    // Given we have 3 slots filled, Say the slots have 100, 80, and 20 items in their respective stacks.
                    // These slots would be worth 50%, 40% and 10% respectively.
                    // Check if the current value of percent is less than the stack's percentage worth.
                    // If we "rolled" a 75, and the stack takes up 50% of all the items, then it is not a "hit" and we continue.
                    // We then subtract the 50% from the 75%, leaving us with 25%.
                    // The next remaining stack is also 40% of the total itmes, which means the remaining 25% is less than the next item's 40% so it is a "Hit" and we return that item.
                    // If for SOME reason (like due to double precision issues) we dont get a hit on all the items, the last stack is returned as a safe-guard. Shouldnt happen though.
                    if ( percent <= curPercent)
                    {
                        break;
                    } else
                    {
                        percent -= curPercent;
                    }
                }

                return slot;
            } else
            {
                return (ViconGachaSlot) filled[random.Next(filled.Length)];
            }
        }

        public int GetNonEmptySlotCount()
        {
            return GetNonEmptySlots().Length;
        }

        private ItemSlot[] GetNonEmptySlots()
        {
            List<ItemSlot> filledSlots = new List<ItemSlot>();
            for (int i = 1; i < slots.Length; i++)
            {
                if (slots[i].Itemstack != null)
                {
                    filledSlots.Add(slots[i]);
                }
            }
            return filledSlots.ToArray();
        }

        public int GetTotalItems()
        {
            int total = 0;
            // Start at 1 to bypass currency slot
            for (int i = 1;  i < slots.Length;  i++)
            {
                total += slots[i].StackSize;
            }
            return total;
        }
    }
}
