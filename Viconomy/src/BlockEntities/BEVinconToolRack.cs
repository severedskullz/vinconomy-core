using Vintagestory.API.Client;
using Vinconomy.Filters;
using Vintagestory.API.Common;
using Vinconomy.Inventory.Impl;

namespace Vinconomy.BlockEntities
{
    public class BEVinconToolRack : BEVinconContainer
    {

        public override string AttributeTransformCode => "toolrackTransform";

        public override void ConfigureInventory()
        {
            VinconItemInventory inv = new VinconItemInventory(this, null, null, StallSlotCount, ProductStacksPerSlot);
            for (int i = 0; i < StallSlotCount; i++)
            {
                inv.SetSlotFilter(i, VinconomyFilters.IsToolOrWeapon);
                inv.SetSlotBackground(i, "vicon-toolrack");
            }
            inventory = inv;
        }

        protected override float[][] GenTransformationMatrices()
        {
            tfMatrices = new float[StallSlotCount][];

            for (int i = 0; i < StallSlotCount; i++)
            {
                float x = (i / 2 == 0) ? 0.55f : 0.05f; // Actually Y
                float y = (i / 2 == 0) ? -0.165f : -0.06f; // Actually Z.
                float z = (i % 2 == 0) ? 0 : 0.5f; // Actually X
                                
                ItemSlot item = inventory.FindFirstNonEmptyStockSlot(i);
                if (item != null)
                {
                    Matrixf wepRight = new Matrixf()
                    .Translate(.5f, .5f, .5f)
                    .RotateYDeg(Block.Shape.rotateY-90)
                    .RotateZDeg(90)
                    .Translate(x, y, z)
                    .Translate(-.5f, -.5f, -.5f)
                    .Scale(0.55f, 0.55f, 0.55f);
                    tfMatrices[i] = wepRight.Values;
                }
            }

            return tfMatrices;
        }
    }
}
