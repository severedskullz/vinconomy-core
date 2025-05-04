namespace Viconomy.Trading
{
    public class TradingConstants
    {
        public static readonly string NO_GACHA_BALL = "vinconomy:no-gacha-ball";
        public static readonly string GACHA_ATLEAST_ONE = "vinconomy:gacha-atleast-one";
        public static readonly string GACHA_BUNDLED_ZERO = "vinconomy:gacha-bundled-zero";
        public static readonly string NOT_ENOUGH_MONEY = "vinconomy:not-enough-money";
        public static readonly string NO_PRICE = "vinconomy:no-price";
        public static readonly string NOT_ENOUGH_STOCK = "vinconomy:not-enough-stock";
        public static readonly string PURCHASED_ZERO = "vinconomy:purchased-zero-quantity";
        public static readonly string NO_PRODUCT = "vinconomy:no-product";
        public static readonly string NOT_REGISTERED = "vinconomy:not-registered-with-shop";
        public static readonly string NO_REGISTER_SPACE = "vinconomy:no-register-space";
        public static readonly string DOESNT_OWN = "vinconomy:doesnt-own";
        public static readonly string NO_PRIVLEGE = "vinconomy:no-privelege";
        public static readonly string PURCHASED_ITEMS = "vinconomy:purchased-item";
        public static readonly string COULDNT_GET_REGISTER = "vinconomy:couldnt-find-register";
        public static readonly string NO_TOOL = "vinconomy:no-tool";
    }

    public enum ToolType
    {
        NONE = 0,
        FOOD_CONTAINER = 1,
        DRINK_CONTAINER = 2,
        LIQUID_COINTAINER = 3,
        CHOPPING = 4,
        CUTTING = 5
    }
}
