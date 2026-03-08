namespace Vinconomy.Config
{
    public class VinconIntegrationConfig
    {
        public bool IsEnabled { get; set; } = false;
        public int MessageIntervalMinutes { get; set; } = 10;
        public string PurchasesWebhook { get; set; }

        public string EmbedImageURL { get; set; }



    }

}
