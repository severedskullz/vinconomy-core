using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Inventory.Impl
{
    public class GachaShopInventory : VinconBaseInventory
    {
        // How many slots can be provided for each "Product" gacha ball.
        private int ContentSlotsPerStall;

        public bool IsCountBasedRandomizer;

        public GachaShopInventory(BlockEntity entity, ICoreAPI api) : base(entity, api)
        {
        }

        public override void InitializeInternalSlots()
        {
            if (!IsInternalSlotsInitialized)
            {
                InternalSlots = new ItemSlot[1];
                InternalSlots[0] = new CurrencySlot(this);
            }
        }


        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            int numStalls = tree.GetInt("numStalls");
            IsCountBasedRandomizer = tree.GetBool("isCountBasedRandomizer", false);
            if (!IsSlotsInitialized)
            {

                SlotsPerStall = tree.GetInt("numSlotsPerStall", 20);
                ContentSlotsPerStall = tree.GetInt("contentsPerStall", 5);
                StallType = tree.GetString("stallType", "GenericStallSlot");
                Type stallType = GetStallType(StallType);

                StallSlots = new GachaStallSlot[numStalls];
                for (int i = 0; i < numStalls; i++)
                {
                    GachaStallSlot stall = new GachaStallSlot(this, i, ContentSlotsPerStall, SlotsPerStall);
                    ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                    stall.PreInitialize(this, i);
                    stall.FromTreeAttributes(stallTree);
                    StallSlots[i] = stall;
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
            else
            {
                for (int i = 0; i < numStalls; i++)
                {
                    GachaStallSlot stall = GetStall<GachaStallSlot>(i);
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

        public int GetTotalWeights()
        {
            int totalWeight = 0;
            foreach (GachaStallSlot stall in StallSlots)
            {
                int quanitty = stall.GetTotalProductAvailable();
                if (quanitty > 0)
                    totalWeight += stall.GetStallWeight();
            }
            return Math.Max(1, totalWeight); // Prevent divide by 0 by having the lowest possible weight as 1
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("numStalls", StallSlots.Length);
            tree.SetString("stallType", StallType);
            tree.SetInt("numSlotsPerStall", SlotsPerStall);
            tree.SetInt("contentsPerStall", ContentSlotsPerStall);
            tree.SetBool("isCountBasedRandomizer", IsCountBasedRandomizer);

            for (int i = 0; i < StallSlots.Length; i++)
            {
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                StallSlots[i].ToTreeAttributes(stallTree);
            }

            ITreeAttribute internalSlots = tree.GetOrAddTreeAttribute("internalSlots");
            //internalSlots.SetInt("numSlots", InternalSlots.Length);
            for (int i = 0; i < InternalSlots.Length; i++)
            {
                internalSlots.SetItemstack("slot" + i, InternalSlots[i].Itemstack);
                internalSlots.SetString("slot" + i + "-name", InternalSlots[i].Itemstack?.ToString());
            }
        }

        public override void InitializeStallSlots(JsonObject properties)
        {
            StallType = typeof(GachaStallSlot).Name; //TODO: Might need some sort of "lookup" of the class to registred name. This should work for now though

            int numStalls = properties["numStalls"].AsInt(6);
            SlotsPerStall = properties["numSlotsPerStall"].AsInt(20);
            ContentSlotsPerStall = properties["contentsPerStall"].AsInt(5);

            if (!IsSlotsInitialized)
            {
                StallSlots = new GachaStallSlot[numStalls];
                for (int i = 0; i < numStalls; i++)
                {
                    GachaStallSlot instance = new GachaStallSlot(this, i, ContentSlotsPerStall, SlotsPerStall);
                    StallSlots[i] = instance;
                }
            }
        }

        /*
        public override void OnStockModified(ItemSlot slot)
        {
            if (Api.Side == EnumAppSide.Client) return;

            if (slot is IStockUpdater stallProductSlot)
            {
                int stallSlot = stallProductSlot.GetStallIndex();
                GachaStallSlot stall = this.GetStall<GachaStallSlot>(stallSlot);

                stall.RegenProduct();
                ItemStack product = stall.Product?.Itemstack?.Clone();
                ItemStack currency = stall.Currency?.Itemstack?.Clone();
                int stockCount = stall.GetProducts().TotalCount;

                UpdateStockForSlot(StallComponent, stallSlot, product, stockCount, currency);

            }
        }
        */
    }
}
