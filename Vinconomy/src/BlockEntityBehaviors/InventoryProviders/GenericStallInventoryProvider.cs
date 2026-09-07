using Commercially.Common.Interfaces;
using Commercially.Common.ModSystems;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders
{
    public class GenericStallInventoryProvider : BaseInventoryProvider, IStallInventoryProvider, IDecocratedBlock
    {
        private DecoratedShopInventory _Inventory;
        public override InventoryBase Inventory => _Inventory;
        public int StallCount => _Inventory.StallSlots?.Length ?? 0;
        protected CommerciallyModSystem ModSystem;
        protected IOwnable Ownable;

        public GenericStallInventoryProvider(BlockEntity blockentity) : base(blockentity)
        {
            _Inventory = new DecoratedShopInventory(blockentity, blockentity.Api);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _Inventory.InitializeFromProperties(properties, "GenericShopInventory", this.Pos.ToString(), api);

        }

        public ItemStack GetCurrencyForStallSlot(int stallSlot)
        {
            return _Inventory.GetStall(stallSlot).Currency?.Itemstack?.Clone();
        }

        public ItemStack GetProductForStallSlot(int stallSlot)
        {
            return _Inventory.GetStall(stallSlot).Product?.Itemstack?.Clone();
        }

        public BaseStallSlot GetStallSlot(int stallSlot)
        {
            return _Inventory.GetStall(stallSlot);
        }

        public T GetStallSlot<T>(int stallSlot) where T : BaseStallSlot
        {
            return _Inventory.GetStall<T>(stallSlot);
        }

        public ItemStack GetDecorationBlock()
        {
            return _Inventory[0].Itemstack;
        }

        public ItemSlot GetDecorationSlot()
        {
            return _Inventory[0];
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (Ownable != null && !Ownable.CanAccess(player))
            {
                if (!((ICoreServerAPI)Api).Server.IsDedicated)
                {
                    CommerciallyModSystem.PrintClientMessage(player, "Nice Try, but that isn't yours... If this wasn't singleplayer, you would have been kicked.");
                }
                else
                {
                    ((IServerPlayer)player).Disconnect("Nice try, but that wasn't yours. (Tried to access Inventory you didn't own)");
                }
            }
            base.OnReceivedClientPacket(player, packetid, data);
        }
    }
}
