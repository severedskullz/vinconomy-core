using Commercially.Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Inventory.Impl
{
    public class PurchaseStallShopInventory : VinconBaseInventory
    {
        public bool IsDiscardProduct;

        public PurchaseStallShopInventory(BlockEntity entity, ICoreAPI api) : base(entity, api)
        {
        }

        public override void InitializeStallSlots(JsonObject properties)
        {
            int numStalls = properties["numStalls"].AsInt(2);
            int currencySlotsPerStall = properties["currencySlotsPerStall"].AsInt(20);
            int purchasedStockPerStall = properties["stockSlotsPerStall"].AsInt(30);

            StallType = typeof(PurchaseStallSlot).Name; //TODO: Might need some sort of "lookup" of the class to registred name. This should work for now though

            if (!IsSlotsInitialized)
            {
                StallSlots = new PurchaseStallSlot[numStalls];
                for (int i = 0; i < numStalls; i++)
                {
                    StallSlots[i] = new PurchaseStallSlot(this, i, currencySlotsPerStall, purchasedStockPerStall);
                }
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            int numStalls = tree.GetInt("numStalls");

            if (!IsSlotsInitialized)
            {
                SlotsPerStall = tree.GetInt("numSlotsPerStall", 9);
                StallType = typeof(PurchaseStallSlot).Name; //TODO: Might need some sort of "lookup" of the class to registred name. This should work for now though

                StallSlots = new PurchaseStallSlot[numStalls];
                for (int i = 0; i < numStalls; i++)
                {
                    PurchaseStallSlot stall = new PurchaseStallSlot(this, i);
                    ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                    stall.PreInitialize(this, i);
                    stall.FromTreeAttributes(stallTree);
                    StallSlots[i] = stall;
                }

                //TODO: How to handle resizing of internal slots? Can we even support this?
                ITreeAttribute internalSlots = tree.GetOrAddTreeAttribute("internalSlots");
                //int numInternalSlots = internalSlots.GetInt("numSlots",0);
                for (int i = 0; i < InternalSlots.Length; i++)
                {
                    ItemStack stack = internalSlots.GetItemstack("slot" + i);
                    InternalSlots[i].Itemstack = stack;

                    if (Api?.World == null)
                    {
                        continue;
                    }

                    stack?.ResolveBlockOrItem(Api.World);
                }
            }
            else
            {
                for (int i = 0; i < numStalls; i++)
                {
                    BaseStallSlot stall = GetStall(i);
                    ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                    stall.FromTreeAttributes(stallTree);
                }

                ITreeAttribute internalSlots = tree.GetOrAddTreeAttribute("internalSlots");
                for (int i = 0; i < InternalSlots.Length; i++)
                {
                    ItemStack stack = internalSlots.GetItemstack("slot" + i);
                    InternalSlots[i].Itemstack = stack;

                    if (Api?.World == null)
                    {
                        continue;
                    }

                    stack?.ResolveBlockOrItem(Api.World);
                }
            }

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("numStalls", StallSlots.Length);

            for (int i = 0; i < StallSlots.Length; i++)
            {
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                StallSlots[i].ToTreeAttributes(stallTree);
            }

            ITreeAttribute internalSlots = tree.GetOrAddTreeAttribute("internalSlots");
            for (int i = 0; i < InternalSlots.Length; i++)
            {
                internalSlots.SetItemstack("slot" + i, InternalSlots[i].Itemstack);
                internalSlots.SetString("slot" + i + "-name", InternalSlots[i].Itemstack?.ToString());
            }
        }
    }
}
