using Commercially.Common.Inventory.Slots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public interface IContainedStallSlot : IStallSlot
    {
        public AggregatedStacks ExtractProduct(int totalProductNeeded, int numPurchases, CapacityAggregatedSlots containerSourceSlots, bool isAdminShop);
        public CapacityAggregatedSlots GetRequiredContainers(IPlayer player);

        public bool AddContents(ItemSlot sourceSlot, int amount);

        public bool RemoveContents(ItemSlot sourceSlot, int amount);
    }
}
