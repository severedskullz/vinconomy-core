namespace Viconomy.Util
{
    public static class VinConstants
    {
        public const string VINCONOMY_CHANNEL = "Vinconomy";

        //Shared Packets
        public const int OPEN_GUI = 1000;
        public const int CLOSE_GUI = 1001;
        public const int SEARCH_SHOPS = 1002;
        public const int GET_PRODUCTS = 1003;
        public const int SET_TRADER = 1004;
        public const int SUMMON_TRADER = 1005;
        public const int SET_COUPON_SHOPS = 1006;
        public const int SET_COUPON_DISCOUNT_TYPE = 1007;
        public const int SET_COUPON_BONUS_TYPE = 1008;
        public const int SET_COUPON_BLACKLIST = 1009;
        public const int SET_COUPON_CONSUME_ON_PURCHASE = 1010;
        public const int ACTIVATE_BLOCK = 1011;

        //Owner Packets
        public const int SET_ITEMS_PER_PURCHASE = 2001;
        public const int SET_REGISTER_ID = 2002;
        public const int SET_SHOP_NAME = 2003;
        public const int SET_ADMIN_SHOP = 2004;
        public const int SET_SCULPTURE_SLOT = 2005;
        public const int SET_SCULPTURE_XZ = 2006;
        public const int SET_SCULPTURE_Y = 2007;
        public const int SET_ITEM_NAME = 2008;
        public const int SET_WAYPOINT = 2009;
        public const int SET_TOTAL_RANDOMIZER = 2010;
        public const int SET_ITEM_PRICE = 2011;

        public const int ADD_PLAYER_PERMISSION = 2012;
        public const int REMOVE_PLAYER_PERMISSION = 2013;
        public const int SET_STALL_PERMISSION = 2014;
        public const int UPDATE_SHOP_PERMISSIONS = 2014;

        public const int SET_PURCHASES_REMAINING = 2015;
        public const int SET_REGISTER_FALLBACK = 2016;
        public const int SET_LIMITED_PURCHASES = 2017;


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
