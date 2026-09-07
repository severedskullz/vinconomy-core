using Commercially.Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.Impl
{
    public class MealShopInventory : FillableShopInventory
    {
        public MealShopInventory(BlockEntity entity, ICoreAPI api) : base(entity, api)
        {
        }

        public override bool TransferToStall(int stallSlot, ItemSlot sourceSlot, int amount)
        {
            MealStallSlot stall = GetStall<MealStallSlot>(stallSlot);
            return stall.AddContents(sourceSlot, amount); ;
        }
        public override bool TransferFromStall(int stallSlot, ItemSlot destSlot, int amount)
        {
            MealStallSlot stall = GetStall<MealStallSlot>(stallSlot);
            return stall.RemoveContents(destSlot, amount);
        }
    }
}
