using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory
{
    public class ViconomyTellerInventory : InventoryBase, ISlotProvider
    {
        BEVinconTeller stall;
        VinconomyCoreSystem modSystem;
        private ItemSlot[] slots;

        public override int Count { get { return this.slots.Length; } }

        public ItemSlot[] Slots => slots;

        public override ItemSlot this[int slotId]
        {
            get { return slots[slotId]; }
            set { slots[slotId] = value; }
        }


        public ViconomyTellerInventory(BEVinconTeller stall, string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            this.stall = stall;
            int numSlots = 5;
            int totalSlots = 2 * numSlots;
            this.slots = new ItemSlot[totalSlots];
            for (int i = 0; i < totalSlots; i++)
            {
                this.slots[i] = NewSlot(i);
            }
        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            
        }

        protected override ItemSlot NewSlot(int id)
        {
            return new ViconCurrencySlot(this);
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            return 0;
        }

        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
          
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
