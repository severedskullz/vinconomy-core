using System;
using Viconomy.BlockEntities;
using Viconomy.Inventory.StallSlots;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Viconomy.Config;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory.Impl
{
    public abstract class ViconBaseInventory : InventoryBase
    {
        protected VinconomyCoreSystem modSystem;
        protected BEVinconBase Stall;

        // The amount of different item groups there are for this inventory
        protected int NumStalls = 4;

        // the amount of product items per stall slot
        public int ProductStacksPerStall { get; protected set; }

        // The total numer of slots per stall, ProductStacksPerStall + Currency slot, in most cases
        public virtual int StallSlotSize => ProductStacksPerStall + 1;
        public ViconDecoBlockSlot ChiselDecoSlot;

        public StallSlotBase[] StallSlots;

        protected ViconBaseInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int productStacksPerStall) : base(inventoryID, api)
        {
            Stall = stall;
            NumStalls = numStalls;
            ProductStacksPerStall = productStacksPerStall;
            ChiselDecoSlot = new ViconDecoBlockSlot(this, 0);            
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
            return StallSlots[slot];
        }

        public int GetStallForSlot(int slotId)
        {
            // Subtract 1 from slotId for the chisel decoration block first
            return (slotId - 1) / GetStallTotalStallSlots();
        }

        public int GetItemSlotForStall(int slotId)
        {
            // Subtract 1 from slotId for the chisel decoration block first
            return (slotId - 1) % GetStallTotalStallSlots();
        }

        public int GetStallTotalStallSlots()
        {
            return StallSlots[0].TotalSlots;
        }

        public virtual ViconCurrencySlot GetCurrencyForStallSlot(int stallSlot)
        {
            return StallSlots[stallSlot].Currency;
        }

        public ItemSlot[] GetSlotsForStallSlot(int stallSlot)
        {
            return StallSlots[stallSlot].GetSlots();
        }

        public ItemSlot FindFirstNonEmptyStockSlot(int stallSlot)
        {
            return StallSlots[stallSlot].FindFirstNonEmptyStockSlot();
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            ViconConfig config = modSystem.Config;
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
                    int stallSlot = GetStallForSlot(inventorySlotId);
                    int itemSlot = GetItemSlotForStall(inventorySlotId);
                    return StallSlots[stallSlot][itemSlot];
                    /*
                    if (itemSlot == GetStallTotalStallSlots() - 1)
                    {
                        return StallSlots[stallSlot].Currency;
                    }
                    else
                    {
                        return StallSlots[stallSlot].GetSlot(itemSlot);
                    }
                    */
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
                    /*
                    if (itemSlot == StallSlotSize - 1)
                    {
                        StallSlots[stallSlot].Currency = (ViconCurrencySlot) value;
                    }
                    else
                    {
                        StallSlots[stallSlot].SetSlot(itemSlot, value);
                    }
                    */

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
    }
}