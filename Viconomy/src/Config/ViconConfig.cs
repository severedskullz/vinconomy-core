using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.src.Config
{
    public class ViconConfig
    {
        public static ViconConfig Current { get; set; }

        public bool FoodDecaysInShops { get; set; }
    }
}
