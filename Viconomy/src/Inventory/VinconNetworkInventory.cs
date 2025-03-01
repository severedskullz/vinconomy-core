using System;
using System.Collections.Generic;
using Viconomy.Delegates;
using Viconomy.Network.Api;
using Viconomy.TradeNetwork.Api;
using Viconomy.Util;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.Inventory
{
    public class VinconNetworkInventory : InventoryBase
    {
        Dictionary<string, VinconNetworkItemSlot> KeyedSlots = new Dictionary<string, VinconNetworkItemSlot>();
        VinconNetworkItemSlot[] Slots;
        public VinconNetworkInventory(TradeNetworkShop shop, ICoreAPI api) : base(shop.NodeId + shop.Id, api)
        {
            Slots = new VinconNetworkItemSlot[shop.Products.Count];
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
            foreach (ShopProduct product in shop.Products)
            {
                VinconNetworkItemSlot slot = new VinconNetworkItemSlot(this);

                ItemStack productStack = VinUtils.DeserializeProduct(Api, product.ProductCode, Math.Clamp(product.ProductQuantity, 0, 999), product.ProductAttributes);
                ItemStack currencyStack = VinUtils.DeserializeProduct(Api, product.CurrencyCode, product.CurrencyQuantity, product.CurrencyAttributes);

                slot.Product = productStack;
                slot.Currency = currencyStack;
                if (productStack != null) { 
                    slot.Itemstack = slot.Product.Clone();
                    slot.Itemstack.StackSize = product.TotalStock;
                }
                slot.X = product.Id.X;
                slot.Y = product.Id.Y;
                slot.Z = product.Id.Z;
                slot.StallSlot = product.Id.StallSlot;
                slot.TotalStock = product.TotalStock;
                KeyedSlots.Add(product.Id.ToKey(), slot);
                Slots[index] = slot;
                index++;
            }
        }

        public VinconNetworkItemSlot GetProductById(string key)
        {
            KeyedSlots.TryGetValue(key, out VinconNetworkItemSlot product);
            return product;
        }

        public VinconNetworkItemSlot GetProductById(int x, int y, int z, int stallSlot)
        {
            string key = $"{x}-{y}-{z}-{stallSlot}";
            return GetProductById(key);
        }
    }
}
