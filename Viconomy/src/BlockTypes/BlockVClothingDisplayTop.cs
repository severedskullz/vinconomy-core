using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.BlockTypes
{
    public class BlockVClothingDisplayTop : Block
    {


        public BlockVClothingDisplayTop()
        {
            this.PriorityInteract = true;
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //Console.WriteLine(api.Side + ": On interaction start was called!");
            BEViconArmorStand be = world.BlockAccessor.GetBlockEntity(blockSel.Position.DownCopy(1)) as BEViconArmorStand;
            if (be != null)
            {
                BlockSelection newSel = blockSel.Clone();
                newSel.SelectionBoxIndex += 3; // Add 3 for Boots, Legs, and Gloves
                return be.OnPlayerRightClick(byPlayer, newSel);
            }
            return false;
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BEViconArmorStand be = world.BlockAccessor.GetBlockEntity(selection.Position.DownCopy(1)) as BEViconArmorStand;
            List<WorldInteraction> interactions = new List<WorldInteraction>();
            if (be != null)
            {
                StallSlot[] slots = ((ViconomyInventory)be.Inventory).StallSlots;
                //In case we have some oddity with selections, just exit gracefully.
                if (selection.SelectionBoxIndex >= slots.Length)
                {
                    return interactions.ToArray();
                }
                int selectionIndex = selection.SelectionBoxIndex + 3; // Add 3 for Boots, Legs, and Gloves
                StallSlot slot = slots[selectionIndex];

                if (be.Owner != forPlayer.PlayerUID)
                {
                    if (slot.currency.Itemstack != null && slot.FindFirstNonEmptyStockSlot() != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase-armor" + selectionIndex,
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "shift",
                            Itemstacks = new ItemStack[] { slot.currency.Itemstack }

                        });
                    }
                }
                else
                {
                    ItemSlot firstSlot = slot.FindFirstNonEmptyStockSlot();
                    if (firstSlot != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add-armor" + selectionIndex,
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "shift",
                            Itemstacks = new ItemStack[] { firstSlot.Itemstack }
                        });
                    }
                    else
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add-armor" + selectionIndex,
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "shift"
                        });
                    }
                }

                interactions.Add(new WorldInteraction
                {
                    ActionLangCode = "vinconomy:stall-open-menu",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                });

            }

            return interactions.ToArray();
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {

            BlockVClothingDisplay block = world.BlockAccessor.GetBlock(pos.DownCopy(1)) as BlockVClothingDisplay;
            if (block != null)
            {
                block.OnBlockBroken(world, pos.DownCopy(1), byPlayer, dropQuantityMultiplier);
            }
            else
            {
                base.OnBlockBroken(world, pos, byPlayer, 0);
            }

        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new ItemStack[0];
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            BlockVClothingDisplay block = world.BlockAccessor.GetBlock(pos.DownCopy(1)) as BlockVClothingDisplay;
            if (block != null)
            {
                return block.GetPlacedBlockInfo(world, pos.DownCopy(1), forPlayer);
            }
            else
            {
                return base.GetPlacedBlockInfo(world, pos, forPlayer);
            }
        }
    }

}
