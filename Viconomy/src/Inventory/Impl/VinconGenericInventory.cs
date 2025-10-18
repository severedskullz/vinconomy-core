using System.Collections.Generic;
using Viconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Inventory.Impl
{
    public class VinconGenericInventory : InventoryGeneric
    {
        public VinconGenericInventory(ICoreAPI api) : base(api)
        {
        }

        public VinconGenericInventory(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) : base(quantitySlots, invId, api, onNewSlot)
        {
        }

        public VinconGenericInventory(int quantitySlots, string className, string instanceId, ICoreAPI api, NewSlotDelegate onNewSlot = null) : base(quantitySlots, className, instanceId, api, onNewSlot)
        {
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
    }
}
