using Viconomy.Filters;
using Viconomy.Inventory.Impl;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.BlockEntities
{
    public class BEVinconHelmetStand : BEVinconContainer
    {
        public override int StallSlotCount => 1;

        public BEVinconHelmetStand()
        {
            bypassShelvableAttributes = true;
        }

        public override void ConfigureInventory()
        {
            ViconItemInventory inv = new ViconItemInventory(this, null, null, StallSlotCount, ProductStacksPerSlot);
            inv.SlotModified += base.Inventory_SlotModified;
            inv.SetSlotFilter(0, ViconomyFilters.IsHelmetSlot);
            inv.SetSlotBackground(0, "vicon-helmet");
            inventory = inv;
        }

        protected override float[][] GenTransformationMatrices()
        {
            float[][] tfMatrices = new float[StallSlotCount][];
            for (int i = 0; i < StallSlotCount; i++)
            {
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f);
                matrix.RotateYDeg(Block.Shape.rotateY + 90);
                matrix.Translate(-0.5f, -1.1f, -0.5f);
                tfMatrices[i] = matrix.Values;
            }

            return tfMatrices;
        }
    }
}
