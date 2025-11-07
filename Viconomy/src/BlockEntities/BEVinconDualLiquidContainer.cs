using System;
using Viconomy.BlockTypes;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVinconDualLiquidContainer : BEVinconLiquidContainer
    {
        public override int StallSlotCount => 2;
        public override int BulkPurchaseAmount => 10;


        public override MeshData GenMesh(int stallSlot)
        {
            if (Block == null) return null;
            LiquidStallSlot stall = inventory.GetStall<LiquidStallSlot>(stallSlot);

            ItemStack stacks = stall.GetSlot(0).Itemstack;

            BlockVDualLiquidContainer ownBlock = Block as BlockVDualLiquidContainer;
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

        protected override float[][] GenTransformationMatrices()
        {

            float[][] tfMatrices = new float[StallSlotCount][];
            for (int index = 0; index < StallSlotCount; index++)
            {
                Cuboidf sb = Block.SelectionBoxes[index];
                float left = .15f;
                float right = left +.425f;

                float x = (index % 2 == 0) ? left : right;
                float y = 0.3f;
                float z = 0.2f;

                float scaleX = 0.275f;
                float scaleY = 0.55f;
                float scaleZ = 0.60f;

                LiquidStallSlot stall = inventory.GetStall<LiquidStallSlot>(index);
                ItemStack stack = stall.FindFirstNonEmptyStockSlot()?.Itemstack;
                WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(stack);
                if (props != null)
                {
                    float numLiters = stack.StackSize / props.ItemsPerLitre;
                    float capacityLites = Math.Min(50, stall.LiterCapacity);
                    scaleY = Math.Min(1, numLiters / capacityLites) * 0.55f;
                }

                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f).Scale(scaleX,scaleY, scaleZ);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }

        protected override void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(-0.5f, 0f, -0.5f);
            for (int i = 0; i < StallSlotCount; i++)
            {
                MeshData data = GenMesh(i);
                if (data != null)
                {
                    mesher.AddMeshData(data, tfMatrices[i]);
                }
            }
            
        }

    }
}
