using Commercially.Common.Interfaces;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.Impl
{
    public class DecoratedShopInventory : VinconBaseInventory, IDecocratedBlock
    {
        public DecoratedShopInventory(BlockEntity entity, ICoreAPI api) : base(entity, api)
        {
        }

        public override void InitializeInternalSlots()
        {
            if (!IsInternalSlotsInitialized)
            {
                InternalSlots = new ItemSlot[1];
                InternalSlots[0] = new DecoBlockSlot(this, 0);
            }
        }

        public ItemStack GetDecorationBlock()
        {
            return GetDecorationSlot().Itemstack;
        }

        public ItemSlot GetDecorationSlot()
        {
            return InternalSlots[0];
        }
    }
}
