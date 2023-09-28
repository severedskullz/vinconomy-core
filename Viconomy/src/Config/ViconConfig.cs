using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Config
{
    public class ViconConfig
    {
        public bool FoodDecaysInShops { get; set; } = true;
        public float StallPerishRate { get; set; } = 0.15f;
        public bool EnforceShopLimits { get; set; } = false;
        public int StallsPerPlayer { get; set; } = 20;
        public int ShopsPerPlayer { get; set; } = 5;
    }
}
