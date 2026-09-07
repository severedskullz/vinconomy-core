using Commercially.Common.Interfaces;
using Commercially.Common.Inventory.Slots;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.Impl;
using System;
using Vinconomy.ItemTypes;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Trading
{
    public class TradingUtil
    {

        public static bool IsMatchingItem(ItemStack desired, ItemStack toCompare, IWorldAccessor world, bool isFuzzy = false)
        {
            if (desired == null || toCompare == null) return false;

            if (isFuzzy)
                return desired.Collectible.Code.Equals(toCompare.Collectible.Code);
            else
                return desired.Equals(world, toCompare, GlobalConstants.IgnoredStackAttributes);
        }

        public static AggregatedSlots GetAllValidSlotsFor(IPlayer customer, ItemSlot desiredItem, bool isFuzzy = false)
        {
            return GetAllValidSlotsFor(customer, desiredItem.Itemstack, isFuzzy);
        }

        public static ItemStack GetItemStackClone(ItemStack stack, int stackSize = 0)
        {
            if (stack == null) return null;

            ItemStack newStack = stack.Clone();
            if (stackSize > 0)
            {
                newStack.StackSize = stackSize;
            }
            return newStack;
        }

        public static ItemStack GetItemStackClone(ItemSlot slot, int stackSize = 0)
        {
            if (slot?.Itemstack == null) return null;

            ItemStack stack = slot.Itemstack.Clone();
            if (stackSize > 0)
            {
                stack.StackSize = stackSize;
            }
            return stack;
        }

        public static GenericAggregatedSlots GetAllValidSlotsFor(ICoreAPI api, ItemStack desiredItem, ItemSlot[] slots, bool isFuzzy = false)
        {
            GenericAggregatedSlots aggregatedSlots = new GenericAggregatedSlots(api);
            foreach (ItemSlot itemSlot in slots)
            {
                if (IsMatchingItem(desiredItem, itemSlot.Itemstack, api.World, isFuzzy))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            return aggregatedSlots;
        }

        public static AggregatedSlots GetAllValidSlotsFor(IPlayer customer, ItemStack desiredItem, bool isFuzzy = false)
        {
            GenericAggregatedSlots aggregatedSlots = new GenericAggregatedSlots(customer.Entity.Api);
            if (desiredItem == null)
            {
                return aggregatedSlots;
            }

            ItemSlot handItem = customer.InventoryManager.ActiveHotbarSlot;
            if (IsMatchingItem(desiredItem, handItem.Itemstack, customer.Entity.World, isFuzzy))
            {
                aggregatedSlots.Add(handItem);
            }

            ItemSlot offhandItem = customer.InventoryManager.OffhandHotbarSlot;
            if (IsMatchingItem(desiredItem, offhandItem.Itemstack, customer.Entity.World, isFuzzy))
            {
                aggregatedSlots.Add(offhandItem);
            }

            IInventory hotbarInv = customer.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (IsMatchingItem(desiredItem, itemSlot.Itemstack, customer.Entity.World, isFuzzy))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = customer.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (IsMatchingItem(desiredItem, itemSlot.Itemstack, customer.Entity.World, isFuzzy))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }
            return aggregatedSlots;
        }

        public static GenericAggregatedSlots GetCouponsSlotsFor(IPlayer customer, ItemStack desiredItem, IShopComponent register)
        {
            GenericAggregatedSlots aggregatedSlots = new GenericAggregatedSlots(customer.Entity.Api);
            
            if (desiredItem == null)
            {
                return aggregatedSlots;
            }

            IOwnableReference ownable = register.GetComponent<IOwnableReference>();

            ItemSlot handItem = customer.InventoryManager.ActiveHotbarSlot;
            if (IsValidCoupon(handItem, desiredItem, ownable))
            {
                aggregatedSlots.Add(handItem);
            }

            ItemSlot offhandItem = customer.InventoryManager.OffhandHotbarSlot;
            if (IsValidCoupon(offhandItem, desiredItem, ownable))
            {
                aggregatedSlots.Add(offhandItem);
            }

            return aggregatedSlots;
        }

        public static ItemSlot GetCouponsSlotsForShop(IPlayer customer, ItemStack desiredItem, IShopComponent register)
        {
            IOwnableReference ownable = register.GetComponent<IOwnableReference>();

            ItemSlot handItem = customer.InventoryManager.ActiveHotbarSlot;
            if (IsValidCoupon(handItem, desiredItem, ownable))
            {
                return handItem;
            }

            ItemSlot offhandItem = customer.InventoryManager.OffhandHotbarSlot;
            if (IsValidCoupon(offhandItem, desiredItem, ownable))
            {
                return offhandItem;
            }

            return null; ;
        }

        private static bool IsValidCoupon(ItemSlot itemSlot, ItemStack desiredItem, IOwnableReference register)
        {
            if (register == null) return false;

            if (itemSlot.Itemstack != null && itemSlot.Itemstack.Class == EnumItemClass.Item)
            {
                if (itemSlot.Itemstack.Item.Code == "vinconomy:coupon")
                {
                    ITreeAttribute attrs = itemSlot.Itemstack.Attributes;
                    if (attrs.HasAttribute(ItemCoupon.ITEM_LIST))
                    {
                        string desiredCode = null;
                        if (desiredItem.Class == EnumItemClass.Item)
                            desiredCode = desiredItem.Item?.Code;
                        else
                            desiredCode = desiredItem.Block.Code;

                        bool isBlacklist = attrs.GetBool(ItemCoupon.IS_BLACKLIST);
                        ITreeAttribute itemList = attrs.GetTreeAttribute(ItemCoupon.ITEM_LIST);
                        int length = attrs.GetInt(ItemCoupon.ITEM_LIST_COUNT);

                        // By setting this to isBlacklist, we are being clever and avoiding "if(!isValid && !isBlacklist)" at the end of the branch
                        // Is this needed? No - probably not. Does it make me feel smart? Yes.
                        bool isValid = isBlacklist;
                        for (int i = 0; i < length; i++)
                        {
                            string itemCode = itemList.GetString(i.ToString());

                            // Its on the black list, so it doesnt matter if its a valid shop - we can just return false
                            // If we never return false here, isValid should be true, since isValid == isBlacklist.
                            if (itemCode == desiredCode && isBlacklist)
                                return false;

                            // If this is a whitelist, we need to set isValid to true (since isValid would be false)
                            if (itemCode == desiredCode && !isBlacklist)
                            {
                                isValid = true;
                                break;
                            }
                        }

                        if (!isValid)
                            return false;
                    }

                    if (attrs.HasAttribute(ItemCoupon.APPLIED_SHOPS))
                    {
                        ITreeAttribute shopList = attrs.GetTreeAttribute(ItemCoupon.APPLIED_SHOPS);
                        int length = attrs.GetInt(ItemCoupon.APPLIED_SHOPS_COUNT);
                        bool isValid = false;
                        for (int i = 0; i < length; i++)
                        {
                            long shopId = shopList.GetLong("ID-" + i);
                            if (register.ID == shopId)
                            {
                                isValid = true;
                                break;
                            }
                        }

                        if (!isValid)
                            return false;
                    }
                    else if (attrs.GetString(ItemCoupon.OWNER) != register.OwnerUID)
                    {
                        return false;
                    }

                    return true;
                }

            }

            return false;
        }
    }

}
