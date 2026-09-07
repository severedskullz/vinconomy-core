using Commercially.Common.Interfaces;
using Commercially.Common.Util;

namespace Commercially.Vinconomy.Interfaces
{
    /// <summary>
    /// Provides Component support for a "Shop" - anything that serves as a root node for "Stalls" which can sell items.
    /// Any currency collected from the sale of items at the Stalls will be deposited here in the ShopInventory
    /// </summary>
    public interface IShopComponent : IBlockEntityComponent
    {
        public IShopInventoryProvider ShopInventoryProvider { get; }
        public IOwnableRoot Ownable { get; }
    }
}