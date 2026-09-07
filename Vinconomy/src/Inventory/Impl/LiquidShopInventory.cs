using Commercially.Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.Impl
{
    public class LiquidShopInventory : FillableShopInventory
    {
        public LiquidShopInventory(BlockEntity entity, ICoreAPI api) : base(entity, api)
        {
        }

        public override bool TransferToStall(int stallSlot, ItemSlot sourceSlot, int amount)
        {
            LiquidStallSlot stall = GetStall<LiquidStallSlot>(stallSlot);
            return stall.AddContents(sourceSlot, amount); ;
        }
        public override bool TransferFromStall(int stallSlot, ItemSlot destSlot, int amount)
        {
            LiquidStallSlot stall = GetStall<LiquidStallSlot>(stallSlot);
            return stall.RemoveContents(destSlot, amount); ;
        }
    }
}
