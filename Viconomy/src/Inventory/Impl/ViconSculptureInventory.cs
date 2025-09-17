using System.Collections.Generic;
using Viconomy.BlockEntities;
using Viconomy.Config;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory.Impl
{
    public class ViconSculptureInventory : InventoryBase, ISlotProvider
    {
        BEVinconBase stall;

        private ItemSlot[] slots;

        public ItemSlot[] Slots => slots;

        public override int Count => slots.Length;

        public override ItemSlot this[int slotId] { get => slots[slotId]; set => slots[slotId] = value; }

        VinconomyCoreSystem modSystem;

        public ViconSculptureInventory(int numSlots, string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {

            slots = new ItemSlot[numSlots];
            for (int i = 0; i < numSlots; i++)
            {
                slots[i] = NewSlot(i);
            }

        }

        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            base.LateInitialize(inventoryID, api);
            modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();
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
            if (config != null && config.FoodDecaysInShops && stall != null && !stall.IsAdminShop)
            {
                return base.GetDefaultTransitionSpeedMul(transType) * modSystem.Config.StallPerishRate;
            }
            else
            {
                return 0;
            }

        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree, slots, null);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }

        
    }
}
