using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory
{
    public class ViconomyInventory : InventoryBase
    {
        // the amount of items per item group
        private int itemsPerBin = 9;

        // The amount of different item groups there are for this inventory
        private int binSize;

        private StallSlot[] stallSlots;

        private ItemSlot[] slots;
        public StallSlot[] StallSlots { get { return this.stallSlots; } }

        public override int Count { get { return this.stallSlots.Length * (itemsPerBin + 1); } }

        ViconomyModSystem modSystem;

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

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            modSystem = Api.ModLoader.GetModSystem<ViconomyModSystem>();
            
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
                    //Console.WriteLine("Accessing ID " + slotId);
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
                    this.stallSlots[stallSlot].currency = (ViconCurrencySlot) value;
                } else
                {
                    this.stallSlots[stallSlot].slots[slotId] = (ViconItemSlot) value;
                }
                
            }
        }

        public void SetSlotFilter(int slot, Vintagestory.API.Common.Func<ItemSlot, bool> filter)
        {
            ViconItemSlot[] filteredSlots = stallSlots[slot].slots;
            foreach (var itemSlot in filteredSlots)
            {
                itemSlot.setFilter(filter);
            }

        }

        public ItemSlot FindFirstNonEmptyStockSlot(int stallSlot)
        {
            return stallSlots[stallSlot].FindFirstNonEmptyStockSlot();
        }

        public ItemSlot GetCurrencyForSelection(int stallSlot)
        {
            return stallSlots[stallSlot].currency; 
        }

        public ItemSlot[] GetSlotsForSelection(int stallSlot)
        {
            return stallSlots[stallSlot].slots;
        }


        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            this.slots = this.SlotsFromTreeAttributes(tree, slots, null);
            for (int i = 0; i < binSize; i++)
            {
                stallSlots[i].itemsPerPurchase = tree.GetAsInt("slotPrice-" + i, 1);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.SlotsToTreeAttributes(this.slots, tree);
            for (int i = 0; i < binSize; i++)
            {
                 tree.SetInt("slotPrice-" + i, stallSlots[i].itemsPerPurchase);
            }
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            ViconConfig config = modSystem.Config;
            if ( config != null && config.FoodDecaysInShops)
            {
                return base.GetDefaultTransitionSpeedMul(transType) * modSystem.Config.StallPerishRate;
            } else
            {
                return 0;
            }
            
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

    }
}
