using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockTypes
{
    public class BlockVClothingDisplay : Block
    {

        public BlockVClothingDisplay()
        {
            this.PriorityInteract = true;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {
                BEViconBase viconBlock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconBase;
                if (viconBlock != null)
                {
                    viconBlock.SetOwner(byPlayer);
                }
            }

            return result;
        }


        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //Console.WriteLine(api.Side + ": On interaction start was called!");
            BEViconStall be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconStall;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return false;
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BEViconStall be = world.BlockAccessor.GetBlockEntity(selection.Position) as BEViconStall;
            List<WorldInteraction> interactions = new List<WorldInteraction>();
            if (be != null)
            {
                int selectionIndex = selection.SelectionBoxIndex;
                StallSlot[] slots = ((ViconomyInventory)be.Inventory).StallSlots;
                //In case we have some oddity with selections, just exit gracefully.
                if (selection.SelectionBoxIndex >= slots.Length)
                {
                    return interactions.ToArray();
                }

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
            if (byPlayer == null)
                return;

            BEViconStall vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEViconStall;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID)
            {
                ViconomyCore modSystem = world.Api.ModLoader.GetModSystem<ViconomyCore>();
                if (modSystem != null && !modSystem.BlockBroken(this.Code, world, pos, byPlayer, dropQuantityMultiplier))
                {
                    return ;
                }
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
                BlockVClothingDisplayTop block = world.BlockAccessor.GetBlock(pos.UpCopy(1)) as BlockVClothingDisplayTop;
                if (block != null)
                {
                    block.OnBlockBroken(world, pos.UpCopy(1), byPlayer, dropQuantityMultiplier);
                }
                
            }
                
            else if (api.Side == EnumAppSide.Server)
                ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("vinconomy:doesnt-own", new object[0]), EnumChatType.CommandError, null);
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            ViconomyCore modSystem = world.Api.ModLoader.GetModSystem<ViconomyCore>();
            if (modSystem != null)
            {
                modSystem.BlockPlaced(this.Code, world, blockPos, byItemStack);
            }
            base.OnBlockPlaced(world, blockPos, byItemStack);
            AssetLocation location = new AssetLocation("vinconomy:clothingstandtop-" + this.Variant["side"]);
            Block toPlaceBlock = world.GetBlock(location);
            world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockPos.UpCopy(1));
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            ViconomyCore modSystem = world.Api.ModLoader.GetModSystem<ViconomyCore>();
            if (modSystem != null && !modSystem.TryPlaceBlock(world, byPlayer, itemstack, blockSel))
            {
                failureCode = "__ignore__";
                return false;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
        {
            if (!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {
                return false;
            }
            BlockSelection bs = blockSel.Clone();
            bs.Position = blockSel.Position.UpCopy(1);
            return base.CanPlaceBlock(world, byPlayer, bs, ref failureCode);
        }
    }

}
