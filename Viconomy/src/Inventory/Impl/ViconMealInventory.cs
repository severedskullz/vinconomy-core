using System;
using Viconomy.BlockEntities;
using Viconomy.Filters;
using Viconomy.Inventory.Slots;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.Impl
{
    public class ViconMealInventory : ViconBaseInventory
    {
        public ViconItemSlot TransferSlot;
        public override int Count => (StallSlotSize * NumStalls) + 2;
        public ViconMealInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(stall, inventoryID, api, numStalls, numStacksPerStall)
        {
            TransferSlot = new ViconItemSlot(this, -1, 1);
            TransferSlot.SetFilter(ViconomyFilters.IsFoodContainer);
            TransferSlot.MaxSlotStackSize = 1;
            InitializeStalls();

        }

        public override ItemSlot this[int inventorySlotId]
        {
            get
            {
                if (inventorySlotId == 0)
                {
                    return ChiselDecoSlot;
                }
                else if (inventorySlotId == 1)
                {
                    return TransferSlot;
                }
                else
                {
                    if (inventorySlotId - 2 < 0 || inventorySlotId >= Count)
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
                    ChiselDecoSlot = (ViconDecoBlockSlot)value;
                }
                else if (inventorySlotId == 1)
                {
                    TransferSlot = (ViconItemSlot)value;
                }
                else
                {
                    if (inventorySlotId-2 < 0 || inventorySlotId >= Count)
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

        public override int GetStallForSlot(int slotId)
        {
            // Subtract 2 from slotId for the chisel decoration block and output first
            return (slotId - 2) / GetStallTotalStallSlots();
        }

        public override int GetItemSlotForStall(int slotId)
        {
            // Subtract 2 from slotId for the chisel decoration block and output first
            return (slotId - 2) % GetStallTotalStallSlots();
        }


        protected override void InitializeStalls()
        {
            StallSlots = new MealStallSlot[NumStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new MealStallSlot(this, i, ProductStacksPerStall);
            }

        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                MealStallSlot stall = (MealStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.FromTreeAttributes(stallTree);
            }
            ChiselDecoSlot.Itemstack = tree.GetItemstack("decoBlock");
            TransferSlot.Itemstack = tree.GetItemstack("transferSlot");

            ResolveBlocksOrItems();
        }


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
            TransferSlot.Itemstack?.ResolveBlockOrItem(Api.World);

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            //base.SlotsToTreeAttributes(this.slots, tree);
            for (int i = 0; i < NumStalls; i++)
            {
                MealStallSlot stall = (MealStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.ToTreeAttributes(stallTree);
            }
            if (ChiselDecoSlot.Itemstack != null)
            {
                tree.SetItemstack("decoBlock", ChiselDecoSlot.Itemstack.Clone());
            }
            if (TransferSlot.Itemstack != null)
            {
                tree.SetItemstack("transferSlot", TransferSlot.Itemstack.Clone());
            }
        }

        public bool AddMealToStall(int stall, ItemSlot sourceSlot, int amount)
        {
            if (sourceSlot.StackSize != 1)
            {
                //This shouldnt ever happen, but just in case, cuz we dont have code to handle this.
                throw new ArgumentException("Shouldn't have a source meal container with more than 1 stack size!");
            }

            MealStallSlot slot = GetStall<MealStallSlot>(stall);
            return slot.AddMeal(sourceSlot, amount);
        }

        public bool RemoveMealFromStall(int stall, ItemSlot outputSlot, int amount)
        {
            MealStallSlot slot = GetStall<MealStallSlot>(stall);
            return slot.RemoveMeal(outputSlot, amount);
        }

        public ItemStack[] GetMealContents(int stallSlot)
        {
            StallSlotBase stall = GetStall<MealStallSlot >(stallSlot);
            ItemStack[] stacks = new ItemStack[4];

            for (int i = 0; i < stacks.Length; i++)
            {
                stacks[i] = stall[i].Itemstack?.Clone();
            }

            return stacks;
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            return 0;
        }

        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            if (TransferSlot.Itemstack != null)
                Api.World.SpawnItemEntity(TransferSlot.Itemstack, pos);
        }

    }
}
