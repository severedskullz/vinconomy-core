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
    public class BlockVContainer : Block
    {
        

        public BlockVContainer()
        {
            this.PriorityInteract = true;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {
                BEViconStall viconBlock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconStall;
                if (viconBlock != null)
                {
                    viconBlock.Owner = byPlayer.PlayerUID;
                    viconBlock.OwnerName = byPlayer.PlayerName;
                }
            }

            return result;
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        /*
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
           
                List<Cuboidf> cubs = new List<Cuboidf>();
                
                cubs.Add(new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.1f));
                cubs.Add(new Cuboidf(0f, 0f, 0f, 1f, 0.0625f, 0.5f));
                cubs.Add(new Cuboidf(0f, 0.9375f, 0f, 1f, 1f, 0.5f));
                cubs.Add(new Cuboidf(0f, 0f, 0f, 0.0625f, 1f, 0.5f));
                cubs.Add(new Cuboidf(0.9375f, 0f, 0f, 1f, 1f, 0.5f));

            return cubs.ToArray();
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
          
            return new Cuboidf[]
            {
                new Cuboidf(0f, 0f, 0f, 1f, 1f, 1f).RotatedCopy(0f,0f ,0f, new Vec3d(0.5, 0.5, 0.5))
            };
        }
        */


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
                StallSlot[] slots = ((ViconomyInventory)be.Inventory).StallSlots;
                //In case we have some oddity with selections, just exit gracefully.
                if (selection.SelectionBoxIndex >= slots.Length)
                {
                    return interactions.ToArray();
                }

                StallSlot slot = slots[selection.SelectionBoxIndex];

                if (be.Owner != forPlayer.PlayerUID)
                {
                    if (slot.currency.Itemstack != null && slot.FindFirstNonEmptyStockSlot() != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "viconomy:stall-purchase",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "shift",
                            Itemstacks = new ItemStack[] { slot.currency.Itemstack }

                        });

                        ItemStack fiveStack = slot.currency.Itemstack.Clone();
                        fiveStack.StackSize = 5 * fiveStack.StackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "viconomy:stall-purchase-five",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = new string[] { "shift", "ctrl" },
                            Itemstacks = new ItemStack[] { fiveStack }
                        });
                    }
                } else {
                    ItemSlot firstSlot = slot.FindFirstNonEmptyStockSlot();
                    if (firstSlot != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "viconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "shift",
                            Itemstacks = new ItemStack[] { firstSlot.Itemstack }
                        });
                    } else {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "viconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "shift"
                        });
                    }
                }

                interactions.Add(new WorldInteraction
                {
                    ActionLangCode = "viconomy:stall-open-menu",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                });
                
            }

            return interactions.ToArray();
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BEViconStall vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEViconStall;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID)
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            else if (api.Side == EnumAppSide.Server)
                ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("viconomy:doesnt-own", new object[0]), EnumChatType.CommandError, null);
        }


    }

}
