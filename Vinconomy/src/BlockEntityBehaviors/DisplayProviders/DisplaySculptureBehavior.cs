using Commercially.Common.Util;
using Commercially.Vinconomy.Inventory.Slots;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.BlockEntityBehaviors.DisplayProviders
{
    public class DisplaySculptureBehavior : BaseDisplayContentsBehavior
    {

        public DisplaySculptureBehavior(BlockEntity blockentity) : base(blockentity)
        {
        }

        private float[][] GenTransformationMatrices(SculptureStallSlot stall)
        {
            int sizeXZ = stall.SculptureHorizontalSize;
            int sizeY = stall.SculptureVerticalSize;

            float[][] tfMatrices = new float[sizeXZ * sizeXZ * sizeY][];
            int i = 0;
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeXZ; z++)
                {
                    for (int x = 0; x < sizeXZ; x++)
                    {

                        int size = Math.Max(sizeXZ, sizeY);

                        float scale = 1.0f / size;
                        float offsetXZ = (sizeXZ - 1) / (sizeXZ * 2.0f);
                        float scaleRatio = Math.Clamp((float)sizeXZ / (float)sizeY,0,1);

                        TransformationData tfData = new TransformationData();
                        tfData.ScaleXYZ = scale;
                        tfData.X = (x * scale) - (offsetXZ * scaleRatio);
                        tfData.Y = y * scale + (2/32f); // Add in the 2 "pixel" height of the slab at the bottom for the model
                        tfData.Z = (z * scale) - (offsetXZ*scaleRatio) ;
                        tfData.preRotate += (float)((Block.Shape.rotateY));
                        tfMatrices[i] = tfData.BuildMatrix() ;
                        i++;
                    }
                }
            }

            return tfMatrices;
        }

        protected override void ApplyDefaultTranforms(ItemStack stack, MeshData mesh)
        {
            // dont do fuckin' SHIT. These are blocks. We want them to start at their original scale to make things easy.
        }

        protected override void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (mesher == null)
                return;


            if (ShouldRenderInventory)
            {

                ToggledStockItemSlot slot = null;

                for (int i = 0; i < _InventoryProvider.StallCount; i++)
                {
                    SculptureStallSlot stall = _InventoryProvider.GetStallSlot<SculptureStallSlot>(i);
                    int tfIndex = 0;
                    float[][] tfMatrices = GenTransformationMatrices(stall);
                    try
                    {
                        for (int layer = 0; layer < stall.SculptureVerticalSize; layer++)
                        {
                            for (int y = 0; y < stall.SculptureHorizontalSize; y++)
                            {
                                for (int x = 0; x < stall.SculptureHorizontalSize; x++)
                                {
                                    slot = stall.GetSlotForGrid(layer, x, y);
                                    if (slot != null && !slot.Empty && slot.Enabled && tfMatrices != null)
                                    {
                                        MeshData mesh = GetOrCreateMesh(slot, i);
                                        mesher.AddMeshData(mesh, tfMatrices[tfIndex]);
                                    }

                                        
                                    tfIndex++;
                                }
                            }
                        }

                        
                    }
                    catch (Exception e)
                    {
                        CommerciallyCore.Mod.Logger.Error($"Had some trouble rendering mesh in a stall @ {Pos.X} {Pos.Y} {Pos.Z} for slot {i}. Exception was {e.Message}");
                    }

                }
            }

        }

    }
    
}
