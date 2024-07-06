using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory
{
    public class ViconomySculptureInventory : InventoryBase, ISlotProvider
    {
        BEVinconBase stall;

        private ItemSlot[] slots;

        public ItemSlot[] Slots => slots;

        public override int Count => slots.Length;

        public override ItemSlot this[int slotId] { get => slots[slotId]; set => slots[slotId] = value; }

        ViconomyCoreSystem modSystem;

        public ViconomySculptureInventory(int numSlots, string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {

            this.slots = new ItemSlot[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                this.slots[i] = NewSlot(i);
            }

        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            modSystem = Api.ModLoader.GetModSystem<ViconomyCoreSystem>();
        }

        protected override ItemSlot NewSlot(int id)
        {
            if (id == 0)
                return new ViconCurrencySlot(this);
            else
                return new ViconSculptureBlockSlot(this, id);
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            ViconConfig config = modSystem.Config;
            if ( (config != null && config.FoodDecaysInShops) && (stall != null && !stall.IsAdminShop))
            {
                return base.GetDefaultTransitionSpeedMul(transType) * modSystem.Config.StallPerishRate; 
            } else
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

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            this.slots = this.SlotsFromTreeAttributes(tree, slots, null);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.SlotsToTreeAttributes(this.slots, tree);
        }
    }
}
