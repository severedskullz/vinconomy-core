using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Viconomy.Filters;

namespace Viconomy.BlockEntities
{
    public class BEViconHelmetStand : BEViconStall
    {
        public override int StallSlotCount => 1;

        public BEViconHelmetStand()
        {
            inventory = new ViconomyInventory(null, null, StallSlotCount, StacksPerSlot);
            inventory.SlotModified += base.Inventory_SlotModified;
            inventory.SetSlotFilter(0, ViconomyFilters.IsHelmetSlot);
            inventory.SetSlotBackground(0, "vicon-helmet");
        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[1][];
            Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(this.block.Shape.rotateY + 90).Translate(0, -1.1f, 0).Translate(-0.5f, 0f, -0.5f);
            tfMatrices[0] = matrix.Values;
            return tfMatrices;
        }
    }
}
