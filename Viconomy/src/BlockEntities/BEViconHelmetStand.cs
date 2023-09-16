using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.BlockEntities
{
    public class BEViconHelmetStand : BEViconStall
    {

        public BEViconHelmetStand()
        {
            this.slotCount = 1;
            this.inventory = new ViconomyInventory(null, null, slotCount, stacksPerSlot);
            this.inventory.SlotModified += base.Inventory_SlotModified;

        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[slotCount][];
            for (int index = 0; index < slotCount; index++)
            {
                float scale = 1f;
                ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(index);



                float x = 0f;
                float y = -1.1f;
                float z = 0f;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(this.block.Shape.rotateY+90).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f).Scale(scale, scale, scale);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }
    }
}
