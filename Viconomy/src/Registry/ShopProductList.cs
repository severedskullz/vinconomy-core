using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Registry
{
    public class ShopProductList
    {
        public long ExpiresAt { get; internal set; }
        public List<ShopProduct> Products { get; internal set; }
    }
}
