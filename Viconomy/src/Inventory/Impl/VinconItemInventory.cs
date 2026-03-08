using System.Collections.Generic;
using Vinconomy.BlockEntities;
using Vinconomy.Inventory.StallSlots;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vinconomy.Inventory.Impl
{
    public class VinconItemInventory : VinconBaseInventory, IStallSlotUpdater
    {

        public VinconItemInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(stall, inventoryID, api, numStalls, numStacksPerStall)
        {
            InitializeStalls();
        }

        protected override void InitializeStalls()
        {
            StallSlots = new ItemStallSlot[NumStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new ItemStallSlot(this, i, ProductStacksPerStall);
            }
        }

        /*
         * TODO: Figure out a way to determine if stock was ADDED and not removed. extractedStack == null if we added items, but if we add items then take it out, we would still get a notification if that was the only criteria.
         * Need to save the state of the inventory somehow. Dont want to poll the DB before we update it to figure that out - plus can lead to sync issues.
        public override void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
        {
            modSystem.Mod.Logger.Debug("A slot was modified. extracted stack is " + (extractedStack == null));
            base.DidModifyItemSlot(slot, extractedStack);
        }
        */


        protected override ItemSlot NewSlot(int slotId)
        {
            if (slotId == 0)
            {
                return new VinconDecoBlockSlot(this, 0);
            }

            int index = slotId - 1;
            int stallSlot = index / StallSlotSize;
            int itemSlot = slotId % StallSlotSize;
            if (itemSlot == ProductStacksPerStall)
            {
                return new VinconCurrencySlot(this);
            }
            return new VinconItemSlot(this, stallSlot, itemSlot);
        }



        public void SetSlotFilter(int slot, Vintagestory.API.Common.Func<ItemSlot, bool> filter)
        {
            VinconItemSlot[] filteredSlots = (VinconItemSlot[]) StallSlots[slot].GetSlots();
            foreach (var itemSlot in filteredSlots)
            {
                itemSlot.SetFilter(filter);
            }

        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                ItemStallSlot stall = (ItemStallSlot)StallSlots[i];
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
                ItemStallSlot stall = (ItemStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.ToTreeAttributes(stallTree);
            }
            if (ChiselDecoSlot.Itemstack != null)
            {
                tree.SetItemstack("decoBlock", ChiselDecoSlot.Itemstack.Clone());
            }
        }

        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            using IEnumerator<ItemSlot> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                ItemSlot current = enumerator.Current;
                if (current.Itemstack == null || current is VinconCurrencySlot)
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

        public int GetItemsPerPurchase(int stallSlot)
        {
            return StallSlots[stallSlot].ItemsPerPurchase;
        }

        public void SetSlotBackground(int stallSlot, string background = null, string hexColor = null)
        {
            ItemSlot[] curSlots = GetSlotsForStallSlot(stallSlot);
            foreach (var slot in curSlots)
            {
                slot.BackgroundIcon = background;
                slot.HexBackgroundColor = hexColor;
            }
        }
    }
}