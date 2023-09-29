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

namespace Viconomy.BlockEntities
{
    public class BEViconWeaponRack : BEViconStall
    {
        public override int StallSlotCount => 3;

        public BEViconWeaponRack()
        {
            this.inventory = new ViconomyInventory(null, null, StallSlotCount, StacksPerSlot);
            this.inventory.SlotModified += base.Inventory_SlotModified;
            this.inventory.SetSlotFilter(0, ViconomyFilters.IsToolOrWeapon);
            this.inventory.SetSlotFilter(1, ViconomyFilters.IsToolOrWeapon);
            this.inventory.SetSlotFilter(2, ViconomyFilters.IsToolOrWeapon);

        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[3][];
            ItemSlot wepLeftItem = inventory.FindFirstNonEmptyStockSlot(0);
            if (wepLeftItem != null )
            {
               
                Matrixf wepLeft = new Matrixf()
                    .Translate(0.5f, 0.5f, 0.5f)
                    .RotateYDeg(this.block.Shape.rotateY)
                    .RotateXDeg(270)
                    .RotateYDeg(-45)
                    .Translate(0,0.35f,0)
                    //.Translate(attr["translation"]["x"].AsFloat(0), attr["translation"]["y"].AsFloat(0), attr["translation"]["z"].AsFloat(0))
                    //.RotateDeg(new Vintagestory.API.MathTools.Vec3f(attr["rotation"]["x"].AsFloat(0), attr["rotation"]["y"].AsFloat(0), attr["rotation"]["z"].AsFloat(0) + 45))
                    //.Scale(attr["scale"].AsFloat(1), attr["scale"].AsFloat(1), attr["scale"].AsFloat(1));
                    .Scale(0.8f, 0.8f, 0.8f)
                    .Translate(-0.5f, 0, -0.5f)
                    
                    ;
                tfMatrices[0] = wepLeft.Values;
            }


            ItemSlot shieldItem = inventory.FindFirstNonEmptyStockSlot(1);
            if (shieldItem != null && shieldItem.Itemstack.ItemAttributes.KeyExists("toolrackTransform") || true)
            {
                //Vintagestory.API.Datastructures.JsonObject attr = shieldItem.Itemstack.ItemAttributes["toolrackTransform"];
                Matrixf shield = new Matrixf().Translate(0.5f, 0f, 0.5f)
                    .RotateYDeg(this.block.Shape.rotateY)
                    .Translate(0.325f, 0.45f,-0.10)
                    .Scale(0.8f, 0.8f, 0.8f)
                    .Translate(-0.5f, 0, -0.5f)
                    ;
                    

                tfMatrices[1] = shield.Values;
            }

            ItemSlot wepRightItem = inventory.FindFirstNonEmptyStockSlot(2);
            if (wepRightItem != null)
            {
                
                Matrixf wepRight = new Matrixf()
                    .Translate(0.5f, 0.5f, 0.5f)
                    .RotateYDeg(this.block.Shape.rotateY)
                    .RotateXDeg(-270)
                    .RotateYDeg(135)
                    .Translate(0, -0.45f, 0)
                    //.Translate(-attr["translation"]["x"].AsFloat(0), -attr["translation"]["y"].AsFloat(0), -attr["translation"]["z"].AsFloat(0))
                    //.RotateDeg(new Vintagestory.API.MathTools.Vec3f(attr["rotation"]["x"].AsFloat(0), attr["rotation"]["y"].AsFloat(0), attr["rotation"]["z"].AsFloat(0) + 45))
                    //.Scale(attr["scale"].AsFloat(1), attr["scale"].AsFloat(1), attr["scale"].AsFloat(1));
                    .Scale(0.8f, 0.8f, 0.8f)
                    .Translate(-0.5f, 0, -0.5f)
                    
                    ;

                tfMatrices[2] = wepRight.Values;
            }

            return tfMatrices;
        }
    }
}
