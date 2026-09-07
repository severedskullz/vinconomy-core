
using Commercially.Common.ModSystems;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Trading;
using Commercially.Vinconomy.Util;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.BlockEntityBehaviors.InventoryProviders
{
    public class PurchaseStallInventoryProvider : BaseInventoryProvider, IStallInventoryProvider
    {
        private PurchaseStallShopInventory _Inventory;

        public override InventoryBase Inventory => _Inventory;

        public int StallCount => _Inventory.StallSlots?.Length ?? 0;

        public PurchaseStallInventoryProvider(BlockEntity blockentity) : base(blockentity)
        {
            _Inventory = new PurchaseStallShopInventory(blockentity, blockentity.Api);
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _Inventory.InitializeFromProperties(properties, "PurchaseStallShopInventory", this.Pos.ToString(), api);
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

            if (packetid == VinConstants.SET_FUZZY_MATCHING)
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

                    PurchaseStallSlot slot = _Inventory.GetStall<PurchaseStallSlot>(stallSlot);
                    slot.IsFuzzyMatching = enabled;
                    this.Blockentity.MarkDirty();

                }
            }
            else if(packetid == VinConstants.SET_REGISTER_FALLBACK)
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
                    PurchaseStallSlot stall = _Inventory.GetStall<PurchaseStallSlot>(stallSlot);
                    stall.RegisterFallback = enabled;
                    this.Blockentity.MarkDirty();

                }
            }
            else if (packetid == VinConstants.SET_LIMITED_PURCHASES)
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
                    PurchaseStallSlot stall = _Inventory.GetStall<PurchaseStallSlot>(stallSlot);
                    stall.IsLimited = enabled;
                    this.Blockentity.MarkDirty();
                }
            }
            else if (packetid == VinConstants.SET_PURCHASES_REMAINING)
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

                    PurchaseStallSlot stall = _Inventory.GetStall<PurchaseStallSlot>(stallSlot);
                    stall.NumPurchasesRemaining = amount;
                    this.Blockentity.MarkDirty();
                }
            }

            else
            {
                base.OnReceivedClientPacket(player, packetid, data);
            }
        }
    }
}