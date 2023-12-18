using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viconomy.Util
{
    public static class VinConstants
    {
        //Shared Packets
        public const int OPEN_GUI = 1000;
        public const int CLOSE_GUI = 1001;

        //Admin Packets
        public const int SET_ITEMS_PER_PURCHASE = 2001;
        public const int SET_REGISTER_ID = 2002;
        public const int SET_SHOP_NAME = 2003;
        public const int SET_ADMIN_SHOP = 2004;
        public const int SET_SCULPTURE_SLOT = 2005;
        public const int SET_SCULPTURE_XZ = 2006;
        public const int SET_SCULPTURE_Y = 2007;
        public const int SET_SCULPTURE_NAME = 2008;

        // Customer Packets
        public const int PURCHASE_ITEMS = 3002;
        public const int CURRENCY_CONVERSION = 3003;

        public const int TOGGLE_GUI = 5000;

    }
}
