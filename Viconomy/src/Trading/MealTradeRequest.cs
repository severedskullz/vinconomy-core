using Viconomy.Trading;
using Vintagestory.API.Common;

namespace Viconomy.Trading
{
    internal class MealTradeRequest : GenericTradeRequest
    {
        public MealTradeRequest(ICoreAPI api, IPlayer player) : base(api, player)
        {
        }
    }
}
