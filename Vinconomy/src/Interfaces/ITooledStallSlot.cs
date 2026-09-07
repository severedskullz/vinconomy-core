using Commercially.Common.Inventory.Slots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public interface ITooledStallSlot : IStallSlot
    {
        void ExtractDurability(int numPurchases, bool isAdminShop);
        public DurabilityAggregatedSlots GetRequiredTools(IPlayer player);
    }
}
