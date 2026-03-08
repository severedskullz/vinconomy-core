using System;
using System.Collections.Generic;
using Vinconomy.BlockEntities;
using Vinconomy.Config;
using Vinconomy.Inventory.Slots;
using Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vinconomy.Inventory.Impl
{
    public abstract class VinconBaseInventory : InventoryBase
    {
        protected VinconomyCoreSystem modSystem;
        protected BEVinconBase Stall;

        // The amount of different item groups there are for this inventory
        protected int NumStalls = 4;

        // the amount of product items per stall slot
        public int ProductStacksPerStall { get; protected set; }

        // The total numer of slots per stall, ProductStacksPerStall + Currency slot, in most cases
        public virtual int StallSlotSize => ProductStacksPerStall + 1;
        public VinconDecoBlockSlot ChiselDecoSlot;

        public StallSlotBase[] StallSlots;

        protected VinconBaseInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int productStacksPerStall) : base(inventoryID, api)
        {
            Stall = stall;
            NumStalls = numStalls;
            ProductStacksPerStall = productStacksPerStall;
            ChiselDecoSlot = new VinconDecoBlockSlot(this, 0);            
        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            modSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
        }

        protected abstract void InitializeStalls();

        public override int Count => (StallSlotSize * NumStalls) + 1;

        
        
        public override void ResolveBlocksOrItems()
        {
            // Because Tyron wants us to try to resolve block items both BEFORE and AFTER the Api has be passed off into the Inventory with LateInitialize,
            // as well as when it is loaded with FromTreeAttributes We need to make sure it actually is fucking SET before we try to resolve the blocks or items...
            // See InventoryBase:SlotsFromTreeAtributes
            //
            // I am not really doing anything different than what the base method does, but attempting to use that throws a null pointer for some reason
            // so why this is needed is beyond me. All it does is call ResloveBlockOrItem on each item stack in the inventory... I suspect Api is null since
            // LateInitialize hasnt gotten called yet, and im trying to use this method to automatically resolve the blocks in FromTreeAttributes?
            //
            // This is probably something I am doing "wrong" here given the null check and "continue" for Api, and that I should be resolving the items manually in
            // the FromTreeAttributes method but whatever - it works, so fuck it.

            if (Api?.World == null)
            {
                return;
            }

            for (int i = 0; i < NumStalls; i++)
            {
                StallSlotBase stall = StallSlots[i];
                stall.ResolveBlockOrItem(Api.World);
                
            }
            ChiselDecoSlot.Itemstack?.ResolveBlockOrItem(Api.World);
            
        }
        
        
        
        public StallSlotBase GetStall(int slot)
        {
            if (slot >= StallSlots.Length || slot < 0)
            {
                return null;
            }

            return StallSlots[slot];
        }

        public T GetStall<T>(int slot) where T : StallSlotBase
        {
            return (T)GetStall(slot);
        }

        public virtual int GetStallForSlot(int slotId)
        {
            // Subtract 1 from slotId for the chisel decoration block first
            return (slotId - 1) / GetStallTotalStallSlots();
        }

        public virtual int GetItemSlotForStall(int slotId)
        {
            // Subtract 1 from slotId for the chisel decoration block first
            return (slotId - 1) % GetStallTotalStallSlots();
        }

        public int GetStallTotalStallSlots()
        {
            return GetStall(0).TotalSlots;
        }

        public virtual VinconCurrencySlot GetCurrencyForStallSlot(int stallSlot)
        {
            return GetStall(stallSlot)?.Currency;
        }

        public ItemSlot[] GetSlotsForStallSlot(int stallSlot)
        {
            return GetStall(stallSlot)?.GetSlots();
        }

        public ItemSlot FindFirstNonEmptyStockSlot(int stallSlot)
        {
            return GetStall(stallSlot)?.FindFirstNonEmptyStockSlot();
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            VinconConfig config = modSystem.Config;
            if (config != null && config.FoodDecaysInShops && Stall != null && !Stall.IsAdminShop)
            {
                return base.GetDefaultTransitionSpeedMul(transType) * modSystem.Config.StallPerishRate;
            }
            else
            {
                return 0;
            }

        }

        public int GetStallSlotCount() => NumStalls;
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
                    if (inventorySlotId - 1 < 0 || inventorySlotId >= Count)
                    {
                        throw new ArgumentOutOfRangeException("slotId");
                    } 

                    int stallSlot = GetStallForSlot(inventorySlotId);
                    int itemSlot = GetItemSlotForStall(inventorySlotId);
                    return StallSlots[stallSlot][itemSlot];
                }
            }
            set
            {
                if (inventorySlotId == 0)
                {
                    ChiselDecoSlot = (VinconDecoBlockSlot) value;
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
                    StallSlots[stallSlot][itemSlot] = value;
                }

            }
        }
        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return null;
        }

        public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            return null;
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

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlotBase stall = StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.FromTreeAttributes(stallTree);
            }
            ChiselDecoSlot.Itemstack = tree.GetItemstack("decoBlock");

            ResolveBlocksOrItems();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlotBase stall = StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.ToTreeAttributes(stallTree);
            }
            if (ChiselDecoSlot.Itemstack != null)
            {
                tree.SetItemstack("decoBlock", ChiselDecoSlot.Itemstack.Clone());
            }
        }
    }
}