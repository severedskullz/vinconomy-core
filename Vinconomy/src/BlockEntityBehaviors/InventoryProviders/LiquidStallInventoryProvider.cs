
using Commercially.Common.Interfaces;
using Commercially.Common.ModSystems;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Trading;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders
{
    public class LiquidStallInventoryProvider : BaseInventoryProvider, IStallInventoryProvider, IDecocratedBlock
    {
        private LiquidShopInventory _Inventory;
        public override InventoryBase Inventory => _Inventory;

        public int StallCount => _Inventory.StallSlots?.Length ?? 0;

        public LiquidStallInventoryProvider(BlockEntity blockentity) : base(blockentity)
        {
            _Inventory = new LiquidShopInventory(blockentity, blockentity.Api);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _Inventory.InitializeFromProperties(properties, "MealShopInventory", this.Pos.ToString(), api);
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
            if (packetid == CommerciallyConstants.TRANSFER_CONTENTS)
            {
                if (!CanAccess(player))
                {
                    CommerciallyModSystem.PrintClientMessage(player, TradingConstants.DOESNT_OWN);
                    return;
                }

                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    BinaryReader binaryReader = new BinaryReader(memoryStream);
                    int stallSlot = binaryReader.ReadInt32();
                    int amount = binaryReader.ReadInt32();

                    if (amount > 0)
                    {
                        _Inventory.TransferToStall(stallSlot, _Inventory.GetTransferSlot(), amount);
                    }
                    else
                    {
                        _Inventory.TransferFromStall(stallSlot, _Inventory.GetTransferSlot(), -amount);
                    }
                }
            }
            else
            {
                base.OnReceivedClientPacket(player, packetid, data);
            }
        }
    }
}
