using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vinconomy.BlockEntities
{
    public class BEVinconShelf : BEVinconContainer
    {
        protected override float[][] GenTransformationMatrices()
        {

            float[][] tfMatrices = new float[StallSlotCount][];
            for (int index = 0; index < StallSlotCount; index++)
            {
                if (index >= Block.SelectionBoxes.Length)
                {
                    modSystem.Mod.Logger.Warning($"Tried to render display for stall slot {index} outside of selection box bounds {Block.SelectionBoxes.Length} at {this.Pos}");
                    tfMatrices[index] = new Matrixf().Values;
                    continue;
                }  

                Cuboidf sb = Block.SelectionBoxes[index];

                float left = -.25f;
                float right = left + .5f;

                float x = (index % 2 == 0) ? left : right;
                float y = sb.MaxY - 0.37f;
                float z = -0.25f;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }
    }
}
