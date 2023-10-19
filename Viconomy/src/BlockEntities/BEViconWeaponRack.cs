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

        public override void ConfigureInventory()
        {
            this.inventory = new ViconomyInventory(this, null, null, StallSlotCount, StacksPerSlot);
            this.inventory.SetSlotFilter(0, ViconomyFilters.IsToolOrWeapon);
            this.inventory.SetSlotBackground(0, "vicon-toolrack");
            this.inventory.SetSlotFilter(1, ViconomyFilters.IsShield);
            this.inventory.SetSlotBackground(1, "vicon-shield");
            this.inventory.SetSlotFilter(2, ViconomyFilters.IsToolOrWeapon);
            this.inventory.SetSlotBackground(2, "vicon-toolrack");

        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[3][];
            ItemSlot wepLeftItem = inventory.FindFirstNonEmptyStockSlot(0);
            if (wepLeftItem != null )
            {

                bool isStupidlyOffset = wepLeftItem.Itemstack.Item != null && (wepLeftItem.Itemstack.Item.Code.Path.StartsWith("saw") ||
                      wepLeftItem.Itemstack.Item.Code.Path.StartsWith("hammer") ||
                      wepLeftItem.Itemstack.Item.Code.Path.StartsWith("cleaver") ||
                      wepLeftItem.Itemstack.Item.Code.Path.StartsWith("tong"));

                float stupidOffset = isStupidlyOffset ? 0.35f : 0;

                Matrixf wepLeft = new Matrixf()
                    .Translate(0.5f, 0.5f, 0.5f)
                    .RotateYDeg(this.block.Shape.rotateY)
                    .RotateXDeg(270)
                    .RotateYDeg(-45)
                    .Translate(0,0.35f-stupidOffset,0)
                    .Scale(0.8f, 0.8f, 0.8f)
                    .Translate(-0.5f, 0, -0.5f);
                tfMatrices[0] = wepLeft.Values;
            }


            ItemSlot shieldItem = inventory.FindFirstNonEmptyStockSlot(1);
            if (shieldItem != null && shieldItem.Itemstack.ItemAttributes.KeyExists("toolrackTransform") || true)
            {
                Matrixf shield = new Matrixf().Translate(0.5f, 0f, 0.5f)
                    .RotateYDeg(this.block.Shape.rotateY)
                    .Translate(0.325f, 0.45f,-0.10)
                    .Scale(0.8f, 0.8f, 0.8f)
                    .Translate(-0.5f, 0, -0.5f);
                    
                tfMatrices[1] = shield.Values;
            }

            ItemSlot wepRightItem = inventory.FindFirstNonEmptyStockSlot(2);
            if (wepRightItem != null)
            {
                bool isStupidlyOffset = wepRightItem.Itemstack.Item != null && (wepRightItem.Itemstack.Item.Code.Path.StartsWith("saw") ||
                     wepRightItem.Itemstack.Item.Code.Path.StartsWith("hammer") ||
                     wepRightItem.Itemstack.Item.Code.Path.StartsWith("cleaver") ||
                     wepRightItem.Itemstack.Item.Code.Path.StartsWith("tong"));

                float stupidOffset = isStupidlyOffset ? 0.30f : 0;

                Matrixf wepRight = new Matrixf()
                    .Translate(0.5f, 0.5f, 0.5f)
                    .RotateYDeg(this.block.Shape.rotateY)
                    .RotateXDeg(-270)
                    .RotateYDeg(135)
                    .Translate(0, -0.425f- stupidOffset, 0)
                    .Scale(0.8f, 0.8f, 0.8f)
                    .Translate(-0.5f, 0, -0.5f);

                tfMatrices[2] = wepRight.Values;
            }

            return tfMatrices;
        }
    }
}
