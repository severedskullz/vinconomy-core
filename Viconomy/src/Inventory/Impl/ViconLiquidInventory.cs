using System;
using Viconomy.BlockEntities;
using Viconomy.Inventory.Slots;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Inventory.Impl
{
    public class ViconLiquidInventory : ViconBaseInventory
    {
        public ViconLiquidInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(stall, inventoryID, api, numStalls, numStacksPerStall)
        {
            //PutLocked = true;
            //TakeLocked = true;
            InitializeStalls();
        }

        protected override void InitializeStalls()
        {
            StallSlots = new LiquidStallSlot[NumStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new LiquidStallSlot(this, i, 250);
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
            int itemSlot = slotId % StallSlotSize;

            if (itemSlot == ProductStacksPerStall)
            {
                return new ViconCurrencySlot(this);
            }
            return new VinconLockedItemSlot(this, stallSlot, itemSlot);
        }



        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                LiquidStallSlot stall = (LiquidStallSlot)StallSlots[i];
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
                LiquidStallSlot stall = (LiquidStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stall.ToTreeAttributes(stallTree);
            }
            if (ChiselDecoSlot.Itemstack != null)
            {
                tree.SetItemstack("decoBlock", ChiselDecoSlot.Itemstack.Clone());
            }
        }

        public int AddLiquidToStall(int stall, ItemStack sourceStack, int amount)
        {
            if (sourceStack == null)
                return 0;

            BlockLiquidContainerBase container = sourceStack.Block as BlockLiquidContainerBase;
            if (container == null)
                return 0;

            WaterTightContainableProps props = container.GetContentProps(sourceStack);
            ItemStack contents = container.GetContent(sourceStack);
            float currentLiters = container.GetCurrentLitres(sourceStack);
            float toTransfer = Math.Min(amount, currentLiters);
            if (contents != null)
            {
                LiquidStallSlot slot = GetStall<LiquidStallSlot>(stall);

                ItemStack containerStack = slot.GetSlot(0).Itemstack;

                ItemStack bowlStack = contents;

                if (containerStack != null && bowlStack.Id != containerStack.Id)
                {
                    return 0;
                }

                float amountMoved = slot.AddLiters(contents, toTransfer);
                float litersTransfered = Math.Max(0, amountMoved);
                        
                currentLiters -= litersTransfered;
                if (currentLiters > 0)
                {
                    container.SetCurrentLitres(sourceStack, currentLiters);
                } else
                {
                    container.SetContents(sourceStack, null);
                }
                return 0;
            }
            
            return 0;
        }
    }
}
