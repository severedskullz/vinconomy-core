using ProtoBuf;
using System.Collections.Generic;

namespace Viconomy.TradeNetwork
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class TradeNetworkShop
    {
        public int id { get; set; }
        public string nodeId { get; set; }
        public string name { get; set; }
        public string serverName { get; set; }
        public string owner {  get; set; }
        public List<TradeNetworkProduct> products { get; set; } = new List<TradeNetworkProduct>();
        public long lastUpdatedTimestamp { get; set; }

    }
}