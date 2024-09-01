using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory
{
    public class ViconomyInventory : InventoryBase, IStallSlotUpdater
    {
        BEVinconBase Stall;

        public int StallSlotSize => NumStacksPerStall + 1;
        public override int Count { get { return (StallSlotSize * NumStalls) + 1; } }
        

        // The amount of different item groups there are for this inventory
        protected int NumStalls = 4;
        // the amount of items per item group
        protected int NumStacksPerStall = 9;

        public StallSlot[] StallSlots;
        public ViconDecoBlockSlot ChiselDecoSlot;


        VinconomyCoreSystem modSystem;

        public ViconomyInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(inventoryID, api)
        {
            Stall = stall;
            NumStalls = numStalls;
            NumStacksPerStall = numStacksPerStall;

            StallSlots = new StallSlot[numStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new StallSlot(this, i, NumStacksPerStall);
            }
            ChiselDecoSlot = new ViconDecoBlockSlot(this, 0);
        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();

        }

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

        protected override ItemSlot NewSlot(int slotId)
        {
            if (slotId == 0)
            {
                return new ViconDecoBlockSlot(this, 0);
            }

            int index = slotId - 1;
            int stallSlot = index / StallSlotSize;
            int itemSlot = slotId % (StallSlotSize);
            if (itemSlot == NumStacksPerStall)
            {
                return new ViconCurrencySlot(this);
            }
            return new ViconItemSlot(this, stallSlot, itemSlot);
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
                    } else
                    {
                        return StallSlots[stallSlot].slots[itemSlot];
                    }
                }
            }
            set
            {
                if (inventorySlotId == 0)
                {
                    ChiselDecoSlot = (ViconDecoBlockSlot) value;
                }
                else
                {
                    int stallSlot = GetStallForSlot(inventorySlotId);
                    int itemSlot = GetItemSlotForStall(inventorySlotId);

                    if (inventorySlotId < 0 || inventorySlotId >= this.Count)
                    {
                        throw new ArgumentOutOfRangeException("slotId");
                    }
                    if (value == null)
                    {
                        throw new ArgumentNullException("value");
                    }
                    if (itemSlot == NumStacksPerStall)
                    {
                        this.StallSlots[stallSlot].currency = (ViconCurrencySlot)value;
                    }
                    else
                    {
                        this.StallSlots[stallSlot].slots[itemSlot] = (ViconItemSlot)value;
                    }

                }

            }
        }

        public void SetSlotFilter(int slot, Vintagestory.API.Common.Func<ItemSlot, bool> filter)
        {
            ViconItemSlot[] filteredSlots = StallSlots[slot].slots;
            foreach (var itemSlot in filteredSlots)
            {
                itemSlot.setFilter(filter);
            }

        }

        public ItemSlot FindFirstNonEmptyStockSlot(int stallSlot)
        {
            return StallSlots[stallSlot].FindFirstNonEmptyStockSlot();
        }

        public ItemSlot GetCurrencyForStallSlot(int stallSlot)
        {
            return StallSlots[stallSlot].currency;
        }

        public ItemSlot[] GetSlotsForStallSlot(int stallSlot)
        {
            return StallSlots[stallSlot].slots;
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlot stall = StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                StallSlots[i].itemsPerPurchase = stallTree.GetInt("purchaseQuantity", 1);

                stall.currency.Itemstack = stallTree.GetItemstack("currency");
                for (int j = 0; j < NumStacksPerStall; j++)
                {
                    ItemStack itemStack = stallTree.GetItemstack("slot" + j);
                    stall.slots[j].Itemstack = itemStack;
                   
                }
            }
            ChiselDecoSlot.Itemstack = tree.GetItemstack("decoBlock");

            ResolveBlockItems();
        }

        private void ResolveBlockItems()
        {
            // Because Tyron wants us to try to resolve block items both BEFORE and AFTER the Api has be passed off into the Inventory with LateInitialize,
            // We need to make sure it actually is fucking SET before we try to resolve the blocks or items... See InventoryBase:SlotsFromTreeAtributes
            // Thanks Tyron!

            if (Api?.World == null)
            {
                return;
            }

            for (int i = 0; i < NumStalls; i++)
            {
                StallSlot stall = StallSlots[i];
                stall.currency.Itemstack?.ResolveBlockOrItem(Api.World);
                for (int j = 0; j < NumStacksPerStall; j++)
                {
                    stall.slots[j].Itemstack?.ResolveBlockOrItem(Api.World);
                }
            }
            ChiselDecoSlot.Itemstack?.ResolveBlockOrItem(Api.World);
            
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            //base.SlotsToTreeAttributes(this.slots, tree);
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlot stall = StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stallTree.SetInt("purchaseQuantity", StallSlots[i].itemsPerPurchase);
                if (stall.currency.Itemstack != null)
                {
                    stallTree.SetItemstack("currency", stall.currency.Itemstack.Clone());
                }
                for (int j = 0; j < NumStacksPerStall; j++)
                {
                    if (stall.slots[j].Itemstack != null)
                    {
                        stallTree.SetItemstack("slot" + j, stall.slots[j].Itemstack.Clone());
                    }
                }
            }
            if (ChiselDecoSlot.Itemstack != null)
            {
                tree.SetItemstack("decoBlock", ChiselDecoSlot.Itemstack.Clone());
            }
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            ViconConfig config = modSystem.Config;
            if ((config != null && config.FoodDecaysInShops) && (Stall != null && !Stall.IsAdminShop))
            {
                return base.GetDefaultTransitionSpeedMul(transType) * modSystem.Config.StallPerishRate;
            }
            else
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

        public int GetItemsPerPurchase(int stallSlot)
        {
            return this.StallSlots[stallSlot].itemsPerPurchase;
        }

        public void SetSlotBackground(int stallSlot, string background = null, string hexColor = null)
        {
            ItemSlot[] curSlots = this.GetSlotsForStallSlot(stallSlot);
            foreach (var slot in curSlots)
            {
                slot.BackgroundIcon = background;
                slot.HexBackgroundColor = hexColor;
            }
        }


    }
}