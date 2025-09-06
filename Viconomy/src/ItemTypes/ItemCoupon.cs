using System.Text;
using Viconomy.Registry;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Viconomy.ItemTypes
{
    public class ItemCoupon : Item
    {
        public const string OWNER = "CouponOwner";
        public const string OWNER_NAME = "CouponOwnerName";

        public const string VALUE = "CouponValue";
        public const string DISCOUNT_TYPE = "DiscountType";
        public const string BONUS_TYPE = "BonusType";
        public const string CONSUME_COUPON = "ConsumeCoupon";
        public const string IS_BLACKLIST = "ItemBlacklist";
        public const string APPLIED_SHOPS = "AppliedShops";
        public const string APPLIED_SHOPS_COUNT = "AppliedShopLength";
        public const string ITEM_LIST = "ItemList";
        public const string ITEM_LIST_COUNT = "ItemListLength";
        public const string NAME = "CouponName";

        public const string DISCOUNT_TYPE_PERCENT = "Percent";
        public const string DISCOUNT_TYPE_UNIT = "Units";
        public const string BONUS_TYPE_DISCOUNT = "Discount";
        public const string BONUS_TYPE_PRODUCT = "Bonus";

        public override string GetHeldItemName(ItemStack itemStack)
        {
            return itemStack.Attributes.GetString(NAME, Lang.Get("vinconomy:item-coupon"));
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ITreeAttribute tree = inSlot.Itemstack.Attributes;

            if (!tree.HasAttribute(NAME))
            {
                dsc.AppendLine("This item seems to be misconfigured. How did you get a hold of this?");
                return;
            }
            int value = tree.GetInt(VALUE);
            string discountType = tree.GetString(DISCOUNT_TYPE);
            string bonusType = tree.GetString(BONUS_TYPE);
            bool consume = tree.GetBool(CONSUME_COUPON);

            string couponTypeStr = consume ? Lang.Get("vinconomy:gui-single-use") : Lang.Get("vinconomy:gui-reusable");
            string discountTypeStr = discountType == DISCOUNT_TYPE_UNIT ? Lang.Get("vinconomy:gui-units") : Lang.Get("vinconomy:gui-percent");
            string bonusTypeStr = bonusType == BONUS_TYPE_PRODUCT ? Lang.Get("vinconomy:gui-bonus-product") : Lang.Get("vinconomy:gui-price-discount");


            dsc.AppendLine(Lang.Get("vinconomy:gui-f-coupon-text", [couponTypeStr, value, discountTypeStr, bonusTypeStr]));
            dsc.AppendLine();

            VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            ShopRegistry registry = modSystem.GetRegistry();

            dsc.AppendLine("Valid Shops:");
            if (tree.HasAttribute(APPLIED_SHOPS))
            {
                int appliedShopsLength = tree.GetInt(APPLIED_SHOPS_COUNT);
                ITreeAttribute shopTree = tree.GetOrAddTreeAttribute(APPLIED_SHOPS);
                for (int i = 0; i < appliedShopsLength; i++)
                {
                    if (i != 0)
                        dsc.Append(", ");

                    int shopId = shopTree.GetInt("ID-" + i);
                    ShopRegistration reg = registry.GetShop(shopId);
                    if (reg != null)
                    {
                        dsc.Append( reg.Name);
                    }
                    else
                    {
                        dsc.Append(shopTree.GetString("Name-" + i));
                    }

                    if (withDebugInfo)
                    {
                        dsc.Append($" ({shopId})");
                    }

                }

            }
            else
            {
                dsc.AppendLine("All Shops owned by " + tree.GetString(OWNER_NAME));
            }

            if (tree.HasAttribute(ITEM_LIST))
            {
                bool isBlacklist = tree.GetBool(IS_BLACKLIST);
                if (isBlacklist)
                    dsc.AppendLine("Item Blacklist");
                else
                    dsc.AppendLine("Item Whitelist");

                ITreeAttribute itemList = tree.GetOrAddTreeAttribute(ITEM_LIST);
                ItemStack[] items = new ItemStack[tree.GetInt(ITEM_LIST_COUNT)];
                for (int i = 0; i < items.Length; i++)
                {
                    ItemStack itemStack = null;
                    string itemCode = itemList.GetString(i.ToString());
                    Item item = world.GetItem(itemCode);
                    if (item != null)
                        itemStack = new ItemStack(item, 1);
                    else
                    {
                        Block block = world.GetBlock(itemList.GetString(i.ToString()));
                        if (block != null)
                            itemStack = new ItemStack(block, 1);
                    }

                    if (itemStack != null)
                    {
                        if (i != 0)
                            dsc.Append(", ");

                        dsc.Append(itemStack.GetName());
                    }
                    else
                    {
                        dsc.Append(itemCode);
                    }
                }

            }
            dsc.AppendLine();
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }
    }
}
