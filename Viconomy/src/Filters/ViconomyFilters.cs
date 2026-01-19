using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.Filters
{
    public class ViconomyFilters
    {
        public static bool IsHelmetSlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.ArmorHead, EnumCharacterDressType.Head, EnumCharacterDressType.Shoulder);
        }

        public static bool IsBodySlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.ArmorBody, EnumCharacterDressType.UpperBody, EnumCharacterDressType.UpperBodyOver);
        }
        public static bool IsPantsSlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.LowerBody, EnumCharacterDressType.ArmorLegs);
        }

        public static bool IsFaceSlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.Neck, EnumCharacterDressType.Face);
        }

        public static bool IsArmSlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.Arm, EnumCharacterDressType.Hand, EnumCharacterDressType.Shoulder);
        }
        public static bool IsWaistSlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.Waist);
        }

        public static bool IsDressType(ItemSlot slot, params EnumCharacterDressType[] dressTypes)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            JsonObject attr = slot.Itemstack.Collectible.Attributes;
            if (attr == null)
            {
                return false;
            }

            string stackDressType = attr["clothescategory"].AsString(null);
            if (stackDressType == null)
                return false;

            foreach (var dressType in dressTypes)
            {
                if (dressType.ToString().Equals(stackDressType, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;

        }

        public static bool IsDressType(ItemSlot slot, params string[] dressTypes)
        {
            if (slot == null || slot.Itemstack == null)
                return false;

            JsonObject attr = slot.Itemstack.Collectible.Attributes;
            if (attr == null)
            {
                return false;
            }

            string stackDressType = attr["clothescategory"].AsString(null);
            if (stackDressType == null)
                return false;

            foreach ( var dressType in dressTypes)
            {
                if (dressType.Equals(stackDressType, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
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
                || slot.Itemstack.Item?.Code.Path.StartsWith("arrow-") == true;
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

            bool isToolOrWeapon = IsToolOrWeapon(slot);
            bool isClothingOrArmor = IsClothingOrArmor(slot);
            bool isShield = IsShield(slot);


            return IsFaceSlot(slot) || (!isToolOrWeapon && !isClothingOrArmor && !isShield); 
        }

        public static bool IsBootsSlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.Foot);
        }

        public static bool IsHandSlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.Hand);
        }

        public static bool IsNeckSlot(ItemSlot slot)
        {
            return IsDressType(slot, EnumCharacterDressType.Neck, EnumCharacterDressType.Emblem);
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

        public static bool IsFoodContainer(ItemSlot slot)
        {
            ItemStack stack = slot.Itemstack;
            if (stack == null) return false;

            return stack.Block is IBlockMealContainer || stack.Block is BlockCookingContainer; // || (stack.Block is BlockContainer container && stack.Block?.Attributes["mealContainer"]?.AsBool() == true);
        }
    }
}
