using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    /// <summary>
    /// Interface for an item slot that belongs to a stall whose product slot is generated from another item slot or slots.
    /// Some examples would be the Sculpture Stall where the Sculpture Bundle is generated from all of the WxHxL blocks in the stall, or the Gacha Stall where each product is generated from up to 5 Cloning Slots.
    /// </summary>
    public interface IStockUpdater : ITrackedItemSlot
    {
        public bool ShouldUpdateProductSlot(ItemSlot productSlot, ItemSlot sourceSlot);
        public bool ItemMatchesProduct(ItemStack product, ItemSlot sourceSlot);

        /// <summary>
        /// Updates the stall's Product slot with the source slot
        /// </summary>
        /// <param name="productSlot"></param>
        /// <param name="sourceSlot"></param>
        public void UpdateProductSlot(ItemSlot productSlot, ItemSlot sourceSlot);
    }
}