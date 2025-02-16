using System;
using System.IO;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Viconomy.Network.Api
{
    public class TradeNetworkProductUpdate
    {
        public int StallSlot { get; set; }
        public ItemStack Product {  get; set; }
        public int NumItemsPerPurchase { get; set; }
        public ItemStack Currency { get; set; }

        public TradeNetworkProductUpdate(int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            StallSlot = stallSlot;
            Product = product;
            NumItemsPerPurchase = numItemsPerPurchase;
            Currency = currency;
        }

        public JsonObject ToJsonString()
        {
            MemoryStream productStream = new MemoryStream();
            BinaryWriter productWriter = new BinaryWriter(productStream);
            Product.Attributes.ToBytes(productWriter);
            byte[] productData = productStream.ToArray();

            Currency.Attributes.ToBytes(productWriter);
            byte[] currencyData = productStream.ToArray();

            JsonObject json = new JsonObject()
            {
                { "stallSlot", StallSlot },
                { "productName", Product.GetName() },
                { "productCode", Product.Collectible.Code.ToString() },
                { "productAttributes", productData.ToString() },
                { "productQuantity", NumItemsPerPurchase },
                { "totalStock", Product.StackSize },
                { "currencyName", Currency.GetName() },
                { "currencyCode", Currency.Collectible.Code.ToString() },
                { "currencyAttributes", currencyData.ToString() },
                { "currencyQuantity", Currency.StackSize },
                { "totalStock", Product.StackSize },
            };



            return json;
        }
    }
}