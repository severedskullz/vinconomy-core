using Commercially.Vinconomy.Interfaces;
using Vintagestory.API.Common;

namespace Vinconomy.Inventory.Slots
{
    public class CurrencySlot : VinconCloningSlot, ITrackedItemSlot
    {
        int StallIndex;
        int SlotIndex;

        public CurrencySlot(InventoryBase inventory, int stallIndex, int slotIndex) : base(inventory)
        {
            this.StallIndex = stallIndex;
            this.SlotIndex = slotIndex;
        }

        public CurrencySlot(InventoryBase inventory) : base(inventory)
        {
            //this.HexBackgroundColor = "#B62521";
            this.BackgroundIcon = "commercially-payment";
        }

        public int GetStallIndex()
        {
            return StallIndex;
        }

        public int GetSlotIndex()
        {
            return SlotIndex;
        }
    }

}
