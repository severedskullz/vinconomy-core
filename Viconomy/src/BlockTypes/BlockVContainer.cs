using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Viconomy.BlockTypes
{
    public class BlockVContainer : TextureSwappableBlock
    {
        

        public BlockVContainer()
        {
            this.PlacedPriorityInteract = true;
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            //Console.WriteLine(api.Side + ": On interaction start was called!");
            BEVinconBase be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconBase;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return true;
        }



        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BEVinconBase be = world.BlockAccessor.GetBlockEntity(selection.Position) as BEVinconBase;
            List<WorldInteraction> interactions = new List<WorldInteraction>();
            if (be != null)
            {
                int index = selection.SelectionBoxIndex;
                ItemSlot product = be.FindFirstNonEmptyStockSlotForStall(index);
                ItemSlot currency = be.GetCurrencyForStall(index);

                //StallSlot slot = slots[selection.SelectionBoxIndex];

                if (be.Owner != forPlayer.PlayerUID)
                {
                    if (currency.Itemstack != null && product != null)
                    {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = new ItemStack[] { currency.Itemstack }

                        });

                        ItemStack fiveStack = currency.Itemstack.Clone();
                        fiveStack.StackSize = 5 * fiveStack.StackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-purchase-bulk",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = new string[] { "sneak", "sprint" },
                            Itemstacks = new ItemStack[] { fiveStack }
                        });
                    }
                } else {
                    ItemSlot firstSlot = product;
                    if (firstSlot != null)
                    {
                        ItemStack helpSlot = firstSlot.Itemstack.Clone();
                        helpSlot.StackSize = 1;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak",
                            Itemstacks = new ItemStack[] { helpSlot }
                        });

                        ItemStack helpSlotStack = helpSlot.Clone();
                        helpSlotStack.StackSize = helpSlotStack.Collectible.MaxStackSize;
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCodes = new string[] { "sneak", "sprint" },
                            Itemstacks = new ItemStack[] { helpSlotStack }
                        });

                        if (currency.Itemstack != null)
                        {
                            interactions.Add(new WorldInteraction
                            {
                                ActionLangCode = "vinconomy:stall-purchase",
                                MouseButton = EnumMouseButton.Right,
                                HotKeyCode = "sneak",
                                Itemstacks = new ItemStack[] { currency.Itemstack }

                            });

                            ItemStack fiveStack = currency.Itemstack.Clone();
                            fiveStack.StackSize = 5 * fiveStack.StackSize;
                            interactions.Add(new WorldInteraction
                            {
                                ActionLangCode = "vinconomy:stall-purchase-bulk",
                                MouseButton = EnumMouseButton.Right,
                                HotKeyCodes = new string[] { "sneak", "sprint" },
                                Itemstacks = new ItemStack[] { fiveStack }
                            });
                        }
                    } else {
                        interactions.Add(new WorldInteraction
                        {
                            ActionLangCode = "vinconomy:stall-add",
                            MouseButton = EnumMouseButton.Right,
                            HotKeyCode = "sneak"
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





        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            if (modSystem != null)
            {
                modSystem.BlockPlaced(this.Code, world, blockPos, byItemStack);
            }
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            if (modSystem != null && !modSystem.TryPlaceBlock(world, byPlayer, itemstack, blockSel))
            {
                failureCode = "__ignore__";
                return false;
            }
            return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {
                BEVinconBase viconBlock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEVinconBase;
                if (viconBlock != null)
                {
                    viconBlock.SetOwner(byPlayer);
                    //viconBlock.SecondaryMaterial = SecondaryMaterial;
                    //viconBlock.DecoMaterial = DecoMaterial;
                }
            }

            return result;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            if (byPlayer == null)
                return;

            BEVinconBase vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEVinconBase;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID || byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
                if (modSystem != null && !modSystem.BlockBroken(this.Code, world, pos, byPlayer, dropQuantityMultiplier))
                {
                    return;
                }
                DoOnBlockBroken(world, pos, byPlayer);
            }

            else if (api.Side == EnumAppSide.Server)
                ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("vinconomy:doesnt-own", new object[0]), EnumChatType.CommandError, null);
        }


    }

}
