
using Commercially.Common.ModSystems;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Trading;
using Commercially.Vinconomy.Util;
using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders
{
    public class GachaStallInventoryProvider : BaseInventoryProvider, IStallInventoryProvider
    {
        private GachaShopInventory _Inventory;
        public override InventoryBase Inventory => _Inventory;

        public int StallCount => _Inventory.StallSlots?.Length ?? 0;

        public GachaStallInventoryProvider(BlockEntity blockentity) : base(blockentity)
        {
            _Inventory = new GachaShopInventory(blockentity, blockentity.Api);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _Inventory.InitializeFromProperties(properties, "GachaShopInventory", this.Pos.ToString(), api);
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
            return null;
        }

        public ItemSlot GetDecorationSlot()
        {
            return null;
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
            //TODO: Fix ordering for this. Need to be checking the owner UUID instead of land claim access and do our "kick if not owner and they modified the inventory" logic

            if (packetid == VinConstants.SET_WEIGHT)
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
                    int weight = binaryReader.ReadInt32();

                   _Inventory.GetStall<GachaStallSlot>(stallSlot).Weight = Math.Max(1, weight);
                    Blockentity.MarkDirty();
                }
            }

            if (packetid == VinConstants.SET_TOTAL_RANDOMIZER)
            {
                if (!CanAccess(player))
                {
                    CommerciallyModSystem.PrintClientMessage(player, TradingConstants.DOESNT_OWN);
                    return;
                }

                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    BinaryReader binaryReader = new BinaryReader(memoryStream);
                    bool value = binaryReader.ReadBoolean();

                    _Inventory.IsCountBasedRandomizer = value;
                    Blockentity.MarkDirty();
                }
            }

            if (packetid == VinConstants.SET_CONTENTS_QUANTITY)
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
                    int contentsSlot = binaryReader.ReadInt32();
                    int amount = binaryReader.ReadInt32();

                    ItemSlot slot = _Inventory.GetStall<GachaStallSlot>(stallSlot).GachaContents[contentsSlot];
                    ItemStack stack = slot.Itemstack;

                    if (stack != null)
                    {
                        stack.StackSize = amount;
                        slot.MarkDirty();
                    }
                }
            }


        }

    }
}
