using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory
{
    public class ViconInventory : InventoryBase
    {

        // the amount of items per item group
        private int itemsPerBin = 3;

        // The amount of different item groups there are for this inventory
        private int binSize;

        private ItemSlot[] slots;
        public ItemSlot[] Slots { get { return this.slots; } }
        public override int Count { get { return this.slots.Length; } }
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

        public ViconInventory(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            this.slots = new ViconCurrencySlot[16];
            for (int i = 0; i < this.slots.Length; i++)
            {
                this.slots[i] = NewSlot(i);
            }
        }

        protected override ItemSlot NewSlot(int id)
        {
            if ((id + 1) % 4 == 0)
                return new ViconCurrencySlot(this, id / 4);

            else
                return new ViconItemSlot(this, id / 4, id);
        }

        // Pass in our Slots to SlotsFromTreeAttributes so it doesnt go and remake our freaking Array >.<
        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            this.slots = this.SlotsFromTreeAttributes(tree, slots, null);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.SlotsToTreeAttributes(this.slots, tree);
        }
    }
}
