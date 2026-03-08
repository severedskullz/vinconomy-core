using Vinconomy.Util;

namespace Viconomy.Network.JavaApi.TradeNetwork
{
    public class TradeNetworkNode
    {
        public string serverName;
        public string guid;
        public string apiKey;
        public string hostname;
        public string ip;
        public string owner;
        public int udpPort;
        public TradeNetwork network;

        public static TradeNetworkNode FromJson(string json)
        {
            // Just incase I need to do it manually in the future for some reason, save myself from Refactoring.
            return VinUtils.DeserializeFromJson<TradeNetworkNode>(json);
        }
    }
}
