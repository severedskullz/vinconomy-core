using System;
using System.Collections.Generic;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.Inventory.Impl
{
    public class ViconRegisterInventory : InventoryGeneric
    {
        ItemSlot[] CouponSlots;
        ViconCurrencySlot TradePass;

        public int CurrencySlotCount => slots.Length;
        public int CouponSlotCount => CouponSlots.Length;
        public ViconRegisterInventory(int quantitySlots, int couponSlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) : base(quantitySlots, invId, api, onNewSlot)
        {
            CouponSlots = GenEmptySlots(couponSlots);
            TradePass = new ViconCurrencySlot(this);
        }

        public override ItemSlot this[int slotId] {
            get
            {
                if (slotId < 0 || slotId >= Count)
                {
                    throw new ArgumentOutOfRangeException("slotId");
                }
                if (slotId == 0)
                    return TradePass;
                else if (slotId <= slots.Length)
                    // Remove 1 for TradePass;
                    return slots[slotId - 1];
                else
                    // Remove 1 for TradePass, and slots.Length
                    return CouponSlots[slotId - 1 - slots.Length];
            }
            set
            {
                if (slotId < 0 || slotId >= Count)
                {
                    throw new ArgumentOutOfRangeException("slotId");
                }

                if (value == null) throw new ArgumentException("value");

                if (slotId == 0)
                    //Shouldnt ever be setting these values explicitly, but whatever...
                    TradePass = (ViconCurrencySlot) value;
                else if (slotId <= slots.Length)
                    // Remove 1 for TradePass;
                    slots[slotId-1] = value;
                else
                    // Remove 1 for TradePass, and slots.Length
                    CouponSlots[slotId-1-slots.Length] = value;

            }
        }

        public override int Count => 1 + slots.Length + CouponSlots.Length;


        //Generic impl to add to the specified array instead of hardcoded to "slots"
        public void AddNewSlots(ItemSlot[] slots, int amount)
        {
            while (amount-- > 0)
            {
                slots = slots.Append(NewSlot(slots.Length));
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            int num = slots.Length;
            slots = SlotsFromTreeAttributes(tree, "qslots", "slots", slots);
            int amount = num - slots.Length;
            AddNewSlots(slots, amount);

            num = slots.Length;
            CouponSlots = SlotsFromTreeAttributes(tree, "cqslots", "cslots", CouponSlots);
            amount = num - slots.Length;
            AddNewSlots(CouponSlots, amount);

            ItemStack tradepass = tree.GetItemstack("tradepass");
            if (Api?.World != null)
            {
                tradepass?.ResolveBlockOrItem(Api.World);
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("qslots", slots.Length);
            TreeAttribute currency = new TreeAttribute();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].Itemstack != null)
                {
                    currency.SetItemstack(i.ToString() ?? "", slots[i].Itemstack.Clone());
                }
            }

            tree["slots"] = currency;

            tree.SetInt("cqslots", CouponSlots.Length);
            TreeAttribute coupons = new TreeAttribute();
            for (int i = 0; i < CouponSlots.Length; i++)
            {
                if (CouponSlots[i].Itemstack != null)
                {
                    coupons.SetItemstack(i.ToString() ?? "", CouponSlots[i].Itemstack.Clone());
                }
            }

            tree["cslots"] = coupons;
            if (TradePass.Itemstack != null)
            {
                tree.SetItemstack("tradepass", TradePass.Itemstack.Clone());
            }
        }

        // For backwards compatability sake, split up slots from older "generic" impl with "qslots" and "slots" keys so players dont lose their items.
        public virtual ItemSlot[] SlotsFromTreeAttributes(ITreeAttribute tree, string quantityKey, string slotKey,  ItemSlot[] slots = null, List<ItemSlot> modifiedSlots = null)
        {
            if (tree == null)
            {
                return slots;
            }

            if (slots == null)
            {
                slots = new ItemSlot[tree.GetInt(quantityKey)];
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i] = NewSlot(i);
                }
            }

            for (int j = 0; j < slots.Length; j++)
            {
                ItemStack itemStack = tree.GetTreeAttribute(slotKey)?.GetItemstack(j.ToString() ?? "");
                slots[j].Itemstack = itemStack;
                if (Api?.World == null)
                {
                    continue;
                }

                itemStack?.ResolveBlockOrItem(Api.World);
                if (modifiedSlots != null)
                {
                    ItemStack itemstack = slots[j].Itemstack;
                    bool num = itemStack != null && !itemStack.Equals(Api.World, itemstack);
                    bool flag = itemstack != null && !itemstack.Equals(Api.World, itemStack);
                    if (num || flag)
                    {
                        modifiedSlots.Add(slots[j]);
                    }
                }
            }

            return slots;
        }

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return null;
        }

        public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            return null;
        }
    }
}
