
using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Viconomy.Filters
{
    public class ViconomyFilters
    {
        public static bool IsHelmetSlot(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorHead)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Head)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Shoulder);
        }

        public static bool IsBodySlot(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorBody)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.UpperBody)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.UpperBodyOver);
        }
        public static bool IsPantsSlot(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return IsDressType(slot.Itemstack, EnumCharacterDressType.LowerBody)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.ArmorLegs);
        }

        public static bool IsFaceSlot(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return IsDressType(slot.Itemstack, EnumCharacterDressType.Neck)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Face);
        }

        public static bool IsArmSlot(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return IsDressType(slot.Itemstack, EnumCharacterDressType.Arm)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Hand)
                || IsDressType(slot.Itemstack, EnumCharacterDressType.Shoulder);
        }
        public static bool IsWaistSlot(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

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

        public static bool IsClothingOrArmor(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return IsHelmetSlot(slot) || IsBodySlot(slot) || IsArmSlot(slot) || IsWaistSlot(slot) || IsPantsSlot(slot) || IsFaceSlot(slot) || IsNeckSlot(slot) || IsHandSlot(slot);
        }

        public static bool IsToolOrWeapon(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return slot.Itemstack.Item?.Attributes?.KeyExists("toolrackTransform") == true 
                || slot.Itemstack.Item?.Code.Path.StartsWith("bugnet") == true
                || slot.Itemstack.Item?.Code.Path.StartsWith("arrow") == true;
        }

        public static bool IsShield(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return slot.Itemstack.Item?.Code.Path.StartsWith("shield") == true;
        }

        public static bool IsGenericItem(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            return !IsToolOrWeapon(slot) && !IsClothingOrArmor(slot) && !IsShield(slot); 
        }

        public static bool IsBootsSlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.Foot);
        }

        public static bool IsHandSlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.Hand);
        }

        public static bool IsNeckSlot(ItemSlot slot)
        {
            return IsDressType(slot.Itemstack, EnumCharacterDressType.Neck) || IsDressType(slot.Itemstack, EnumCharacterDressType.Emblem);
        }

        public static bool IsEmptyGachaSlot(ItemSlot slot)
        {

            return slot.Itemstack.Item?.Code.Path == "gachaball" && slot.Itemstack.Attributes.GetTreeAttribute("Contents") == null;
        }

        public static bool IsFilledGachaSlot(ItemSlot slot)
        {
            return slot.Itemstack.Item?.Code.Path == "gachaball" && slot.Itemstack.Attributes.GetTreeAttribute("Contents") != null;
        }

        public static bool IsMicroblock(ItemSlot slot)
        {
            return slot.Itemstack?.Block is BlockMicroBlock;
        }
    }
}
