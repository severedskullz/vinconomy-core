using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Config;
using Viconomy.Inventory.StallSlots;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory.Impl
{
    public class ViconomyItemInventory : ViconomyBaseInventory<ViconItemSlot>, IStallSlotUpdater
    {


        public ViconomyItemInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(stall, inventoryID, api, numStalls, numStacksPerStall)
        {
            Stall = stall;
            NumStalls = numStalls;
            NumStacksPerStall = numStacksPerStall;


            ChiselDecoSlot = new ViconDecoBlockSlot(this, 0);
        }

        protected override void InitializeStalls()
        {
            StallSlots = new ItemStallSlot[NumStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new ItemStallSlot(this, i, NumStacksPerStall);
            }
        }

        /*
         * TODO: Figure out a way to determine if stock was ADDED and not removed. extractedStack == null if we added items, but if we add items then take it out, we would still get a notification if that was the only criteria.
         * Need to save the state of the inventory somehow. Dont want to poll the DB before we update it to figure that out - plus can lead to sync issues.
        public override void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
        {
            modSystem.Mod.Logger.Debug("A slot was modified. extracted stack is " + (extractedStack == null));
            base.DidModifyItemSlot(slot, extractedStack);
        }
        */

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();

        }



        protected override ItemSlot NewSlot(int slotId)
        {
            if (slotId == 0)
            {
                return new ViconDecoBlockSlot(this, 0);
            }

            int index = slotId - 1;
            int stallSlot = index / StallSlotSize;
            int itemSlot = slotId % StallSlotSize;
            if (itemSlot == NumStacksPerStall)
            {
                return new ViconCurrencySlot(this);
            }
            return new ViconItemSlot(this, stallSlot, itemSlot);
        }



        public void SetSlotFilter(int slot, Vintagestory.API.Common.Func<ItemSlot, bool> filter)
        {
            ViconItemSlot[] filteredSlots = StallSlots[slot].GetSlots();
            foreach (var itemSlot in filteredSlots)
            {
                itemSlot.setFilter(filter);
            }

        }





        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                ItemStallSlot stall = (ItemStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                StallSlots[i].itemsPerPurchase = stallTree.GetInt("purchaseQuantity", 1);

                stall.currency.Itemstack = stallTree.GetItemstack("currency");
                for (int j = 0; j < NumStacksPerStall; j++)
                {
                    ItemStack itemStack = stallTree.GetItemstack("slot" + j);
                    stall.slots[j].Itemstack = itemStack;

                }
            }
            ChiselDecoSlot.Itemstack = tree.GetItemstack("decoBlock");

            ResolveBlockItems();
        }

        private void ResolveBlockItems()
        {
            // Because Tyron wants us to try to resolve block items both BEFORE and AFTER the Api has be passed off into the Inventory with LateInitialize,
            // We need to make sure it actually is fucking SET before we try to resolve the blocks or items... See InventoryBase:SlotsFromTreeAtributes
            // Thanks Tyron!

            if (Api?.World == null)
            {
                return;
            }

            for (int i = 0; i < NumStalls; i++)
            {
                ItemStallSlot stall = (ItemStallSlot)StallSlots[i];
                stall.currency.Itemstack?.ResolveBlockOrItem(Api.World);
                for (int j = 0; j < NumStacksPerStall; j++)
                {
                    stall.slots[j].Itemstack?.ResolveBlockOrItem(Api.World);
                }
            }
            ChiselDecoSlot.Itemstack?.ResolveBlockOrItem(Api.World);

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            //base.SlotsToTreeAttributes(this.slots, tree);
            for (int i = 0; i < NumStalls; i++)
            {
                ItemStallSlot stall = (ItemStallSlot)StallSlots[i];
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                stallTree.SetInt("purchaseQuantity", StallSlots[i].itemsPerPurchase);
                if (stall.currency.Itemstack != null)
                {
                    stallTree.SetItemstack("currency", stall.currency.Itemstack.Clone());
                }
                for (int j = 0; j < NumStacksPerStall; j++)
                {
                    if (stall.slots[j].Itemstack != null)
                    {
                        stallTree.SetItemstack("slot" + j, stall.slots[j].Itemstack.Clone());
                    }
                }
            }
            if (ChiselDecoSlot.Itemstack != null)
            {
                tree.SetItemstack("decoBlock", ChiselDecoSlot.Itemstack.Clone());
            }
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            ViconConfig config = modSystem.Config;
            if (config != null && config.FoodDecaysInShops && Stall != null && !Stall.IsAdminShop)
            {
                return base.GetDefaultTransitionSpeedMul(transType) * modSystem.Config.StallPerishRate;
            }
            else
            {
                return 0;
            }

        }

        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            using IEnumerator<ItemSlot> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                ItemSlot current = enumerator.Current;
                if (current.Itemstack == null || current is ViconCurrencySlot)
                {
                    continue;
                }

                if (maxStackSize > 0)
                {
                    while (current.StackSize > 0)
                    {
                        ItemStack itemstack = current.TakeOut(GameMath.Clamp(current.StackSize, 1, maxStackSize));
                        Api.World.SpawnItemEntity(itemstack, pos);
                    }
                }
                else
                {
                    Api.World.SpawnItemEntity(current.Itemstack, pos);
                }

                current.Itemstack = null;
                current.MarkDirty();
            }
        }

        public int GetItemsPerPurchase(int stallSlot)
        {
            return StallSlots[stallSlot].itemsPerPurchase;
        }

        public void SetSlotBackground(int stallSlot, string background = null, string hexColor = null)
        {
            ItemSlot[] curSlots = GetSlotsForStallSlot(stallSlot);
            foreach (var slot in curSlots)
            {
                slot.BackgroundIcon = background;
                slot.HexBackgroundColor = hexColor;
            }
        }
    }
}