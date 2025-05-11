using System;
using Viconomy.BlockEntities;
using Viconomy.Inventory.StallSlots;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Viconomy.Config;

namespace Viconomy.Inventory.Impl
{
    public abstract class ViconBaseInventory : InventoryBase
    {
        protected VinconomyCoreSystem modSystem;
        protected BEVinconBase Stall;

        // The amount of different item groups there are for this inventory
        protected int NumStalls = 4;
        // the amount of items per item group
        protected int NumStacksPerStall = 9;
        public int StallSlotSize = 10;
        public ViconDecoBlockSlot ChiselDecoSlot;

        public StallSlotBase[] StallSlots;

        protected ViconBaseInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(inventoryID, api)
        {
            Stall = stall;
            NumStalls = numStalls;
            NumStacksPerStall = numStacksPerStall;
            StallSlotSize = NumStacksPerStall + 1;

            InitializeStalls();
            ChiselDecoSlot = new ViconDecoBlockSlot(this, 0);
        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            modSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
        }

        protected abstract void InitializeStalls();

        public override int Count { get { return StallSlotSize * NumStalls + 1; } }

        /*
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
        }*/

        public override void ResolveBlocksOrItems()
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
                StallSlotBase stall = StallSlots[i];
                stall.currency.Itemstack?.ResolveBlockOrItem(Api.World);
                for (int j = 0; j < NumStacksPerStall; j++)
                {
                    stall.GetSlot(j).Itemstack?.ResolveBlockOrItem(Api.World);
                }
            }
            ChiselDecoSlot.Itemstack?.ResolveBlockOrItem(Api.World);
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
                    ChiselDecoSlot = (ViconDecoBlockSlot) value;
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
                        StallSlots[stallSlot].currency = (ViconCurrencySlot) value;
                    }
                    else
                    {
                        StallSlots[stallSlot].SetSlot(itemSlot, value);
                    }

                }

            }
        }
    }
}