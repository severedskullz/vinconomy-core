using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Trading;
using Commercially.Vinconomy.Trading.Processor;
using Commercially.Vinconomy.Util;
using System;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.Inventory.StallSlots
{
    public class LiquidStallSlot : BaseStallSlot, IContainedStallSlot
    {
        public static AssetLocation fillSound = new AssetLocation("sounds/effect/water-fill.ogg");

        public override int StockSlotCount => 1;

        public override bool IsInitialized => Liquid != null;

        public StockItemSlot Liquid;

        public float LiterCapacity { get; private set; } = 50;

        public override ItemSlot this[int slotId] {
            get 
            {
                if (slotId == 0) return Currency;
                else if (slotId == 1) return Product;
                else
                {
                    return Liquid;
                }
            }
            set {
                if (slotId == 0) Currency = (CurrencySlot) value;
                else if (slotId == 1) Product = (ProductSlot) value;
                else
                {
                    Liquid = (StockItemSlot)value;
                }
            } 
        }

        public LiquidStallSlot(VinconBaseInventory inventory, int stallSlot) : base(inventory, stallSlot)
        {
            StockItemSlot liquid = new StockItemSlot(inventory, stallSlot, 0);
            liquid.IsLocked = true;
            Liquid = liquid;
        }

        public override ItemSlot[] GetStockSlots()
        {
            return [Liquid];
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("numSlots", 1);
            tree.SetItemstack("slot0", Liquid.Itemstack);

        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            base.FromTreeAttributes(tree);

            ItemStack itemStack = tree.GetItemstack("slot0");
            Liquid.Itemstack = itemStack;
            if (Inventory.Api?.World != null)
            {
                itemStack?.ResolveBlockOrItem(Inventory.Api.World);
            }

        }

        public ItemSlot GetProductSlot(int itemSlot)
        {
            return Liquid;
        }

        public override void Initialize(VinconBaseInventory inventory, int stallSlot, int numSlotsPerStall)
        {
            base.Initialize(inventory, stallSlot, numSlotsPerStall);

            if (!IsInitialized) // Just like the MealStallSlot, this is now redundant with the constructor
            {
                Liquid = new StockItemSlot(inventory, StallSlot, 0);
            }
        }

        public AggregatedSlots GetProducts()
        {
            ICoreAPI api = Inventory.Api;
            GenericAggregatedSlots slots = new GenericAggregatedSlots(api);
            
            if (Liquid.Itemstack != null && TradingUtil.IsMatchingItem(Product.Itemstack, Liquid.Itemstack, api.World))
            {
                slots.Add(Liquid);
            }
         

            return slots;
        }

        public override AggregatedStacks ExtractProduct(int amount, int numPurchases, bool isAdminOwned)
        {
            AggregatedStacks result = new AggregatedStacks();
            int totalProductToMove = amount;
            if (isAdminOwned)
            {
                ItemStack productStack = GetOfferedProduct();
                int maxStackSize = productStack.Collectible.MaxStackSize;
                while (totalProductToMove > 0)
                {
                    ItemStack transferStack = productStack.Clone();
                    int stackSize = Math.Min(totalProductToMove, maxStackSize);
                    transferStack.StackSize = stackSize;
                    result.Add(transferStack);
                    totalProductToMove -= stackSize;
                }
            }
            else
            {
                ItemSlot[] slots = GetStockSlots();
                foreach (ItemSlot slot in slots)
                {
                    ItemStack takenStack = slot.TakeOut(totalProductToMove);
                    if (takenStack != null)
                    {
                        this.Inventory.modSystem.Mod.Logger.Debug($"Took out {takenStack.StackSize}x {takenStack} product from Product Stacks");
                        totalProductToMove -= takenStack.StackSize;
                        result.Add(takenStack);
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

            return result;
        }

        public void ExtractProductFromStall(TradeResult result)
        {
            int totalProductToMove = result.TotalProductAmount;
            AggregatedStacks productStacks = result.ProductStacks;

            if (result.Request.IsAdminShop)
            {
                int maxStackSize = result.Request.ProductNeeded.Collectible.MaxStackSize;
                while (totalProductToMove > 0)
                {
                    ItemStack transferStack = result.Request.ProductNeeded.Clone();
                    int stackSize = Math.Min(totalProductToMove, maxStackSize);
                    transferStack.StackSize = stackSize;
                    productStacks.Add(transferStack);
                    totalProductToMove -= stackSize;
                }
            }
            else
            {
                AggregatedSlots products = result.Request.ProductSourceSlots;
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
        }

        public void TransferProdutToPlayer(TradeResult result)
        {
            if (result.ProductStacks.TotalCount == 0)
            {
                GenericTradingProcessor.AuditLogError(result, "Tried to give player products, but has nothing to give");
            }

            IPlayer player = result.Request.Customer;
            while (result.ProductStacks.CanRemoveStack())
            {
                ItemStack item = result.ProductStacks.RemoveStack();

                foreach (ItemSlot containerSlot in result.Request.ContainerSourceSlots.Slots)
                {
                    while (containerSlot.StackSize > 0)
                    {
                        ItemStack filledStack = LiquidUtils.TransferLiquidContentsToContainer(containerSlot, item, item.StackSize, out int moved);
                        item.StackSize -= moved;

                        if (filledStack != null)
                        {
                            player.InventoryManager.TryGiveItemstack(filledStack, true);
                            containerSlot.MarkDirty();
                        }

                        if (item.StackSize <= 0)
                            break;
                    }

                    if (item.StackSize <= 0)
                        break;
                }
            }

            result.Request.Api.World.PlaySoundAt(fillSound, player.Entity, player, true, 16f, 1f);
        }

        public CapacityAggregatedSlots GetRequiredContainers(IPlayer player)
        {
            ItemStack desiredStack = Product.Itemstack;
            LiquidCapacityAggregatedSlots aggregatedSlots = new LiquidCapacityAggregatedSlots(Inventory.Api);

            IWorldAccessor world = Inventory.Api.World;

            ItemSlot handItem = player.InventoryManager.ActiveHotbarSlot;
            if (LiquidUtils.CanContainerHoldLiquid(world, handItem.Itemstack, desiredStack))
            {
                aggregatedSlots.Add(handItem);
            }

            IInventory hotbarInv = player.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (LiquidUtils.CanContainerHoldLiquid(world, itemSlot.Itemstack, desiredStack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (LiquidUtils.CanContainerHoldLiquid(world, itemSlot.Itemstack, desiredStack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }
            return aggregatedSlots;
        }



        public override int AddProductToSlot(IPlayer byPlayer, ItemSlot sourceSlot, int amount)
        {
            int moved = 0;
            if (sourceSlot.Itemstack?.Block is BlockLiquidContainerBase container)
            {
                WaterTightContainableProps props = container.GetContentProps(sourceSlot.Itemstack);

                if (sourceSlot.StackSize == 1)
                {
                    moved = AddContentsToStall(sourceSlot.Itemstack, amount);
                }
                else
                {
                    ItemStack containerStack = sourceSlot.Itemstack.Clone();
                    containerStack.StackSize = 1;

                    
                    moved = AddContentsToStall(containerStack, amount);
                    // If there was any amount moved, we need to take an item out of the Container Stack
                    // And give the player back the container that was cloned and modified (which might now be empty)
                    if (moved > 0)
                    {
                        sourceSlot.TakeOut(1);
                        byPlayer.InventoryManager.TryGiveItemstack(containerStack, true);
                    }

                }

                if (moved > 0)
                {
                    Inventory.Api.World.PlaySoundAt((props?.PourSound != null) ? props.PourSound : new AssetLocation("sounds/effect/water-pour.ogg"), byPlayer.Entity, byPlayer, true, 16f, 1f);
                    sourceSlot.MarkDirty();
                }
            }

            return moved;
        }

        public int AddContentsToStall(ItemStack sourceStack, int liters)
        {
            if (sourceStack?.StackSize > 1)
            {
                throw new ArgumentException("Liquid Source Stack must be a stack size of 1, otherwise we risk deleting multiple stacks worth of liquid! You're WELCOME!");
            }

            BlockLiquidContainerBase container = sourceStack?.Block as BlockLiquidContainerBase;
            if (container == null)
                return 0;

            ItemStack contents = container.GetContent(sourceStack);
            if (contents == null)
                return 0;

            if (Liquid.Itemstack != null && !Liquid.Itemstack.Equals(Inventory.Api.World, contents, GlobalConstants.IgnoredStackAttributes))
                return 0;

            float itemsPerLiter = LiquidUtils.GetItemsPerLiter(contents);
            float stallCapacity = LiterCapacity * itemsPerLiter;
            float currentCapacity = LiquidUtils.GetItemsPerLiter(Liquid.Itemstack);
            float remainingCapacity = stallCapacity - currentCapacity;
            
            float containerCurrentLiters = container.GetCurrentLitres(sourceStack);
            float fromTransferLimit = Math.Min(liters, containerCurrentLiters);
            float toTransferLimit = Math.Min(fromTransferLimit, remainingCapacity);

            int numItemsFromLiters = LiquidUtils.GetStackSizeFromLiters(contents, toTransferLimit);

            ItemStack? taken = container.TryTakeContent(sourceStack, numItemsFromLiters);
            DummySlot slot = new DummySlot(taken);
            Liquid.IsLocked = false;
            int takenAmt = slot.TryPutInto(Inventory.Api.World, Liquid, slot.StackSize);
            Liquid.IsLocked = true;

            if (takenAmt > 0) Liquid.MarkDirty(); 
            return takenAmt;
        }

        public int RemoveContentsFromStall(ItemStack destContainer, int liters)
        {
            if (destContainer?.StackSize > 1)
            {
                throw new ArgumentException("Liquid Source Stack must be a stack size of 1, otherwise we risk adding multiple stacks worth of liquid! You're WELCOME!");
            }

            BlockLiquidContainerBase container = destContainer?.Block as BlockLiquidContainerBase;
            if (container == null)
                return 0;

            ItemStack contents = container.GetContent(destContainer);
            if (contents != null && !contents.Equals(Inventory.Api.World, Liquid.Itemstack, GlobalConstants.IgnoredStackAttributes))
                return 0;

            float itemsPerLiter = LiquidUtils.GetItemsPerLiter(contents);
            float containerCurrentLiters = container.GetCurrentLitres(destContainer);

            int desiredItemsToTransfer = LiquidUtils.GetStackSizeFromLiters(Liquid.Itemstack, liters);
            int capacityItemsToTransfer = LiquidUtils.GetStackSizeFromLiters(Liquid.Itemstack, container.CapacityLitres - containerCurrentLiters);
            int actualItemsToTransfer = Math.Min(desiredItemsToTransfer, capacityItemsToTransfer);
            float actualLitersToTransfer = LiquidUtils.GetLitersFromStackSize(Liquid.Itemstack, actualItemsToTransfer);

            Liquid.IsLocked = false;
            int moved = container.TryPutLiquid(destContainer, Liquid.Itemstack, actualLitersToTransfer);
            Liquid.TakeOut(moved);
            Liquid.IsLocked = true;
            if (moved > 0) Liquid.MarkDirty();
            return moved;
        }

        public override void DropInventory(Vec3d pos, int maxStackSize, bool markDirty = false)
        {
            // DO NOTHING. Liquids go bye-bye! Needs a container since liquid-portion isnt an actual obtainable item. Pretend they spilled on the floor, I don't care.
        }

        public AggregatedStacks ExtractProduct(int totalProductNeeded, int numPurchases, CapacityAggregatedSlots containerSourceSlots, bool isAdminShop)
        {
            throw new NotImplementedException();
        }

        public bool AddContents(ItemSlot sourceSlot, int amount)
        {
            int moved = AddContentsToStall(sourceSlot.Itemstack, amount);
            if (moved > 0)
            {
                ResetProduct();
            }
            return moved > 0;
        }

        public bool RemoveContents(ItemSlot sourceSlot, int amount)
        {
            int moved = RemoveContentsFromStall(sourceSlot.Itemstack, amount);
            if (moved > 0)
            {
                ResetProduct();
            }
            return moved > 0;
        }

        public void ResetProduct()
        {
            if (Product.Itemstack == null)
            {
                if (Liquid.Itemstack != null)
                {
                    Product.Itemstack = Liquid.Itemstack.Clone();
                    Product.Itemstack.StackSize = LiquidUtils.GetStackSizeFromLiters(Liquid.Itemstack, 1);
                    Product.MarkDirty();
                }
            } 
            else if (Liquid.Itemstack != null && !TradingUtil.IsMatchingItem(Product.Itemstack, Liquid.Itemstack, Inventory.Api.World))
            {
                // Product is different than whats in the stall - Get the amount for sale and transfer it over. Items Per Liter might be different, so convert to liters first, then back on the new item
                float origLiters = LiquidUtils.GetLitersFromStackSize(Product.Itemstack);
                Product.Itemstack = Liquid.Itemstack.Clone();
                Product.Itemstack.StackSize = LiquidUtils.GetStackSizeFromLiters(Liquid.Itemstack, origLiters);
                Product.MarkDirty();
            }
        }
    }
}
