using System.Collections.Generic;

namespace Viconomy.Config
{
    public class ViconConfig
    {
        public bool FoodDecaysInShops { get; set; } = true;
        public float StallPerishRate { get; set; } = 0.15f;
        //public bool EnforceShopLimits { get; set; } = false;
        //public int StallsPerPlayer { get; set; } = 20;
        //public int ShopsPerPlayer { get; set; } = 5;

        public ViconTenretniWhitelist[] ViconTenretniWhitelists { get; set; }

        public string tradingNetworkUrl { get; set; }
        public Dictionary<string, string> networkAPIKeys { get; set; } = new Dictionary<string, string>();
        public bool tradingNetworkEnabled { get; set; } = true;


        public string GetAPIKey(string guid)
        {
            if (networkAPIKeys == null)
            {
                networkAPIKeys = new Dictionary<string, string>();
            }
            if (networkAPIKeys.ContainsKey(guid))
            {
                return networkAPIKeys[guid];
            }
            return null;
        }

        public void ClearApiKeyForGUID(string savegameIdentifier)
        {
            networkAPIKeys.Remove(savegameIdentifier);
        }
    }

    public class ViconTenretniWhitelist
    {
        public string baseURL { get; set; }
        public string name { get; set; }
    }
}
