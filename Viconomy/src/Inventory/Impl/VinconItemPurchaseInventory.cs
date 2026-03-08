using System;
using System.Collections.Generic;
using Vinconomy.BlockEntities;
using Vinconomy.Inventory.Slots;
using Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vinconomy.Inventory.Impl
{
    public class VinconItemPurchaseInventory : VinconBaseInventory, IStallSlotUpdater
    {
        public int PurchasedItemStacksPerStall { get; protected set; }
        public override int StallSlotSize => ProductStacksPerStall + PurchasedItemStacksPerStall + 2;

        public VinconItemPurchaseInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int productStacks, int purchasedStacks) : base(stall, inventoryID, api, numStalls, productStacks)
        {
            ProductStacksPerStall = productStacks;
            PurchasedItemStacksPerStall = purchasedStacks;
            InitializeStalls();
        }

        protected override void InitializeStalls()
        {
            
            StallSlots = new PurchaseStallSlot[NumStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new PurchaseStallSlot(this, i, ProductStacksPerStall, PurchasedItemStacksPerStall);
            }
        }

        protected override ItemSlot NewSlot(int slotId)
        {
            throw new NotImplementedException();
        }

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
                    PurchaseStallSlot stall = GetStall< PurchaseStallSlot>(stallSlot);

                    if (itemSlot == GetStallTotalStallSlots() - 2)
                    {
                        return stall.DesiredProduct;
                    }
                    else if (itemSlot == GetStallTotalStallSlots() - 1)
                    {
                        return stall.Currency;
                    }
                    else
                    {
                        return stall.GetSlot(itemSlot);
                    }
                }
            }
            set
            {
                if (inventorySlotId == 0)
                {
                    ChiselDecoSlot = (VinconDecoBlockSlot)value;
                }
                else
                {
                    if (inventorySlotId < 0 || inventorySlotId >= Count)
                    {
                        throw new ArgumentOutOfRangeException("slotId");
                    }
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }

                    int stallSlot = GetStallForSlot(inventorySlotId);
                    int itemSlot = GetItemSlotForStall(inventorySlotId);

                    if (itemSlot == StallSlotSize - 1)
                    {
                        StallSlots[stallSlot].Currency = (VinconCurrencySlot)value;
                    }
                    else
                    {
                        StallSlots[stallSlot].SetSlot(itemSlot, value);
                    }

                }

            }
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
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                StallSlots[i].FromTreeAttributes(stallTree);
            }
            ChiselDecoSlot.Itemstack = tree.GetItemstack("decoBlock");

            ResolveBlocksOrItems();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            //base.SlotsToTreeAttributes(this.slots, tree);
            for (int i = 0; i < NumStalls; i++)
            {
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                StallSlots[i].ToTreeAttributes(stallTree);
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

        public override VinconCurrencySlot GetCurrencyForStallSlot(int stallSlot)
        {
            return GetStall<PurchaseStallSlot>(stallSlot).DesiredProduct;
        }

    }
}