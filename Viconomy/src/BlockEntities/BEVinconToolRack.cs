using System;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Viconomy.Filters;
using Vintagestory.API.Common;

namespace Viconomy.BlockEntities
{
    public class BEVinconToolRack : BEVinconContainer
    {

        public override void ConfigureInventory()
        {
            this.inventory = new ViconomyInventory(this, null, null, StallSlotCount, StacksPerSlot);
            for (int i = 0; i < StallSlotCount; i++)
            {
                inventory.SetSlotFilter(i, ViconomyFilters.IsToolOrWeapon);
                inventory.SetSlotBackground(i, "vicon-toolrack");
            }
        }

        protected override float[][] genTransformationMatrices()
        {
            tfMatrices = new float[StallSlotCount][];

            int perRow = (int) Math.Ceiling(StallSlotCount / 2.0f);

            for (int i = 0; i < StallSlotCount; i++)
            {
                float x = (i / 2 == 0) ? 0.5f : 0f; // Actually Y
                float y = (i / 2 == 0) ? 0.125f : 0.225f; // Actually Z.
                float z = (i % 2 == 0) ? 0 : 0.5f; // Actually X
                                
                ItemSlot item = inventory.FindFirstNonEmptyStockSlot(i);
                if (item != null)
                {
                    bool isStupidlyOffset = item.Itemstack.Item != null && (item.Itemstack.Item.Code.Path.StartsWith("saw") ||
                        item.Itemstack.Item.Code.Path.StartsWith("hammer") ||
                        item.Itemstack.Item.Code.Path.StartsWith("cleaver") ||
                        item.Itemstack.Item.Code.Path.StartsWith("tong"));

                    float stupidOffset = isStupidlyOffset ? 0.25f : 0;

                    Matrixf wepRight = new Matrixf()
                    .Translate(.5f, .5f, .5f)
                    .Translate(.0f, 0, 0)
                    .RotateYDeg(Block.Shape.rotateY-90)
                    .RotateZDeg(90)
                    .Translate(x, y- stupidOffset, z)
                    .Translate(-.5f, -.5f, -.5f)
                    .Scale(0.5f, 0.5f, 0.5f);
                    tfMatrices[i] = wepRight.Values;
                }
            }

            return tfMatrices;
        }
    }
}
