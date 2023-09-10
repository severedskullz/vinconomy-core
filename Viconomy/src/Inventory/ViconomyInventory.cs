using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory
{
    public class ViconomyInventory : InventoryBase, ISlotProvider
    {
        // the amount of items per item group
        private int itemsPerBin = 9;

        // The amount of different item groups there are for this inventory
        private int binSize;

        private StallSlot[] stallSlots;

        private ItemSlot[] slots;
        public ItemSlot[] Slots { get { return this.slots; } }
        public override int Count { get { return this.stallSlots.Length * (itemsPerBin + 1); } }

        public ViconomyInventory(string inventoryID, ICoreAPI api, int binSize, int itemsPerBin) : base(inventoryID, api)
        {
            this.binSize = binSize;
            this.itemsPerBin = itemsPerBin;

            int binSlotCount = itemsPerBin + 1; // 9 slots, plus 1 for the currency = 10
            int totalSlots = binSlotCount * binSize; // 10 slots, times 4 bins = 40

            this.slots = new ItemSlot[totalSlots];
            for (int i = 0; i < totalSlots; i++)
            {
                this.slots[i] = NewSlot(i);
            }

            this.stallSlots = new StallSlot[binSize];
            for (int i = 0; i < binSize; i++)
            {
                this.stallSlots[i] = new StallSlot(this, i, itemsPerBin, slots);
            }





        }

        protected override ItemSlot NewSlot(int id)
        {
            if ((id + 1) % (itemsPerBin + 1) == 0)
                return new ViconCurrencySlot(this, id / (itemsPerBin + 1));

            else
                return new ViconItemSlot(this, id / (itemsPerBin + 1), id);
        }

        
        public override ItemSlot this[int slotId]
        {
            get
            {
                int stallSlot = slotId / (itemsPerBin + 1);
                int itemSlot = slotId % (itemsPerBin + 1);
                if (slotId < 0 || slotId >= this.Count)
                {
                    return null;
                }
                else
                {
                    if (itemSlot == itemsPerBin)
                    {
                        return this.stallSlots[stallSlot].currency;
                    }
                    return this.stallSlots[stallSlot].slots[itemSlot];
                }
            }
            set
            {
                int stallSlot = slotId / itemsPerBin + 1;
                int itemSlot = slotId % (itemsPerBin + 1);
                if (slotId < 0 || slotId >= this.Count)
                {
                    throw new ArgumentOutOfRangeException("slotId");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if (itemSlot == itemsPerBin)
                {
                    this.stallSlots[stallSlot].currency = value;
                } else
                {
                    this.stallSlots[stallSlot].slots[slotId] = value;
                }
                
            }
        }
        

        /*
        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= this.Count)
                {
                    return null;
                }
                else
                {
                    return this.slots[slotId];
                }
            }
            set
            {
                if (slotId < 0 || slotId >= this.Count)
                {
                    throw new ArgumentOutOfRangeException("slotId");
                }
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                this.slots[slotId] = value;
            }
        }
        */

        public ItemSlot FindFirstNonEmptyStockSlot(int stallSlot)
        {
            ItemSlot[] slots = GetSlotsForSelection(stallSlot);
            foreach (ItemSlot slot in slots)
            {
                if (slot.Itemstack != null)
                    return slot;
            }
            return null;
        }

        public ItemSlot GetCurrencyForSelection(int stallSlot)
        {
            //return this.slots[stallSlot * (itemsPerBin + 1)];
            return stallSlots[stallSlot].currency; 
        }

        public ItemSlot[] GetSlotsForSelection(int stallSlot)
        {
            /*
            int totalItemsPerSlot = itemsPerBin + 1; // Need to add the currency slot.
            ItemSlot[] slots = new ItemSlot[itemsPerBin];
            for (int i = 0; i < itemsPerBin; i++)
            {
                slots[i] = Slots[(stallSlot * totalItemsPerSlot) + i];
            }

            return slots;
            */
            return stallSlots[stallSlot].slots;
        }


        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            this.slots = this.SlotsFromTreeAttributes(tree, slots, null);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.SlotsToTreeAttributes(this.slots, tree);
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            return .05f;
        }

    }
}
