using System;
using System.Text;
using Vinconomy.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vinconomy.ItemTypes
{
    public class ItemSculptureBundle : Item
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
  
            
            if (byEntity is EntityPlayer)
            {
                IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);

                if (byEntity.Controls.Sneak && byEntity.Controls.Sprint)
                {
                    if (blockSel != null)
                    {
                        PlaceBundledItems(slot, player, blockSel);
                    }
                    
                } else if (byEntity.Controls.Sneak) {
                    GiveBundledItems(slot, player);

                }

            }
        }

        private bool PlaceBundledItems(ItemSlot slot, IPlayer player, BlockSelection blockSel)
        {
            //BlockFacing[] horVer = Block.SuggestedHVOrientation(player, blockSel);
            //AssetLocation blockCode = this.block.CodeWithVariant(this.variantCode, horVer[0].Code);

            string failureCode = null;

            ITreeAttribute treeAttr = slot.Itemstack.Attributes;
            int sizeX = treeAttr.GetAsInt("SizeX");
            int sizeY = treeAttr.GetAsInt("SizeY");
            int sizeZ = treeAttr.GetAsInt("SizeZ");
            ITreeAttribute contents = (TreeAttribute)treeAttr.GetTreeAttribute("Contents");
            EntityAgent ent = player.Entity;
            Vec3d pos = blockSel.FullPosition;



            //Create the matrix, and resolve all the block items in the bundle
            ItemStack[][][] matrix = new ItemStack[sizeY][][];
            for (int y = 0; y < sizeY; y++)
            {
                matrix[y] = new ItemStack[sizeX][];
                for (int x = 0; x < sizeX; x++)
                {
                    matrix[y][x] = new ItemStack[sizeZ];
                    for (int z = 0; z < sizeZ; z++)
                    {
                        ItemStack blockStack = contents.GetItemstack(String.Format("{0}-{1}-{2}", z, y, x));
                        if (blockStack != null)
                        {
                            matrix[y][x][z] = blockStack;
                            blockStack.ResolveBlockOrItem(ent.World);
                        }
                    }
                }
            }


            // Rotate the Matrix for each Y level
            BlockFacing[] facing = Block.SuggestedHVOrientation(player, blockSel);
            int numRotations = facing[0].Index;
            for (int y = 0; y < sizeY; y++)
            {
                for (int i = 0; i < numRotations; i++)
                {
                    VinUtils.Rotate(matrix[y]);
                    
                }
            }

            
            // Check if we can place all the blocks.
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        ItemStack blockStack = matrix[y][z][x];
                        if (blockStack != null)
                        {
                            BlockPos newPos = new BlockPos(x + (int)pos.X - (sizeX / 2), y + (int)pos.Y, z + (int)pos.Z - (sizeZ / 2), 0);
                            //Block worldBlock = ent.World.BlockAccessor.GetBlock(pos.X, pos.X, pos.X);
                            BlockSelection targetPos = new BlockSelection(newPos, blockSel.Face, blockSel.Block);
                            bool result = blockStack.Block.CanPlaceBlock(player.Entity.World, player, targetPos, ref failureCode);
                            if (!result)
                            {
                                return false;
                            }
                        }

                    }
                }
            }

            // Place all the blocks
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        ItemStack blockStack = matrix[y][z][x];
                        if (blockStack != null)
                        {
                            BlockPos newPos = new BlockPos(x + (int)pos.X - (sizeX/2), y + (int)pos.Y, z + (int)pos.Z - (sizeZ/2), 0);
                            BlockSelection targetPos = new BlockSelection(newPos, blockSel.Face, blockSel.Block);

                            Block block = blockStack.Block;
                            AssetLocation blockCode = block.CodeWithVariant("horizontalorientation", facing[0].Code);
                            Block orientedBlock = api.World.BlockAccessor.GetBlock(blockCode);
                            if (orientedBlock != null)
                            {
                                block = orientedBlock;
                            }


                            bool result = block.TryPlaceBlock(api.World, player, blockStack, targetPos, ref failureCode);
                            if (!result)
                            {
                                api.Logger.Error("Error placing block at " + x + " " + y + " " + z + ": " + failureCode);
                                player.InventoryManager.TryGiveItemstack(blockStack, true);
                                if (blockStack.StackSize > 0)
                                {
                                    //Console.WriteLine("Should have spawned item for " + String.Format("{0}-{1}-{2}", x, y, z));
                                    ent.World.SpawnItemEntity(blockStack, ent.SidedPos.XYZ.Add(0.0f, 0.5f, 0.0f), null);
                                }
                            } else
                            {
                                if (block is BlockChisel)
                                {
                                    BlockEntityMicroBlock be = api.World.BlockAccessor.GetBlockEntity<BlockEntityMicroBlock>(newPos);
                                    if (be != null)
                                    {
                                        be.RotateModel(numRotations * 90, null);
                                    }
                                }
                            }
                        }

                    }
                }
            }
            slot.TakeOut(1);
            return true;
        }

        private void GiveBundledItems(ItemSlot slot, IPlayer player)
        {
            ITreeAttribute treeAttr = slot.Itemstack.Attributes;
            int sizeX = treeAttr.GetAsInt("SizeX");
            int sizeY = treeAttr.GetAsInt("SizeY");
            int sizeZ = treeAttr.GetAsInt("SizeZ");
            ITreeAttribute contents = (TreeAttribute)treeAttr.GetTreeAttribute("Contents");
            EntityAgent ent = player.Entity;

            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        ItemStack blockStack = contents.GetItemstack(String.Format("{0}-{1}-{2}", x, y, z));
                        if (blockStack != null)
                        {
                            blockStack.ResolveBlockOrItem(ent.World);

                            player.InventoryManager.TryGiveItemstack(blockStack, true);
                            if (blockStack.StackSize > 0)
                            {
                                //Console.WriteLine("Should have spawned item for " + String.Format("{0}-{1}-{2}", x, y, z));
                                ent.World.SpawnItemEntity(blockStack, ent.SidedPos.XYZ.Add(0.0f, 0.5f, 0.0f), null);
                            }
                        }

                    }
                }
            }
            slot.TakeOut(1);
            slot.MarkDirty();
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ITreeAttribute attrs = inSlot.Itemstack.Attributes;
            dsc.AppendLine("Sculpture: " + attrs.GetString("SculptureName"));
            dsc.AppendLine("Size: " + attrs.GetInt("SizeX") + " x " + attrs.GetInt("SizeY") + " x " + attrs.GetInt("SizeZ"));
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
          

        }
    }
}
