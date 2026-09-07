using Commercially.Common.Interfaces;
using Commercially.Common.Inventory;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Inventory.Impl
{
    public class RegisterInventory : InventoryBase, ILateInitInventory, ITradePassProvider
    {
        public VinconCloningSlot TradePass {  get; protected set; }
        public ItemSlot[] CurrencySlots { get; protected set; }
        public ItemSlot[] CouponSlots { get; protected set; }

        public RegisterInventory(ICoreAPI api) : base("-", api)
        {
        }

        public override ItemSlot this[int slotId] {
            get {
                if (slotId == 0)
                    return TradePass;
                slotId--;

                if (slotId < CurrencySlots.Length)
                {
                    return CurrencySlots[slotId];
                }
                slotId -= CurrencySlots.Length;

                return CouponSlots[slotId];
            }
            set {
                if (slotId == 0)
                    TradePass = (VinconCloningSlot) value; //TODO: Workaround to enforce this? No one should really be setting the slots explicitly anyway
                slotId--;

                if (slotId < CurrencySlots.Length)
                {
                    CurrencySlots[slotId] = value;
                }
                slotId -= CurrencySlots.Length;

                CouponSlots[slotId] = value;
            }
        }

        public override int Count => 1 + (CurrencySlots?.Length ?? 0) + (CouponSlots?.Length ?? 0);

        public bool IsSlotsInitialized => CurrencySlots != null && CouponSlots != null && TradePass != null;

        //TODO: Not effecient due to multiple nested trees, but IDGAF right now.
        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            ITreeAttribute currencyTree = tree.GetOrAddTreeAttribute("currencySlots");
            CurrencySlots = SlotsFromTreeAttributes(currencyTree, CurrencySlots);

            ITreeAttribute couponTree = tree.GetOrAddTreeAttribute("couponSlots");
            CouponSlots = SlotsFromTreeAttributes(couponTree, CouponSlots);

            if (TradePass == null) TradePass = new VinconCloningSlot(this);
            TradePass.Itemstack = tree.GetItemstack("tradePass");
            if (Api?.World != null)
            {
                TradePass.Itemstack?.ResolveBlockOrItem(Api.World);
            }

           
        }

        //TODO: Not effecient due to multiple nested trees, but IDGAF right now.
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            ITreeAttribute currencyTree = tree.GetOrAddTreeAttribute("currencySlots");
            SlotsToTreeAttributes(CurrencySlots, currencyTree);

            ITreeAttribute couponTree = tree.GetOrAddTreeAttribute("couponSlots");
            SlotsToTreeAttributes(CouponSlots, couponTree);

            if (TradePass == null) TradePass = new VinconCloningSlot(this);
            tree.SetItemstack("tradePass", TradePass.Itemstack);
        }

        public void InitializeFromProperties(JsonObject properties, string className, string instanceID, ICoreAPI api)
        {
            int numSlots = properties["numSlots"].AsInt(30);
            int numCouponSlots = properties["numCouponSlots"].AsInt(10);

            this.instanceID = instanceID;
            this.className = className;
            Api = api;

            // SlotsFromTreeAttributes actually instantiates "slots" so to avoid overwriting the array, check if they are already set
            if (!IsSlotsInitialized)
            {
                TradePass = new VinconCloningSlot(this);
                CurrencySlots = new ItemSlot[numSlots];
                CouponSlots = new ItemSlot[numCouponSlots];

                for (int i = 0; i < numSlots; i++)
                {
                    CurrencySlots[i] = new ItemSlot(this);
                }

                for (int i = 0; i < numCouponSlots; i++)
                {
                    CouponSlots[i] = new ItemSlot(this);
                }
            }

            //If we already have the slot array, make sure it is atleast greater than numSlots. I don't expect people to be resizing inventories mid-playthrough, but better safe than sorry
            else
            {
                if (CurrencySlots.Length < numSlots)
                {
                    CurrencySlots = ResizeSlots(CurrencySlots, numSlots);
                }

                if (CouponSlots.Length < numSlots)
                {
                    CouponSlots = ResizeSlots(CouponSlots, numSlots);
                }
            }

            // This has bit us in the but more times than I can count, so lets triple check that InvNetworkUtil is initialized
            if (api != null && InvNetworkUtil == null)
            {
                InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
            }

            //Lastly, be sure to resolve the collectible IDs into actual blocks/items 
            AfterBlocksLoaded(api.World);
        }

        public ItemStack GetTradePass()
        {
            return TradePass?.Itemstack?.Clone();
        }


        private ItemSlot[] ResizeSlots(ItemSlot[] existing, int amount)
        {
            ItemSlot[] newSlots = new ItemSlot[amount];
            for (int i = 0; i < amount; i++)
            {
                if (i < existing.Length)
                {
                    newSlots[i] = existing[i];
                } else
                {
                    newSlots[i] = new ItemSlot(this);
                }

            }

            return newSlots;
        }
    }
}
