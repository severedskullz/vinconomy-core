using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Inventory.StallSlots;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Viconomy.Inventory.Impl
{
    public abstract class ViconomyBaseInventory<E> : InventoryBase where E : ItemSlot
    {
        protected VinconomyCoreSystem modSystem;
        protected BEVinconBase Stall;

        // The amount of different item groups there are for this inventory
        protected int NumStalls = 4;
        // the amount of items per item group
        protected int NumStacksPerStall = 9;
        public int StallSlotSize = 10;
        public ViconDecoBlockSlot ChiselDecoSlot;

        public StallSlotBase<E>[] StallSlots;

        protected ViconomyBaseInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(inventoryID, api)
        {
            Stall = stall;
            NumStalls = numStalls;
            NumStacksPerStall = numStacksPerStall;
            StallSlotSize = NumStacksPerStall + 1;

            InitializeStalls();
            ChiselDecoSlot = new ViconDecoBlockSlot(this, 0);

        }

        protected abstract void InitializeStalls();

        public override int Count { get { return StallSlotSize * NumStalls + 1; } }

        public override void ResolveBlocksOrItems()
        {
            using IEnumerator<ItemSlot> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                ItemSlot current = enumerator.Current;
                if (current.Itemstack != null && !current.Itemstack.ResolveBlockOrItem(Api.World))
                {
                    current.Itemstack = null;
                }
            }
        }

        public int GetStallForSlot(int slotId)
        {
            // Subtract 1 from slotId for the chisel decoration block first
            return (slotId - 1) / StallSlotSize;
        }

        public int GetItemSlotForStall(int slotId)
        {
            // Subtract 1 from slotId for the chisel decoration block first
            return (slotId - 1) % StallSlotSize;
        }

        public ViconCurrencySlot GetCurrencyForStallSlot(int stallSlot)
        {
            return StallSlots[stallSlot].currency;
        }

        public ItemSlot[] GetSlotsForStallSlot(int stallSlot)
        {
            return StallSlots[stallSlot].GetSlots();
        }

        public E FindFirstNonEmptyStockSlot(int stallSlot)
        {
            return StallSlots[stallSlot].FindFirstNonEmptyStockSlot();
        }

        public int GetStallSlotCount() => NumStalls;
        public int GetStacksPerStall() => NumStacksPerStall;
        public override ItemSlot this[int inventorySlotId]
        {
            get
            {
                if (inventorySlotId == 0)
                {
                    return ChiselDecoSlot;
                }
                else
                {
                    int stallSlot = GetStallForSlot(inventorySlotId);
                    int itemSlot = GetItemSlotForStall(inventorySlotId);
                    if (itemSlot == NumStacksPerStall)
                    {
                        return StallSlots[stallSlot].currency;
                    }
                    else
                    {
                        return StallSlots[stallSlot].GetSlot(itemSlot);
                    }
                }
            }
            set
            {
                if (inventorySlotId == 0)
                {
                    ChiselDecoSlot = (ViconDecoBlockSlot)value;
                }
                else
                {
                    int stallSlot = GetStallForSlot(inventorySlotId);
                    int itemSlot = GetItemSlotForStall(inventorySlotId);

                    if (inventorySlotId < 0 || inventorySlotId >= Count)
                    {
                        throw new ArgumentOutOfRangeException("slotId");
                    }
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }
                    if (itemSlot == NumStacksPerStall)
                    {
                        StallSlots[stallSlot].currency = (ViconCurrencySlot)value;
                    }
                    else
                    {
                        StallSlots[stallSlot].SetSlot(itemSlot, (E)value);
                    }

                }

            }
        }
    }
}