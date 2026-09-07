using Commercially.Common.Interfaces;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public interface ICouponSinkProvider : IInventoryProvider
    {
        public ItemSlot[] CouponSlots { get; }
    }
}
