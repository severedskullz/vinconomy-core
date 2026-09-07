
using Commercially.Common.ModSystems;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.Slots;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Trading;
using Commercially.Vinconomy.Util;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders
{
    public class SculptureStallInventoryProvider : BaseInventoryProvider, IStallInventoryProvider
    {
        private GenericShopInventory _Inventory;
        public override InventoryBase Inventory => _Inventory;

        public int StallCount => _Inventory.StallSlots?.Length ?? 0;

        public SculptureStallInventoryProvider(BlockEntity blockentity) : base(blockentity)
        {
            _Inventory = new GenericShopInventory(blockentity, blockentity.Api);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _Inventory.InitializeFromProperties(properties, "SculptureShopInventory", this.Pos.ToString(), api);
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

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == CommerciallyConstants.TOGGLE_SLOT)
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
                    bool enabled = binaryReader.ReadBoolean();

                    ToggledStockItemSlot slot = _Inventory.GetStall(stallSlot).GetStockSlot<ToggledStockItemSlot>(stallSlot);
                    slot.Enabled = enabled;

                }
            }
            else if(packetid == VinConstants.SET_SCULPTURE_XZ)
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
                    int sizeXZ = binaryReader.ReadInt32();
                    SculptureStallSlot stall = _Inventory.GetStall<SculptureStallSlot>(stallSlot);
                    stall.SculptureHorizontalSize = sizeXZ;
                    stall.UpdateEnabledSlots();


                }
            }
            else if (packetid == VinConstants.SET_SCULPTURE_Y)
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
                    int sizeY = binaryReader.ReadInt32();
                    SculptureStallSlot stall = _Inventory.GetStall<SculptureStallSlot>(stallSlot);
                    stall.SculptureVerticalSize = sizeY;
                    stall.UpdateEnabledSlots();
                }
            }
            else if (packetid == VinConstants.SET_ITEM_NAME)
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
                    string name = binaryReader.ReadString();

                    SculptureStallSlot stall = _Inventory.GetStall<SculptureStallSlot>(stallSlot);
                    stall.SculptureName = name;
                    stall.UpdateProductSlot();

                }
            }

            else
            {
                base.OnReceivedClientPacket(player, packetid, data);
            }
        }
    }
}