namespace Viconomy.Util
{
    public static class VinConstants
    {
        public const string VINCONOMY_CHANNEL = "Vinconomy";

        //Client Packets


        //Shared Packets
        public const int OPEN_GUI = 1000;
        public const int CLOSE_GUI = 1001;
        public static int SEARCH_SHOPS = 1002;
        public static int GET_PRODUCTS = 1003;
        public static int SET_TRADER = 1004;
        public static int SUMMON_TRADER = 1005;


        //Admin Packets
        public const int SET_ITEMS_PER_PURCHASE = 2001;
        public const int SET_REGISTER_ID = 2002;
        public const int SET_SHOP_NAME = 2003;
        public const int SET_ADMIN_SHOP = 2004;
        public const int SET_SCULPTURE_SLOT = 2005;
        public const int SET_SCULPTURE_XZ = 2006;
        public const int SET_SCULPTURE_Y = 2007;
        public const int SET_SCULPTURE_NAME = 2008;
        public const int SET_WAYPOINT = 2009;
        public const int SET_TOTAL_RANDOMIZER = 2010;
        public const int SET_ITEM_PRICE = 2011;

        // Customer Packets
        public const int PURCHASE_ITEMS = 3002;
        public const int CURRENCY_CONVERSION = 3003;

        public const int TOGGLE_GUI = 5000;

        public const string TRADE_STATUS_PENDING = "PENDING";
        public const string TRADE_STATUS_PROCESSED = "PROCESSED";
        public const string TRADE_STATUS_COMPLETED = "COMPLETED";
        public const string TRADE_STATUS_FAILED = "FAILED";
        public const string TRADE_STATUS_LACKS_ITEMS = "LACKS_ITEMS";
        public const string TRADE_STATUS_CANCELED = "CANCELED";

    }
}
