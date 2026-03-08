using Vintagestory.API.Client;
using Vinconomy.Filters;
using Vintagestory.API.Common;
using Vinconomy.Inventory.Impl;

namespace Vinconomy.BlockEntities
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
            VinconItemInventory inv = new VinconItemInventory(this, null, null, StallSlotCount, ProductStacksPerSlot);
            inv.SetSlotFilter(0, VinconomyFilters.IsBootsSlot);
            inv.SetSlotBackground(0, "vicon-boots");
            inv.SetSlotFilter(1, VinconomyFilters.IsPantsSlot);
            inv.SetSlotBackground(1, "vicon-legs");
            inv.SetSlotFilter(2, VinconomyFilters.IsHandSlot);
            inv.SetSlotBackground(2, "vicon-hands");
            inv.SetSlotFilter(3, VinconomyFilters.IsWaistSlot);
            inv.SetSlotBackground(3, "vicon-belt");
            inv.SetSlotFilter(4, VinconomyFilters.IsBodySlot);
            inv.SetSlotBackground(4, "vicon-body");
            inv.SetSlotFilter(5, VinconomyFilters.IsArmSlot);
            inv.SetSlotBackground(5, "vicon-arms");
            inv.SetSlotFilter(6, VinconomyFilters.IsNeckSlot);
            inv.SetSlotBackground(6, "vicon-neck");
            inv.SetSlotFilter(7, VinconomyFilters.IsFaceSlot);
            inv.SetSlotBackground(7, "vicon-face");
            inv.SetSlotFilter(8, VinconomyFilters.IsHelmetSlot);
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
                    if (!VinconomyFilters.IsWaistSlot(slot))
                    {
                        matrix.Translate(0, 0.05f, 0);
                        
                    }
                    else 
                    {
                        matrix.Translate(0f, 0.05f, 0f);
                    }
                }
                matrix.RotateYDeg(Block.Shape.rotateY + 90);
                matrix.Translate(-0.5f, 0f, -0.5f);
                tfMatrices[i] = matrix.Values;
            }
            
            return tfMatrices;
        }
    }
}
