using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public interface IShopInventoryProvider : ICurrencySinkProvider, ICouponSinkProvider
    {
        public ItemSlot TradePass { get; } 
    }
}