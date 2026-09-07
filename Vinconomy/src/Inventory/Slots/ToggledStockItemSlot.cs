using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.Slots
{
    public class ToggledStockItemSlot : ToggledSlot, IStockUpdater
    {
        public int StallIndex { get; private set; } = 0;
        public int SlotIndex { get; private set; } = 0;

        

        public ToggledStockItemSlot(InventoryBase inventory, int stallSlot, int itemSlot) : base(inventory)
        {
            this.StallIndex = stallSlot;
            this.SlotIndex = itemSlot;
        }

        public int GetStallIndex()
        {
            return StallIndex;
        }

        public int GetSlotIndex()
        {
            return SlotIndex;
        }

        public override bool CanHold(ItemSlot sourceSlot)
        {
            if (!Enabled)
            {
                return false;
            }

            if (inventory is VinconBaseInventory vinconInventory)
            {
                ItemSlot productSlot = vinconInventory.GetStall(StallIndex).Product;

                if (ShouldUpdateProductSlot(productSlot, sourceSlot))
                {
                    UpdateProductSlot(productSlot, sourceSlot);
                }

                if (ItemMatchesProduct(productSlot.Itemstack, sourceSlot))
                {
                    return base.CanHold(sourceSlot);
                }
                else
                {
                    return false;
                }

            }

            return base.CanHold(sourceSlot);
        }


        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            //Console.WriteLine("Can Take From " + stallSlot + " called...");
            if (CanHold(sourceSlot))
            {
                return base.CanTakeFrom(sourceSlot, priority);
            }
            return false;
        }

        public void SetFilter(Func<ItemSlot, bool> filter)
        {
            Filter = filter;
        }

        public int GetStall()
        {
            return StallIndex;
        }

        public int GetProductSlot()
        {
            return SlotIndex;
        }

        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (Itemstack == null && sourceSlot.Itemstack == null && op.MouseButton == EnumMouseButton.Left)
            {
                this.Enabled = !this.Enabled;
                //this.HexBackgroundColor =  this.Enabled ? null : "#FF0000";
            } else
                base.ActivateSlot(sourceSlot, ref op);
        }

        public bool ShouldUpdateProductSlot(ItemSlot productSlot, ItemSlot sourceSlot)
        {
            // We always need to update the product with a new bundle
            return true;
        }

        public bool ItemMatchesProduct(ItemStack product, ItemSlot sourceSlot)
        {
            // Blocks will never match the bundle, so we always return true to allow the block to be placed in the slot
            return true;
        }


        public void UpdateProductSlot(ItemSlot productSlot, ItemSlot sourceSlot)
        {
            VinconBaseInventory inv = inventory as VinconBaseInventory;
            productSlot.Itemstack = inv.GetStall<SculptureStallSlot>(StallIndex).GenStubbedBundle(); // Stubbed because we dont actually care about the block contents. No point in serializing all that crap.
        }
    }
}
