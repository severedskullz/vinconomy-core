using Commercially.Common.Blocks.BlockEntityBehaviors;
using Commercially.Common.Interfaces;
using Commercially.Common.Inventory.Slots;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Trading;
using Commercially.Vinconomy.Trading.Processor;
using System;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.Inventory.StallSlots
{
    public class TellerStallSlot : BaseStallSlot
    {
        public TellerStallSlot(VinconBaseInventory inventory, int stallSlot) : base(inventory, stallSlot)
        {
        }

        public override ItemSlot this[int slotId] {
            get
            {
                if (slotId == 0) return Currency;
                else return Product;
               
            }
            set
            {
                if (slotId == 0) Currency = (CurrencySlot)value;
                else Product = (ProductSlot)value;

            }
        }

        public override bool IsInitialized => Currency != null && Product != null;

        public void ExtractProductFromStall(TradeResult result)
        {
            AggregatedSlots products = result.Request.ProductSourceSlots;
            int totalProductToMove = result.Request.GetFinalProductNeededPerPurchase() * result.Request.NumPurchases;
            AggregatedStacks productStacks = result.ProductStacks;

            foreach (ItemSlot slot in products)
            {
                ItemStack takenStack = slot.TakeOut(totalProductToMove);
                if (takenStack != null)
                {
                    GenericTradingProcessor.AuditLogDebug(result, $"Took out {takenStack.StackSize}x {takenStack} product from Product Stacks");
                    totalProductToMove -= takenStack.StackSize;
                    productStacks.Add(takenStack);
                    slot.MarkDirty();
                }

                if (totalProductToMove <= 0)
                {
                    if (totalProductToMove < 0)
                    {
                        GenericTradingProcessor.AuditLogError(result, $"Somehow removed {Math.Abs(totalProductToMove)} extra items from Product");
                    }
                    break;
                }

            }
        }


        public ItemSlot[] GetProductSlots()
        {
            if (this.Inventory.Api.Side == EnumAppSide.Client) return [];

            long? parentId = this.Inventory.BlockEntity.GetBehavior<BEBehaviorOwnableChild>().ParentID;
            if (parentId != null)
            {
                IOwnableReference ownable = this.Inventory.modSystem.CommerciallySystem.GetOwnable(parentId);
                return ownable.GetComponent<ICurrencySinkProvider>()?.CurrencySlots ?? [];
            }
            return [];
        }

        public void TransferProdutToPlayer(TradeResult result)
        {
            if (result.ProductStacks.TotalCount == 0) return;

            IPlayer player = result.Request.Customer;
            AssetLocation sound = null;
            while (result.ProductStacks.CanRemoveStack())
            {
                ItemStack stack = result.ProductStacks.RemoveStack();

                if (stack != null)
                {
                    this.Inventory.modSystem.Mod.Logger.Debug($"Adding {stack.StackSize}x {stack} product to Parent");
                    if (stack.Block?.Sounds?.Place.Location != null)
                    {
                        sound = stack.Block?.Sounds?.Place.Location;
                    }

                    player.InventoryManager.TryGiveItemstack(stack, true);
                    if (stack.StackSize > 0)
                    {
                        result.Request.Api.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ.Add(0.5), null);
                    }
                }
            }

            result.Request.Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), result.Request.Customer.Entity, result.Request.Customer, true, 16f, 1f);
        }

        public AggregatedSlots GetProducts()
        {
            ICoreAPI api = Inventory.Api;
            GenericAggregatedSlots slots = new GenericAggregatedSlots(api);
            ItemSlot[] products = GetProductSlots();

            if (products.Length > 0)
            {
                foreach (var slot in products)
                {
                    if (slot.Itemstack != null && TradingUtil.IsMatchingItem(Product.Itemstack, slot.Itemstack, api.World))
                    {
                        slots.Add(slot);
                    }
                }
            }
            return slots;
        }

        public override ItemSlot[] GetStockSlots()
        {
            return null;
        }

        public override AggregatedStacks ExtractProduct(int amount, int numPurchases, bool isAdminOwned)
        {
            throw new NotImplementedException();
        }
    }
}
