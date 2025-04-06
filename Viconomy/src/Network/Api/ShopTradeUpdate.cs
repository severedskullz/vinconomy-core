
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Viconomy.Network.Api
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ShopTradeUpdate
    {
        public long Id { get; set; }
        public int ShopId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int StallSlot { get; set; }

        public string Status { get; set; }
        public string RequestingNode { get; set; }
        public string OriginNode { get; set; }

        //TODO: Respect original product/currency in trade

        public string Name { get; set; }
        public string PlayerGuid { get; set; }

        public int Amount { get; set; }

        public string Created { get; set; }
        public string Modified { get; set; }

    }
}
