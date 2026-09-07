using Commercially.Common.Inventory.Slots;
using System;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.Slots
{
    public class SculptureAggregatedSlots : GenericAggregatedSlots
    {
        public SculptureAggregatedSlots(ICoreAPI api) : base(api)
        {

        }

        public override void Add(ItemSlot item)
        {
            if (Slots.Count == 0)
            {
                TotalCount = item.StackSize;
            } else
            {
                TotalCount = Math.Min(TotalCount, item.StackSize);
            }
            Slots.Add(item);

        }
    }
}
