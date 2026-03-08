using System.IO;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;

namespace Vinconomy.Network.JavaApi
{
    public class Product
    {
        public int StallSlot { get; set; }
        public ItemStack Item { get; set; }
        public int NumItemsPerPurchase { get; set; }
        public ItemStack Currency { get; set; }

        public Product(int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            StallSlot = stallSlot;
            Item = product;
            NumItemsPerPurchase = numItemsPerPurchase;
            Currency = currency;
        }

        public JsonObject ToJsonString()
        {
            MemoryStream productStream = new MemoryStream();
            BinaryWriter productWriter = new BinaryWriter(productStream);


            JsonObject json = new JsonObject()
            {
                { "stallSlot", StallSlot },
            };
            if (Item != null)
            {

                json.Add("productName", Item.GetName());
                json.Add("productCode", Item.Collectible.Code.ToString());
                json.Add("productQuantity", NumItemsPerPurchase);
                json.Add("totalStock", Item.StackSize);
                Item.Attributes.ToBytes(productWriter);
                byte[] productData = productStream.ToArray();
                json.Add("productAttributes", System.Convert.ToBase64String(productData));
            }

            if (Currency != null)
            {
                json.Add("currencyName", Currency.GetName());
                json.Add("currencyCode", Currency.Collectible.Code.ToString());
                json.Add("currencyQuantity", Currency.StackSize);

                Currency.Attributes.ToBytes(productWriter);
                byte[] currencyData = productStream.ToArray();
                json.Add("currencyAttributes", System.Convert.ToBase64String(currencyData));

            }

            return json;
        }
    }
}