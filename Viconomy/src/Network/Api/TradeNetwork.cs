using Viconomy.Util;

namespace Viconomy.Network.Api
{
    public class TradeNetwork
    {
        private string name;
        //private ApiUser owner;
        private string description;
        private string networkAccessKey;
        private bool autoAcceptRequests;
        private bool visible;
        private bool moddedItemsAllowed;
        private string asyncType;


        public static TradeNetwork FromJson(string json)
        {
            // Just incase I need to do it manually in the future for some reason, save myself from Refactoring.
            return VinUtils.DeserializeFromJson<TradeNetwork>(json);
        }
    }
}