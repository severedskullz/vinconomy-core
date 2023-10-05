using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Viconomy.Filters;
using Vintagestory.API.Common;
using Viconomy.BlockTypes;

namespace Viconomy.BlockEntities
{
    public class BEViconArmorStand : BEViconStall
    {
        public override int StallSlotCount => 9;

        public override void ConfigureInventory()
        {
            inventory = new ViconomyInventory(null, null, StallSlotCount, StacksPerSlot);
            inventory.SetSlotFilter(0, ViconomyFilters.IsBootsSlot);
            inventory.SetSlotBackground(0, "vicon-boots");
            inventory.SetSlotFilter(1, ViconomyFilters.IsPantsSlot);
            inventory.SetSlotBackground(1, "vicon-legs");
            inventory.SetSlotFilter(2, ViconomyFilters.IsHandSlot);
            inventory.SetSlotBackground(2, "vicon-hands");
            inventory.SetSlotFilter(3, ViconomyFilters.IsWaistSlot);
            inventory.SetSlotBackground(3, "vicon-belt");
            inventory.SetSlotFilter(4, ViconomyFilters.IsBodySlot);
            inventory.SetSlotBackground(4, "vicon-body");
            inventory.SetSlotFilter(5, ViconomyFilters.IsArmSlot);
            inventory.SetSlotBackground(5, "vicon-arms");
            inventory.SetSlotFilter(6, ViconomyFilters.IsNeckSlot);
            inventory.SetSlotBackground(6, "vicon-neck");
            inventory.SetSlotFilter(7, ViconomyFilters.IsFaceSlot);
            inventory.SetSlotBackground(7, "vicon-face");
            inventory.SetSlotFilter(8, ViconomyFilters.IsHelmetSlot);
            inventory.SetSlotBackground(8, "vicon-helmet");
        }


        /*
            RegisterCustomIcon("arms");
            RegisterCustomIcon("belt");
            RegisterCustomIcon("body");
            RegisterCustomIcon("boots");
            RegisterCustomIcon("face");
            RegisterCustomIcon("general");
            RegisterCustomIcon("general2");
            RegisterCustomIcon("hands");
            RegisterCustomIcon("hands2");
            RegisterCustomIcon("hat");
            RegisterCustomIcon("helmet");
            RegisterCustomIcon("legs");
            RegisterCustomIcon("neck");
            RegisterCustomIcon("payment");
            RegisterCustomIcon("produce");
            RegisterCustomIcon("shield");
            RegisterCustomIcon("toolrack");
            RegisterCustomIcon("weapon");

            EnumCharacterDressType.Foot = "boots"
            EnumCharacterDressType.Hand="gloves"
            EnumCharacterDressType.Shoulder="cape"
            EnumCharacterDressType.Head="hat"
            EnumCharacterDressType.LowerBody="trousers"
            EnumCharacterDressType.UpperBody="shirt"
            EnumCharacterDressType.UpperBodyOver="pullover"
            EnumCharacterDressType.Neck="necklace"
            EnumCharacterDressType.Arm="bracers"
            EnumCharacterDressType.Waist="belt"
            EnumCharacterDressType.Emblem="medal"
            EnumCharacterDressType.Face="mask"
            EnumCharacterDressType.ArmorHead="armorhead"
            EnumCharacterDressType.ArmorBody="armorbody"
            EnumCharacterDressType.ArmorLegs="armorlegs"
        */



        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[StallSlotCount][];
            for (int i = 0; i < StallSlotCount; i++)
            {
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f);

                ItemSlot slot = inventory.FindFirstNonEmptyStockSlot(i);
                if (slot != null && slot.Itemstack != null)
                { 
                    if (ViconomyFilters.IsWaistSlot(slot))
                    {
                        matrix.Scale(1.01f, 1.01f, 1.01f);
                        matrix.RotateYDeg(this.block.Shape.rotateY - 90);
                        
                    }
                    else if (ViconomyFilters.IsBootsSlot(slot))
                    {
                        matrix.Scale(1.1f, 1.1f, 1.2f);
                        matrix.Translate(0.02f, 0.10f, -0.025f);
                    } else
                    {
                        matrix.RotateYDeg(this.block.Shape.rotateY + 90);
                    }
                }
                matrix.Translate(-0.5f, 0f, -0.5f);
                tfMatrices[i] = matrix.Values;
            }
            
            return tfMatrices;
        }
    }
}
