using Viconomy.BlockEntities;
using Viconomy.Inventory.Slots;
using Viconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory.Impl
{
    public class ViconMealInventory : ViconomyBaseInventory<ItemSlot>
    {
        public ViconMealInventory(BEVinconBase stall, string inventoryID, ICoreAPI api, int numStalls, int numStacksPerStall) : base(stall, inventoryID, api, numStalls, numStacksPerStall)
        {
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
            return new ItemSlot(this);
        }



        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            for (int i = 0; i < NumStalls; i++)
            {
                MealStallSlot stall = (MealStallSlot)StallSlots[i];
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
                MealStallSlot stall = (MealStallSlot)StallSlots[i];
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
                MealStallSlot stall = (MealStallSlot)StallSlots[i];
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

        protected override void InitializeStalls()
        {
            StallSlots = new MealStallSlot[NumStalls];
            for (int i = 0; i < NumStalls; i++)
            {
                StallSlots[i] = new MealStallSlot(this, i, NumStacksPerStall);
            }
        }
    }
}
