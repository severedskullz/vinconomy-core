using Commercially.Vinconomy.Inventory.Slots;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.Impl
{
    /// <summary>
    /// An inventory meant to be filled/emptied from a dedicated slot - such as through a UI, 
    /// </summary>
    public abstract class FillableShopInventory : VinconBaseInventory
    {
        public FillableShopInventory(BlockEntity entity, ICoreAPI api) : base(entity, api)
        {
        }

        public override void InitializeInternalSlots()
        {
            if (!IsInternalSlotsInitialized)
            {
                InternalSlots = new ItemSlot[2];
                InternalSlots[0] = new DecoBlockSlot(this, 0);
                InternalSlots[1] = new VinconTransferSlot(this);
            }
        }

        public ItemSlot GetDecorationBlock()
        {
            return InternalSlots[0];
        }

        public ItemSlot GetTransferSlot()
        {
            return InternalSlots[1];
        }

        public abstract bool TransferToStall(int stallSlot, ItemSlot sourceSlot, int amount);
        public abstract bool TransferFromStall(int stallSlot, ItemSlot destSlot, int amount);
    }
}
