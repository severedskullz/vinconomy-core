using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Commercially.Vinconomy.Inventory.Impl
{
    public class TellerShopInventory : DecoratedShopInventory
    {
        public TellerShopInventory(BlockEntity entity, ICoreAPI api) : base(entity, api)
        {
        }

        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            // Do nothing. This has no inventory. Stock Slots come from register.
        }
    }
}
