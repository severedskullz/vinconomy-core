using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.BlockEntities
{
    public class BEVinconShelf : BEVinconContainer
    {
        protected override float[][] genTransformationMatrices()
        {

            float[][] tfMatrices = new float[StallSlotCount][];
            for (int index = 0; index < StallSlotCount; index++)
            {
                float scale = 0.35f;
                ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(index);
                if (slot != null)
                {
                    if (slot.Itemstack.Collectible.Code.Path == "crock-burned-east" 
                        || slot.Itemstack.Collectible.Code.Path == "bowl-meal" 
                        || slot.Itemstack.Collectible.Code.Path == "claypot-cooked" 
                        ||slot.Itemstack.Class != EnumItemClass.Block)
                    {
                        scale = .85f;
                    }
                }

                Cuboidf sb = block.SelectionBoxes[index];
                float left = .25f - (scale / 2);
                float right = left + .5f;

                float x = (index % 2 == 0) ? left : right;
                float y = sb.MaxY - 0.37f;
                float z = 0.25f - (scale / 2);
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(this.block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f).Scale(scale, scale, scale);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }
    }
}
