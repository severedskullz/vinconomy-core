using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public delegate void OnStockUpdatedDelegate(IStallComponent shop, int stallSlot, ItemStack product, int stockCount, ItemStack currency);

    public interface IStallStockUpdater
    {
        public void OnStockModified(ItemSlot slot);
        void UpdateStockForSlot(IStallComponent shop, int stallSlot, ItemStack product, int stockCount, ItemStack currency);

        public event OnStockUpdatedDelegate OnStockUpdated;
    }
}
