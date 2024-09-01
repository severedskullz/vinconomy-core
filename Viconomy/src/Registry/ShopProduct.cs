using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Registry
{
    public class ShopProduct
    {
        public string ProductName { get; internal set; }
        public string ProductCode { get; internal set; }
        public string ProductAttributes { get; internal set; }
        public int ProductQuantity { get; internal set; }
        public int TotalStock { get; internal set; }
        public string CurrencyAttributes { get; internal set; }
        public int CurrencyAmount { get; internal set; }
        public string CurrencyCode { get; internal set; }
        public string CurrencyName { get; internal set; }
       
    }
}
