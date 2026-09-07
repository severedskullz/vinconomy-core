using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders
{
    public class RegisterInventoryProvider : BaseInventoryProvider, IShopInventoryProvider
    {
        private RegisterInventory _Inventory;
        public override InventoryBase Inventory => _Inventory;
        public ItemSlot[] CurrencySlots => _Inventory.CurrencySlots;
        public ItemSlot[] CouponSlots => _Inventory.CouponSlots;
        public ItemSlot TradePass => _Inventory.TradePass;

        public RegisterInventoryProvider(BlockEntity blockentity) : base(blockentity)
        {
            _Inventory = new RegisterInventory(blockentity.Api);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _Inventory.InitializeFromProperties(properties, "BERegisterInventoryProvider", this.Pos.ToString(), api);
        }

    }
}
