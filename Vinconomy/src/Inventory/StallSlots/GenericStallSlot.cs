using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Trading;
using System;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Inventory.StallSlots
{
    public class GenericStallSlot : BaseStallSlot
    {
        public override int StockSlotCount => Stock?.Length ?? 0;

        public override bool IsInitialized => Stock != null;

        public ItemSlot[] Stock;

        public override ItemSlot this[int slotId] {
            get 
            {
                if (slotId == 0) return Currency;
                else if (slotId == 1) return Product;
                else
                {
                    return Stock[slotId-2];
                }
            }
            set {
                if (slotId == 0) Currency = (CurrencySlot) value;
                else if (slotId == 1) Product = (ProductSlot) value;
                else
                {
                    Stock[slotId - 2] = value;
                }
            } 
        }

        public GenericStallSlot(VinconBaseInventory inventory, int stallSlot) : base(inventory, stallSlot) { 
        }


        public override ItemSlot[] GetStockSlots()
        {
            return Stock;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("numSlots", Stock.Length);
            for (int j = 0; j < StockSlotCount; j++)
            {
                if (Stock[j].Itemstack != null)
                {
                    tree.SetItemstack("slot" + j, Stock[j].Itemstack);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            base.FromTreeAttributes(tree);

            int numSlots = tree.GetInt("numSlots");

            if (!IsInitialized)
            {
                Stock = new ItemSlot[numSlots];
                for (int i = 0; i < numSlots; i++)
                {
                    Stock[i] = new StockItemSlot(Inventory, StallSlot, i);
                    ItemStack itemStack = tree.GetItemstack("slot" + i);
                    Stock[i].Itemstack = itemStack;
                    if (Inventory.Api?.World != null)
                    {
                        itemStack?.ResolveBlockOrItem(Inventory.Api.World);
                    }
                }
            } else
            {
                for (int i = 0; i < numSlots; i++)
                {
                    ItemStack itemStack = tree.GetItemstack("slot" + i);
                    Stock[i].Itemstack = itemStack;
                    if (Inventory.Api?.World != null)
                    {
                        itemStack?.ResolveBlockOrItem(Inventory.Api.World);
                    }
                }
            }
            
        }

        public override void Initialize(VinconBaseInventory inventory, int stallSlot, int numSlotsPerStall)
        {
            base.Initialize(inventory, stallSlot, numSlotsPerStall);

            if (!IsInitialized)
            {
                Stock = new ItemSlot[numSlotsPerStall];
                for (int i = 0; i < numSlotsPerStall; i++)
                {
                    Stock[i] = new StockItemSlot(inventory, StallSlot, i); ;
                }
            }
        }

        public override AggregatedStacks ExtractProduct(int amount, int numPurchases, bool isAdminOwned)
        {
            int totalProductToMove = amount;
            AggregatedStacks productStacks = new AggregatedStacks();
            ItemStack product = GetOfferedProduct();

            if (isAdminOwned)
            {

                int maxStackSize = product.Collectible.MaxStackSize;
                while (totalProductToMove > 0)
                {
                    ItemStack transferStack = product.Clone();
                    int stackSize = Math.Min(totalProductToMove, maxStackSize);
                    transferStack.StackSize = stackSize;
                    productStacks.Add(transferStack);
                    totalProductToMove -= stackSize;
                }
            }
            else
            {
                AggregatedSlots products = TradingUtil.GetAllValidSlotsFor(this.Inventory.Api, product, Stock, IsFuzzyMatching);
                foreach (ItemSlot slot in products)
                {
                    ItemStack takenStack = slot.TakeOut(totalProductToMove);
                    if (takenStack != null)
                    {
                        this.Inventory.modSystem.Mod.Logger.Debug($"Took out {takenStack.StackSize}x {takenStack} product from Product Stacks");
                        totalProductToMove -= takenStack.StackSize;
                        productStacks.Add(takenStack);
                        slot.MarkDirty();
                    }

                    if (totalProductToMove <= 0)
                    {
                        if (totalProductToMove < 0)
                        {
                            this.Inventory.modSystem.Mod.Logger.Error($"Somehow removed {Math.Abs(totalProductToMove)} extra items from Product");
                        }
                        break;
                    }
                }
            }

            if (totalProductToMove > 0)
            {
                this.Inventory.modSystem.Mod.Logger.Error($"Somehow missing {totalProductToMove}  items from Product");
            }

            return productStacks;
        }
    }
}
