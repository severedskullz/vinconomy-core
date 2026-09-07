using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Trading;
using Vintagestory.API.Common;

namespace Vinconomy.Inventory.Slots
{
    public class StockItemSlot : FilteredItemSlot, IProductUpdater
    {
        public int StallIndex { get; private set; } = 0;
        public int SlotIndex { get; private set; } = 0;
        public bool IsLocked { get; set; }

        public StockItemSlot(InventoryBase inventory, int stallSlot, int itemSlot) : base(inventory)
        {
            this.StallIndex = stallSlot;
            this.SlotIndex = itemSlot;
            this.StorageType = EnumItemStorageFlags.General
                | EnumItemStorageFlags.Metallurgy
                | EnumItemStorageFlags.Jewellery
                | EnumItemStorageFlags.Alchemy
                | EnumItemStorageFlags.Agriculture
                | EnumItemStorageFlags.Outfit
                | EnumItemStorageFlags.Backpack;
            //this.HexBackgroundColor = "#65d934";
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
            if (IsLocked) return false;

            return base.CanHold(sourceSlot);
        }


        public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
        {
            if (IsLocked) return false;

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

        public virtual bool ItemMatchesProduct(ProductSlot product)
        {
            return TradingUtil.IsMatchingItem(product.Itemstack, this.Itemstack, inventory.Api.World);
        }

        public virtual bool ShouldUpdateProductSlot(ProductSlot product)
        {
            return product?.Itemstack == null;
        }

        /// <summary>
        /// Updates the product slot for the stall with the given source slot. Returns true if the update is successful.
        /// Consequently, this also means that the item stack can be contained in the stall.
        /// </summary>
        /// <param name="sourceSlot"></param>
        /// <returns></returns>
        public virtual void UpdateProductSlot(BaseStallSlot stall)
        {
            ItemSlot productSlot = stall.Product;

            if (productSlot.CanHold(this)) {
                productSlot.Itemstack = this.Itemstack?.Clone();
                productSlot.Itemstack.StackSize = 1;
                productSlot.MarkDirty();

            }
        }
    }

}
