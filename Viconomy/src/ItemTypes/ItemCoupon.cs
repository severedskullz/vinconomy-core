using System.Text;
using Viconomy.Registry;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.ItemTypes
{
    public class ItemCoupon : Item
    {

        public override string GetHeldItemName(ItemStack itemStack)
        {
            return itemStack.Attributes.GetString("CouponName", Lang.Get("vinconomy:item-coupon"));
        }


        private void GiveBundledItems(ItemSlot slot, IPlayer player)
        {
            ITreeAttribute treeAttr = slot.Itemstack.Attributes;
            ITreeAttribute contents = (TreeAttribute)treeAttr.GetTreeAttribute("Contents");

            if (contents != null)
            {
                EntityAgent ent = player.Entity;

                for (int i = 0; i < 6; i++)
                {
                    ItemStack blockStack = contents.GetItemstack($"Item{i}");
                    if (blockStack != null)
                    {
                        //Note: These console Writes were here because the spawning/giving of items was inconsistent. Sometimes it gave me nothing, some times it gave me the stack size AND spawned more.
                        // If it ever happens again, this is where the problem was. Still no idea why/how. Hunch is the actual attribute stack size was getting modified by TryGiveItemstack before cloned it,
                        // thus all subsequent calls gave me 0?
                        //Console.WriteLine($"From Item: {blockStack.StackSize}");
                        ItemStack newStack = blockStack.Clone();
                        newStack.ResolveBlockOrItem(ent.World);
                        //Console.WriteLine($"From New Item: {newStack.StackSize}");
                        player.InventoryManager.TryGiveItemstack(newStack, true);
                        //Console.WriteLine($"After Item: {blockStack.StackSize}");
                        //Console.WriteLine($"After New Item: {newStack.StackSize}");
                        if (newStack.StackSize > 0)
                        {
                            ent.World.SpawnItemEntity(newStack, ent.SidedPos.XYZ.Add(0.0f, 0.5f, 0.0f), null);
                        }

                    }
                }
            }

            slot.TakeOut(1);
            slot.MarkDirty();
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ITreeAttribute tree = inSlot.Itemstack.Attributes;

            if (!tree.HasAttribute("CouponName"))
            {
                dsc.AppendLine("This item seems to be misconfigured. How did you get a hold of this?");
                return;
            }
            int value = tree.GetInt("CouponValue");
            string discountType = tree.GetString("DiscountType");
            string bonusType = tree.GetString("BonusType");
            bool consume = tree.GetBool("ConsumeCoupon");

            dsc.AppendLine($"A {consume} coupon for {value} {discountType} {bonusType}");
            dsc.AppendLine();


            VinconomyCoreSystem modSystem = world.Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            ShopRegistry registry = modSystem.GetRegistry();

            dsc.AppendLine("Valid Shops:");
            if (tree.HasAttribute("AppliedShops"))
            {
                int appliedShopsLength = tree.GetInt("AppliedShopLength");
                ITreeAttribute shopTree = tree.GetOrAddTreeAttribute("AppliedShops");
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

                }

            }
            else
            {
                dsc.AppendLine("All Shops owned by " + tree.GetString("CouponOwnerName"));
            }


            if (tree.HasAttribute("ItemList"))
            {
                bool isBlacklist = tree.GetBool("ItemBlacklist");
                if (isBlacklist)
                    dsc.AppendLine("Item Blacklist");
                else
                    dsc.AppendLine("Item Whitelist");

                ITreeAttribute itemList = tree.GetOrAddTreeAttribute("ItemList");
                ItemStack[] items = new ItemStack[tree.GetInt("AppliedShopLength")];
                for (int i = 0; i < items.Length; i++)
                {
                    ItemStack itemStack = null;

                    Item item = world.GetItem(itemList.GetString(i.ToString()));
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
                    tree.GetInt("ItemListLength");
                }

            }
            dsc.AppendLine();

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }
    }
}
