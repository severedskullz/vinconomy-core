using Commercially.Vinconomy.Inventory.StallSlots;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    /// <summary>
    /// Interface for an item slot should attempt to update the product slot of a stall when the slot is modified.
    /// Typically used for stock slots where the Product should be set to the newly added item when the Product's ItemStack is null.
    /// This way, the player does not have to manually set the product each time when adding new stock to a stall.
    /// </summary>
    public interface IProductUpdater : ITrackedItemSlot
    {
        bool ItemMatchesProduct(ProductSlot product);
        bool ShouldUpdateProductSlot(ProductSlot product);
        void UpdateProductSlot(BaseStallSlot stall);
    }
}