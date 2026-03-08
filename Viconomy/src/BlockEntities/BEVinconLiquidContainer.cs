using System;
using Vinconomy.BlockTypes;
using Vinconomy.Inventory.Impl;
using Vinconomy.Inventory.StallSlots;
using Vinconomy.Trading;
using Vinconomy.Trading.TradeHandlers;
using Vinconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Vinconomy.BlockEntities
{
    public class BEVinconLiquidContainer : BEVinconContainer
    {
        public override int StallSlotCount => 1;
        public override int ProductStacksPerSlot => 1;
        public override int BulkPurchaseAmount => 10;

        public override void ConfigureInventory()
        {
            inventory = new VinconLiquidInventory(this, null, Api, StallSlotCount, ProductStacksPerSlot);
        }

        public virtual MeshData GenMesh(int stallSlot)
        {
            if (Block == null) return null;
            LiquidStallSlot stall = (LiquidStallSlot)inventory.StallSlots[stallSlot];

            ItemStack stacks = stall.GetSlot(0).Itemstack;

            BlockVLiquidContainer ownBlock = Block as BlockVLiquidContainer;
            if (ownBlock == null) return null;

            MeshData mesh = ownBlock.GenMesh(null, stacks, false, Pos);

            
            if (mesh?.CustomInts != null)
            {
                int[] CustomInts = mesh.CustomInts.Values;
                int count = mesh.CustomInts.Count;
                for (int i = 0; i < CustomInts.Length; i++)
                {
                    if (i >= count) break;
                    CustomInts[i] |= VertexFlags.LiquidWeakWaveBitMask  // Enable weak water wavy
                                    | VertexFlags.LiquidWeakFoamBitMask;  // Enabled weak foam
                }
            }

            return mesh;
        }

        protected override void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(-0.5f, 0f, -0.5f);
            MeshData data = GenMesh(0);
            if (data != null)
            {
                mesher.AddMeshData(data, matrix.Values);
            }
        }

        private bool CanMergeContents(ItemSlot handSlot, int stallSlot)
        {
            ItemStack stallStack = inventory.StallSlots[stallSlot].GetSlot(0).Itemstack;

            if (handSlot?.Itemstack == null) return false;
            if (stallStack == null) return true;

            if (handSlot.Itemstack.Block is ILiquidSource liquid)
            {
                return TradingUtil.isMatchingItem(liquid.GetContent(handSlot.Itemstack), stallStack, Api.World);
            }

            return false;
        }

        private float TransferContentsFromStall(IPlayer byPlayer, ItemSlot source, int stallSlot, int amount)
        {
            Block block = source?.Itemstack?.Block;
            if (block == null) {
                return 0;
            }

            if (block is BlockLiquidContainerBase container)
            {
                
                float curLiters = container.GetCurrentLitres(source.Itemstack);
                float capacity = container.CapacityLitres;

                LiquidStallSlot slot = inventory.GetStall<LiquidStallSlot>(stallSlot);
                WaterTightContainableProps props = container.GetContentProps(slot.FindFirstNonEmptyStockSlot()?.Itemstack);
                float toTransfer = Math.Min( Math.Min(amount, (capacity - curLiters)), slot.GetLiters());
                ItemStack removedContents = slot.RemoveLiters(toTransfer);

                int movedStacks = LiquidTradeHandler.TransferToLiquidContainer(byPlayer, source, removedContents);
                if (movedStacks > 0)
                {
                    source.MarkDirty();
                    Api.World.PlaySoundAt((props?.FillSound != null) ? props.FillSound : new AssetLocation("sounds/effect/water-fill.ogg"), byPlayer.Entity, byPlayer, true, 16f, 1f);
                }
                return toTransfer;
            }
            return 0;
        }

        
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            int slotIndex = GetStallSlotForSelectionIndex(blockSel.SelectionBoxIndex);

            bool sneakMod = byPlayer.Entity.Controls.Sneak;
            bool sprintMod = byPlayer.Entity.Controls.Sprint;


            ItemSlot hotbarslot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (byPlayer.PlayerUID == Owner && !VinconomyCoreSystem.ShouldForceCustomerScreen)
            {
                // If we are holding an empty food container, and sneaking => take from stall
                if (VinUtils.IsEmptyLiquidContainer(hotbarslot.Itemstack) && sneakMod)
                {
                    TransferContentsFromStall(byPlayer, hotbarslot, slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                }

                // If we are holding a filled container, and it matches
                else if (CanMergeContents(hotbarslot, slotIndex)) {
                    // if we are holding Sneak, take from stall
                    if (sneakMod)
                    {
                        TransferContentsFromStall(byPlayer, hotbarslot, slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                    } else
                    {
                        TryPut(byPlayer, hotbarslot, slotIndex, sprintMod);
                    }
                } else if (sneakMod && TradingUtil.isMatchingItem(inventory.GetCurrencyForStallSlot(slotIndex).Itemstack, hotbarslot.Itemstack, Api.World)) {
                    RequestPurchaseItem(slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                } else {
                    OpenShopForPlayer(byPlayer, slotIndex);
                }
            }
            else
            {
                if (sneakMod)
                {
                    //Purchase the items.
                    RequestPurchaseItem(slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                }
                else
                {
                    // Open the shop inventory for that block selection
                    OpenShopForPlayer(byPlayer, slotIndex);
                }
            }
            return true;
        }

        protected override bool TryAddItemToStall(IPlayer byPlayer, ItemSlot activeSlot, int stallSlot, bool bulk)
        {
            bool result = false;
            if (activeSlot.Itemstack?.Block is BlockLiquidContainerBase container)
            {
                WaterTightContainableProps props = container.GetContentProps(activeSlot.Itemstack);

                if (activeSlot.StackSize == 1)
                {
                    result = ((VinconLiquidInventory)inventory).AddLiquidToStall(stallSlot, activeSlot.Itemstack, bulk ? BulkPurchaseAmount : 1) > 0;
                } else
                {
                    ItemStack taken = activeSlot.TakeOut(1);
                    ((VinconLiquidInventory)inventory).AddLiquidToStall(stallSlot, taken, bulk ? BulkPurchaseAmount : 1);
                    byPlayer.InventoryManager.TryGiveItemstack(taken);
                }
                Api.World.PlaySoundAt((props?.PourSound != null) ? props.PourSound : new AssetLocation("sounds/effect/water-pour.ogg"), byPlayer.Entity, byPlayer, true, 16f, 1f);
            }

            if (result) 
                activeSlot.MarkDirty();

            return result;
        }

        public override AggregatedSlots GetRequiredTools(IPlayer player, int stallSlot)
        {
            ItemStack desiredStack = inventory.GetStall<LiquidStallSlot>(stallSlot).FindFirstNonEmptyStockSlot()?.Itemstack;
            LiquidCapacityAggregatedSlots aggregatedSlots = new LiquidCapacityAggregatedSlots(Api);

            ItemSlot handItem = player.InventoryManager.ActiveHotbarSlot;
            if (LiquidTradeHandler.CanHoldLiquid(handItem.Itemstack, desiredStack))
            {
                aggregatedSlots.Add(handItem);
            }

            IInventory hotbarInv = player.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (LiquidTradeHandler.CanHoldLiquid(itemSlot.Itemstack, desiredStack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (LiquidTradeHandler.CanHoldLiquid(itemSlot.Itemstack, desiredStack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }
            return aggregatedSlots;
        }
        

        public override GenericTradeResult PurchaseItem(GenericTradeRequest request)
        {
            return LiquidTradeHandler.TryPurchaseItem(request);
        }
    }
}
