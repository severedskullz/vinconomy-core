using Commercially.Common;
using Commercially.Common.Interfaces;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Interfaces
{
    public interface ICurrencySinkProvider : IInventoryProvider
    {
        public ItemSlot[] CurrencySlots { get; }

    }
}
