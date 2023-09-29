using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Viconomy.Filters;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;

namespace Viconomy.BlockEntities
{
    public class BEViconToolRack : BEViconStall
    {

        public BEViconToolRack()
        {
            this.inventory = new ViconomyInventory(null, null, StallSlotCount, StacksPerSlot);
            this.inventory.SlotModified += base.Inventory_SlotModified;
            this.inventory.SetSlotFilter(0, ViconomyFilters.IsToolOrWeapon);
            this.inventory.SetSlotFilter(1, ViconomyFilters.IsToolOrWeapon);
            this.inventory.SetSlotFilter(2, ViconomyFilters.IsToolOrWeapon);

        }

        protected override float[][] genTransformationMatrices()
        {
            tfMatrices = new float[StallSlotCount][];

            int perRow = (int) Math.Ceiling(StallSlotCount / 2.0f);


            for (int i = 0; i < StallSlotCount; i++)
            {

                float yOff = i / perRow == 0 ? -0.0f : -0.5f;
                float xOff = (i % perRow) * (1.0f/(perRow));
                float zOff = i / perRow == 0 ? -0.65f : -0.55f ;
                ItemSlot item = inventory.FindFirstNonEmptyStockSlot(i);
                if (item != null)
                {
                    JsonObject attr = item.Itemstack.ItemAttributes["toolrackTransform"];
                    Matrixf wepRight = new Matrixf()
                    .Translate(0.5f, 0.5f, 0.5f)
                    //.Translate(xOff, yOff, zOff)
                    .Translate(xOff-.25,yOff,zOff)
                    .RotateYDeg(this.block.Shape.rotateY-270)
                    .RotateZDeg(90)
                    //.Translate(xOff, 0, 0)
                    //.RotateZDeg(90)
                    //.Translate(attr["translation"]["x"].AsFloat(0), attr["translation"]["y"].AsFloat(0), attr["translation"]["z"].AsFloat(0));
                    
                    
                    .Scale(0.6f, 0.6f, 0.6f)
                    .Translate(-0.5, -0.5, -0.5)
                    ;
                    tfMatrices[i] = wepRight.Values;
                }

            }

            return tfMatrices;
        }
    }
}
