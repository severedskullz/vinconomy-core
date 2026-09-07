using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    /// <summary>
    /// Denotes that a given stall has its product generated in some manner, whether a combination of other products, designated slots, or other mechanism
    /// that does not directly rely on the Stock slots for a given stall.
    /// </summary>
    public interface IGeneratedProductStall
    {
        /// <summary>
        /// Generates a bare minimal ItemStack for the given stall. Usually only for display purposes and should not include the full TreeAttributes of the product.
        /// </summary>
        /// <returns></returns>
        public ItemStack GenStubbedProduct();

        /// <summary>
        /// Generates the full product for the given stall. This should include all TreeAttributes and other data that is required for the product to function properly.
        /// Should be called by the trading logic to generate the ItemStack that is given to the player when they make a purchase
        /// </summary>
        /// <returns></returns>
        public ItemStack GenProduct();

        /// <summary>
        /// Generates the product for the given stall and sets it to the Product slot. This method may result in the Product's ItemStack to be set to null if the stall does not have enough stock to generate a product.
        /// </summary>
        public void RegenProduct();
        
    }
}