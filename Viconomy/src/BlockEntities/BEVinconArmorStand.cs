using Vintagestory.API.Client;
using Viconomy.Filters;
using Vintagestory.API.Common;
using Viconomy.Inventory.Impl;

namespace Viconomy.BlockEntities
{
    public class BEVinconArmorStand : BEVinconContainer
    {
        public override int StallSlotCount => 9;
        public override int BulkPurchaseAmount => 1;
        
        public BEVinconArmorStand() { 
            bypassShelvableAttributes = true;
        }

        public override void ConfigureInventory()
        {
            ViconItemInventory inv = new ViconItemInventory(this, null, null, StallSlotCount, ProductStacksPerSlot);
            inv.SetSlotFilter(0, ViconomyFilters.IsBootsSlot);
            inv.SetSlotBackground(0, "vicon-boots");
            inv.SetSlotFilter(1, ViconomyFilters.IsPantsSlot);
            inv.SetSlotBackground(1, "vicon-legs");
            inv.SetSlotFilter(2, ViconomyFilters.IsHandSlot);
            inv.SetSlotBackground(2, "vicon-hands");
            inv.SetSlotFilter(3, ViconomyFilters.IsWaistSlot);
            inv.SetSlotBackground(3, "vicon-belt");
            inv.SetSlotFilter(4, ViconomyFilters.IsBodySlot);
            inv.SetSlotBackground(4, "vicon-body");
            inv.SetSlotFilter(5, ViconomyFilters.IsArmSlot);
            inv.SetSlotBackground(5, "vicon-arms");
            inv.SetSlotFilter(6, ViconomyFilters.IsNeckSlot);
            inv.SetSlotBackground(6, "vicon-neck");
            inv.SetSlotFilter(7, ViconomyFilters.IsFaceSlot);
            inv.SetSlotBackground(7, "vicon-face");
            inv.SetSlotFilter(8, ViconomyFilters.IsHelmetSlot);
            inv.SetSlotBackground(8, "vicon-helmet");
            inventory = inv;
        }


        /*
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



        protected override float[][] GenTransformationMatrices()
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
                        matrix.RotateYDeg(Block.Shape.rotateY - 90);
                        matrix.Translate(-0.05f, 0.10f, -0.0125f);
                    }
                    else if (ViconomyFilters.IsBootsSlot(slot))
                    {
                        matrix.RotateYDeg(Block.Shape.rotateY + 90);
                        matrix.Translate(-0.05f, 0.10f, -0.0125f);
                    }
                    else
                    {
                        matrix.RotateYDeg(Block.Shape.rotateY + 90);
                        matrix.Translate(0f, 0.050f, -0.01250f);
                    }
                }
                matrix.Translate(-0.5f, 0f, -0.5f);
                tfMatrices[i] = matrix.Values;
            }
            
            return tfMatrices;
        }
    }
}
