using Vintagestory.API.Client;
using Viconomy.Filters;
using Viconomy.Inventory.Impl;

namespace Viconomy.BlockEntities
{
    public class BEVinconHelmetStand : BEVinconContainer
    {
        public override int StallSlotCount => 1;

        public BEVinconHelmetStand()
        {
            ViconItemInventory inv = new ViconItemInventory(this, null, null, StallSlotCount, ProductStacksPerSlot);
            inv.SlotModified += base.Inventory_SlotModified;
            inv.SetSlotFilter(0, ViconomyFilters.IsHelmetSlot);
            inv.SetSlotBackground(0, "vicon-helmet");
            inventory = inv;
        }

        protected override float[][] GenTransformationMatrices()
        {
            float[][] tfMatrices = new float[1][];
            Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY + 90).Translate(0, -1.1f, 0).Translate(-0.5f, 0f, -0.5f);
            tfMatrices[0] = matrix.Values;
            return tfMatrices;
        }
    }
}
