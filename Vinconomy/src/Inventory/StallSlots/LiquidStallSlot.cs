using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Trading;
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

        //TODO: Cleanup. Redundant and not reusable.
        public int AddContentsToStall(ItemStack sourceContainer, float liters)
        {
            if (sourceContainer?.StackSize > 1)
            {
                throw new ArgumentException("Liquid Source Stack must be a stack size of 1, otherwise we risk deleting multiple stacks worth of liquid! You're WELCOME!");
            }

            BlockLiquidContainerBase container = sourceContainer?.Block as BlockLiquidContainerBase;
            if (container == null)
                return 0;

            ItemStack sourceContents = container.GetContent(sourceContainer);
            if (sourceContents == null)
                return 0;

            if (Liquid.Itemstack != null && !Liquid.Itemstack.Equals(Inventory.Api.World, sourceContents, GlobalConstants.IgnoredStackAttributes))
                return 0;

            float itemsPerLiter = LiquidUtils.GetItemsPerLiter(sourceContents);
            float stallCapacity = LiterCapacity * itemsPerLiter;
            float currentCapacity = LiquidUtils.GetItemsPerLiter(Liquid.Itemstack);
            float remainingCapacity = stallCapacity - currentCapacity;
            
            float containerCurrentLiters = container.GetCurrentLitres(sourceContainer);
            float fromTransferLimit = Math.Min(liters, containerCurrentLiters);
            float toTransferLimit = Math.Min(fromTransferLimit, remainingCapacity);

            int numItemsFromLiters = LiquidUtils.GetStackSizeFromLiters(sourceContents, toTransferLimit);

            ItemStack? taken = container.TryTakeContent(sourceContainer, numItemsFromLiters);
            DummySlot slot = new DummySlot(taken);
            Liquid.IsLocked = false;
            int takenAmt = slot.TryPutInto(Inventory.Api.World, Liquid, slot.StackSize);
            Liquid.IsLocked = true;

            if (takenAmt > 0) Liquid.MarkDirty(); 
            return takenAmt;
        }

        //TODO: Cleanup. Redundant and not reusable.
        public int TransferToContainer(ItemStack destContainer, ItemStack liquid, float desiredLitersToTransfer)
        {
            if (destContainer?.StackSize > 1)
            {
                throw new ArgumentException("Liquid Source Stack must be a stack size of 1, otherwise we risk adding multiple stacks worth of liquid! You're WELCOME!");
            }

            int actualItemsToTransfer = GetTransferrableCapacity(destContainer, liquid, desiredLitersToTransfer);
            if (actualItemsToTransfer <= 0)
                return 0;

            float actualLitersToTransfer = LiquidUtils.GetLitersFromStackSize(liquid, actualItemsToTransfer);
            return (destContainer?.Block as BlockLiquidContainerBase)?.TryPutLiquid(destContainer, liquid, actualLitersToTransfer) ?? 0; ;
        }

        public int GetTransferrableCapacity(ItemStack destContainer, ItemStack contentsToTransfer, float liters)
        {
            BlockLiquidContainerBase container = destContainer?.Block as BlockLiquidContainerBase;
            if (container == null)
                return 0;
            ItemStack containerContents = container.GetContent(destContainer);
            if (containerContents != null && !containerContents.Equals(Inventory.Api.World, contentsToTransfer, GlobalConstants.IgnoredStackAttributes))
                return 0;
            float itemsPerLiter = LiquidUtils.GetItemsPerLiter(contentsToTransfer);
            float containerCurrentLiters = container.GetCurrentLitres(destContainer);

            int desiredItemsToTransfer = LiquidUtils.GetStackSizeFromLiters(contentsToTransfer, liters);
            int capacityItemsToTransfer = LiquidUtils.GetStackSizeFromLiters(contentsToTransfer, container.CapacityLitres - containerCurrentLiters);
            int actualItemsToTransfer = Math.Min(desiredItemsToTransfer, capacityItemsToTransfer);

            return actualItemsToTransfer;
        }

        public override void DropInventory(Vec3d pos, int maxStackSize, bool markDirty = false)
        {
            // DO NOTHING. Liquids go bye-bye! Needs a container since liquid-portion isnt an actual obtainable item. Pretend they spilled on the floor, I don't care.
        }

        public override AggregatedStacks ExtractProduct(int amount, int numPurchases, bool isAdminOwned)
        {
            AggregatedStacks result = new AggregatedStacks();
            if (!isAdminOwned)
            {
                ItemStack taken = Liquid.TakeOut(amount);
                if (taken != null)
                {
                    Liquid.MarkDirty();
                    result.Add(taken);
                }
            } else
            {
                ItemStack taken = Liquid.Itemstack?.Clone();
                if (taken != null)
                {
                    taken.StackSize = amount;
                    result.Add(taken);
                }
            }
            return result;
        }

        public AggregatedStacks ExtractProduct(int totalProductNeeded, int numPurchases, CapacityAggregatedSlots containerSourceSlots, bool isAdminShop)
        {
            AggregatedStacks result = new AggregatedStacks();
            int totalItemsToTransfer = totalProductNeeded;

            AggregatedStacks stacks = ExtractProduct(totalItemsToTransfer, numPurchases, isAdminShop);
            ItemStack liquidStack = null;
            if (stacks.CanRemoveStack())
            {
                //TODO: Im writing myself into a corner here by assuming I will always get 1 stack, but right now thats just how the stalls work. Too tired to figure out a more elegant solution for multi-stack / multi-container logic
                liquidStack = stacks.RemoveStack(); 
            } else 
            {
                return result;
            }
            

            foreach (ItemSlot containerSlot in containerSourceSlots)
            {
                // Save stacksize as variable. We will be taking items OUT of this stack, so it would exit the loop early.
                // Eg. Had 2 buckets, loop ran, took one out, 'i' is now 1, and stack size is 1, so loop terminates and doesnt run on second buckets.
                int numAttempts = containerSlot.StackSize;
                for (int i = 0; i < numAttempts; i++)
                {
                    ItemStack container = containerSlot.TakeOut(1);
                    containerSlot.MarkDirty();
                    float actualLitersToTransfer = LiquidUtils.GetLitersFromStackSize(liquidStack, totalItemsToTransfer);
                    totalItemsToTransfer -= TransferToContainer(container, liquidStack, actualLitersToTransfer);

                    result.Add(container);
                    if (totalItemsToTransfer <= 0)
                        break;
                }

                if (totalItemsToTransfer <= 0)
                    return result;

            }

            if (totalItemsToTransfer > 0)
            {
                //GenericTradingProcessor.AuditLogError(result, "Somehow allowed purchase of " + totalItemsToTransfer + " extra servings even though we didnt have enough containers");
                this.Inventory.Api.World.Logger.Error("Somehow allowed purchase of " + totalItemsToTransfer + " extra servings even though we didnt have enough containers");
            }
            return result;
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

        public bool RemoveContents(ItemSlot sourceSlot, int liters)
        {
            int actualItemsToTransfer = GetTransferrableCapacity(sourceSlot.Itemstack, Liquid.Itemstack, liters);
            AggregatedStacks stacks = ExtractProduct(actualItemsToTransfer, 0, false);
            if (stacks.CanRemoveStack())
            {
                ItemStack removedStack = stacks.RemoveStack();
                int moved = TransferToContainer(sourceSlot.Itemstack, removedStack, actualItemsToTransfer);
                if (moved > 0)
                {
                    ResetProduct();
                    sourceSlot.MarkDirty();
                    return true;
                }

            }
            
            return false;
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
