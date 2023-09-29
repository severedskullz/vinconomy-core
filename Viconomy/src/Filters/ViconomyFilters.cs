
using System;
using Vintagestory.API.Common;

namespace Viconomy.Filters
{
    public class ViconomyFilters
    {
        public static bool IsHelmetSlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorHead)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Head)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Shoulder);
        }

        public static bool IsBodySlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorBody)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.UpperBody)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.UpperBodyOver);
        }
        public static bool IsPantsSlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.LowerBody) 
                || IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorLegs)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Foot);
        }

        public static bool IsFaceSlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.Neck)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Face);
        }

        public static bool IsArmSlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.Arm)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Hand);
        }
        public static bool IsWaistSlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.Waist);
        }

        public static bool IsDressType(IItemStack itemstack, EnumCharacterDressType dressType)
            {
                if (itemstack == null || itemstack.Collectible.Attributes == null)
                {
                    return false;
                }
                string stackDressType = itemstack.Collectible.Attributes["clothescategory"].AsString(null);
                return stackDressType != null && dressType.ToString().Equals(stackDressType, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsToolOrWeapon(ItemSlot slot)
        {

            return slot.Itemstack.Item?.Attributes.KeyExists("tool") == true || true;

        }
    }
}
