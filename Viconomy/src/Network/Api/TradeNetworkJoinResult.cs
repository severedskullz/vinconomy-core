using Viconomy.Network.Api;
using Viconomy.Util;

namespace Viconomy.TradeNetwork
{
    internal class TradeNetworkJoinResult
    {
        public string apiKey;
        public string status;
        public string message;

        public static TradeNetworkJoinResult FromJson(string json)
        {
            // Just incase I need to do it manually in the future for some reason, save myself from Refactoring.
            return VinUtils.DeserializeFromJson<TradeNetworkJoinResult>(json);
        }
    }
}
