using System;
using Viconomy.Delegates;
using Viconomy.TradeNetwork;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory
{
    public class VinconNetworkInventory : InventoryBase
    {

        VinconNetworkItemSlot[] Slots;
        public VinconNetworkInventory(TradeNetworkShop shop, ICoreAPI api) : base(shop.nodeId + shop.id, api)
        {
            Slots = new VinconNetworkItemSlot[shop.products.Count];
            Init(shop);
        }

        public event OnTradeSelectedDelegate OnTradeSelected;

        public override ItemSlot this[int slotId] { get => Slots[slotId]; set => Slots[slotId] = value as VinconNetworkItemSlot; }

        public override int Count => Slots.Length;

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {

        }

        private ItemStack ResolveBlockOrItem(string code, int size)
        {
            AssetLocation location = new AssetLocation(code);
            Item item = Api.World.GetItem(location);
            if (item != null)
            {
                return new ItemStack(item, size);
            }

            Block block = Api.World.GetBlock(location);
            if (block != null)
            {
                return new ItemStack(block, size);
            }
            return null;
        }

        public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            VinconNetworkItemSlot slot = Slots[slotId];
            OnTradeSelected.Invoke(slot);

            return null;
        }

        public void Init(TradeNetworkShop shop)
        {
            int index = 0;
            //Add our Product and Currency to each inventory. Catch JSON errors on attributes, cuz Quotes in descriptions got me once already
            foreach (TradeNetworkProduct product in shop.products)
            {
                VinconNetworkItemSlot slot = new VinconNetworkItemSlot(this);

                ItemStack productStack = ResolveBlockOrItem(product.ProductCode, Math.Clamp(product.ProductQuantity, 0, 999));
                try
                {
                    if (product.ProductAttributes != null)
                    {
                        TreeAttribute attr = new TreeAttribute();
                        attr.FromBytes(product.ProductAttributes);

                        // Remove transition state from any food items. SQL entries are the last time it was inserted and isnt updated
                        attr.RemoveAttribute("transitionstate");

                        //JsonObject productAttr = JsonObject.FromJson(product.ProductAttributes);
                        productStack.Attributes = attr;//(ITreeAttribute)ToAttribute(productAttr.Token);

                    }
                }
                catch (Exception ex) { }


                slot.Product = productStack;
                slot.Itemstack = slot.Product.Clone();
                slot.Itemstack.StackSize = product.TotalStock;
                slot.X = product.X;
                slot.Y = product.Y;
                slot.Z = product.Z;
                slot.StallSlot = product.StallSlot;
                slot.TotalStock = product.TotalStock;

                ItemStack currencyStack = ResolveBlockOrItem(product.CurrencyCode, product.currencyQuantity);
                if (product.CurrencyAttributes != null)
                {
                    TreeAttribute attr = new TreeAttribute();

                    // Remove transition state from any food items. SQL entries are the last time it was inserted and isnt updated
                    attr.RemoveAttribute("transitionstate");

                    attr.FromBytes(product.CurrencyAttributes);
                    //JsonObject currencyAttr = JsonObject.FromJson(product.CurrencyAttributes);
                    currencyStack.Attributes = attr; // (ITreeAttribute)currencyAttr.ToAttribute();
                }
                slot.Currency = currencyStack;
                Slots[index] = slot;
                index++;
            }
        }
    }
}
