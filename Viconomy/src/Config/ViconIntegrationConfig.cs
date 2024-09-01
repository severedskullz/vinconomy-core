using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Config
{
    public class VinconIntegrationConfig
    {
        public bool IsEnabled { get; set; }
        public int MessageIntervalMinutes { get; set; }
        public string GlobalWebhook { get; set; }
        public string PurchasesWebhook { get; set; }
        public string RestockWebhook { get; set; }


    }

}
