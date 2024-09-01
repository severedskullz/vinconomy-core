using Vintagestory.API.Common;

namespace Viconomy.Inventory
{
    internal interface IStallSlotUpdater
    {
        /**
         * Converts a global ItemSlot Id to a local Stall Slot
         * For example, if each Stall has 10 elements, and "index" is 15, then we would be in the 2nd stall slot, and should return 1 (Remember: 0-indexed)
         */
        public int GetStallForSlot(int index);

        /**
         * Converts a global ItemSlot Id to a local Stall Slot's item slot
         * For example, if each Stall has 10 elements, and "index" is 15, then we would be the 6th item in the 2nd stall slot, and should return 5 (Remember: 0-indexed)
         */
        public int GetItemSlotForStall(int index);

        /**
         * Gets the number of stall slots the given inventory has
         */
        public int GetStallSlotCount();

        /**
         * Gets the currenty ItemSlot for the given Stall Slot
         */
        public ItemSlot GetCurrencyForStallSlot(int stallSlot);

        /**
        * Gets the product ItemSlots for the given Stall Slot
        */
        public ItemSlot[] GetSlotsForStallSlot(int stallSlot);

        //public void UpdateStallForSlot(int index);
    }
}