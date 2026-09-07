namespace Commercially.Vinconomy.Util
{
    public static class VinConstants
    {
        public const string VINCONOMY_CHANNEL = "Vinconomy";

        //Shared Packets [1000 - 1999]
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

        public const int SEARCH_ITEM = 1020;
        public const int LOAD_ITEM = 1021;
        public const int SAVE_ITEM = 1022;
        public const int CREATE_ITEM = 1023;

        //Owner Packets [2000 - 2999]

        public const int SET_CONFIGURATION = 2006;
        public const int SET_SCULPTURE_XZ = 2007;
        public const int SET_SCULPTURE_Y = 2008;
        public const int SET_ITEM_NAME = 2009;
        public const int SET_TOTAL_RANDOMIZER = 2011;
        public const int SET_COUPON_VALUE = 2012;

        public const int ADD_PLAYER_PERMISSION = 2013;
        public const int REMOVE_PLAYER_PERMISSION = 2014;
        public const int SET_STALL_PERMISSION = 2015;
        public const int UPDATE_SHOP_PERMISSIONS = 2016;

        public const int SET_PURCHASES_REMAINING = 2017;
        public const int SET_REGISTER_FALLBACK = 2018;
        public const int SET_LIMITED_PURCHASES = 2019;
        public const int SET_FUZZY_MATCHING = 2020;

        public const int SET_WEIGHT = 2021;
        public const int SET_CONTENTS_QUANTITY = 2022;


        // Customer Packets [3000 - 3999]

        public const string TRADE_STATUS_PENDING = "PENDING";
        public const string TRADE_STATUS_PROCESSED = "PROCESSED";
        public const string TRADE_STATUS_COMPLETED = "COMPLETED";
        public const string TRADE_STATUS_FAILED = "FAILED";
        public const string TRADE_STATUS_LACKS_ITEMS = "LACKS_ITEMS";
        public const string TRADE_STATUS_CANCELED = "CANCELED";
        
    }
}
