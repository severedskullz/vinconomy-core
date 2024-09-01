using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Viconomy.BlockEntities;
using Viconomy.Config;
using Viconomy.Registry;
using Viconomy.Trading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using static Viconomy.VinconomyDiscordIntegrationSystem;

namespace Viconomy
{

    public class VinconomyDiscordIntegrationSystem : ModSystem
    {
        private ICoreServerAPI _coreServerAPI;
        private VinconomyCoreSystem _coreSystem;
        private HttpClient httpClient;
        Dictionary<int, DiscordUpdate> queuedSales = new Dictionary<int, DiscordUpdate>();

        public VinconIntegrationConfig Config { get; internal set; }


        public override double ExecuteOrder() => 2;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartPre(ICoreAPI api)
        {
            this.Mod.Logger.Event("Start Pre Called");
            string filename = "vinconomy-integration.json";
            try
            {
                VinconIntegrationConfig config = api.LoadModConfig<VinconIntegrationConfig>(filename);
                if (config == null)
                {
                    config = ResetModConfig();
                    api.StoreModConfig(config, filename);
                }

                this.Config = config;
            }
            catch
            {
                Config = ResetModConfig();
                this.Mod.Logger.Error("Could not load Mod Integration Config for Vinconomy. Loading defaults instead. Check your config and ensure there are no errors.");
                //api.StoreModConfig<ViconConfig>(new ViconConfig(), filename);
            }

            base.StartPre(api);
        }

        public VinconIntegrationConfig ResetModConfig()
        {
            VinconIntegrationConfig config = new VinconIntegrationConfig();
            return config;
        }

        public override void Dispose()
        {
            base.Dispose();
            httpClient?.Dispose();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            _coreServerAPI = api;
            _coreSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            this.Mod.Logger.Event("Start Serverside Called");
            httpClient = new HttpClient();
            //httpClient.BaseAddress = new Uri("https://discord.com");
            PostAsync();
            //Task.Run(PostAsync);

            _coreSystem.OnPurchasedItem += EnquePurchase;
            _coreServerAPI.Event.Timer(PostAsync, 10);

        }

        private void EnquePurchase(TradeResult result, ItemStack product, ItemStack payment)
        {
            lock (this)
            {
                BEVinconRegister register = result.shopRegister;
                int shopId = register.ID;

                /*
                string customer = result.customer.PlayerName;
                string productName = product.GetName();
                int productAmount = product.StackSize;
                string paymentName = payment.GetName();
                int paymentAmount = payment.StackSize;
                */

                if (!queuedSales.ContainsKey(shopId))
                {
                    queuedSales.Add(shopId, new DiscordUpdate(_coreSystem.GetRegistry().GetShop(shopId)));
                }

                queuedSales[shopId].AddPurchase(result, product, payment);
            }
        }

        public void PostAsync()
        {
            this.Mod.Logger.Chat("Update HTTP Called with " + queuedSales.Count + " entries");
            if (queuedSales.Count == 0 || !Config.IsEnabled)
            {
                return;
            }
            List<Embed> embeds = new List<Embed>();
            Dictionary<int, DiscordUpdate> sales = null;

            lock (this)
            {
                // Transfer queedSales over to GC-eligible variable, before clearing out queuedSales for the next update.
                // Much easier to do this instead of putting *all* of the update logic inside of the Lock.
                // This way we can read/modify the dictionary without the main game thread from adding more stuff to it.
                sales = queuedSales;
                queuedSales = new Dictionary<int, DiscordUpdate>();
            }

            foreach (DiscordUpdate update in sales.Values)
            {
                Embed embed = new Embed();
                embed.title = update.shopName;
                if (update.url != null)
                {
                    embed.thumbnail = new Thumbnail(update.url);
                }

                StringBuilder sb = new StringBuilder();
                foreach (CustomerPurchaseList purchaseList in update.customers.Values)
                {

                    bool hasMultipleCustomers = purchaseList.purchases.Count > 1;
                    if (hasMultipleCustomers)
                    {
                        sb.AppendLine($"- {purchaseList.customerName} purchased:");
                        foreach (Purchase purchase in purchaseList.purchases.Values)
                        {
                            sb.AppendLine($" - {purchase.amountSold}x {purchase.productName} for {purchase.paymentCollected}x {purchase.paymentName}");
                        }
                    } else
                    {
                        Purchase purchase = purchaseList.purchases.FirstOrDefault().Value;
                        sb.AppendLine($"- {purchaseList.customerName} purchased {purchase.amountSold}x {purchase.productName} for {purchase.paymentCollected}x {purchase.paymentName}");
                    }
                   
                }
                embed.description = sb.ToString();
                embeds.Add(embed);
            }

            DiscordMessage message = new DiscordMessage();
            //message.content = "## :coin: The following shops have had sales recently:";
            message.embeds = embeds.Take(10).ToArray();
            string data = JsonSerializer.Serialize(message);
            StringContent jsonContent = new(data,Encoding.UTF8, "application/json");

            //Console.WriteLine(data);
            //HttpResponseMessage result = 
            httpClient.PostAsync(Config.GlobalWebhook, jsonContent);//.Result;
            //Console.WriteLine(result.Content);
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.Mod.Logger.Event("Start Clientside Called");
        }

        private class DiscordUpdate
        {


            internal int shopId { get; set; }
            internal string shopName { get; set; }
            internal string discordWebhook { get; set; }
            internal bool isAdminShop { get; set; }
            internal string url { get; set; }
            //internal Dictionary<string, PurchaseList>  productsPurchased {get;set;} = new Dictionary<string, PurchaseList>();
            internal Dictionary<string, CustomerPurchaseList> customers { get; set; } = new Dictionary<string, CustomerPurchaseList>();

            internal DiscordUpdate(ShopRegistration reg)
            {
                shopId = reg.ID;
                shopName = reg.Name;
                url = "https://mods.vintagestory.at/files/asset/8379/Vinconomy-2.0.jpg";
                //url = reg.imageUrl;
                //discordWebhook = reg.webhook;
            }

            internal void AddPurchase(TradeResult result, ItemStack product, ItemStack payment)
            {
                string customer = result.customer.PlayerName;
                if (!customers.ContainsKey(customer))
                {
                    customers[customer] = new CustomerPurchaseList() { customerName = customer};
                }

                customers[customer].AddPurchase(product, payment);
            }
        }

        internal class CustomerPurchaseList
        {
            internal int totalPurchases { get; set; }
            internal string customerName { get; set; }
            internal Dictionary<string, Purchase> purchases { get; set; } = new Dictionary<string, Purchase>();

            internal void AddPurchase(ItemStack product, ItemStack payment)
            {
                string key = product.Collectible.Code.GetName() + "-" + payment.Collectible.Code.GetName();
                if (!purchases.ContainsKey(key))
                {
                    purchases[key] = new Purchase() { productName = product.GetName(), paymentName = payment.GetName() };
                }

                Purchase purchase = purchases[key];
                purchase.amountSold += product.StackSize;
                purchase.paymentCollected += payment.StackSize;
                totalPurchases += product.StackSize;

            }
        }

        internal class Purchase
        {
            public string productName { get; set; }
            public string paymentName { get; set;  }
            internal int amountSold { get; set; }
            internal int paymentCollected { get; set; }
        }

        internal class DiscordMessage
        {
            public string content { get; set; }
            public string username { get; set; }
            public Embed[] embeds { get; set; }
        }

        internal class Thumbnail
        {
            public string url { get; set; }
            internal Thumbnail(string url)
            {
                this.url = url;
            }
        }

        internal class Embed
        {
            public string title { get; set; }
            public string description { get; set; }
            public int color { get; set; }
            public Thumbnail thumbnail { get; set; }
        }
    }
}
