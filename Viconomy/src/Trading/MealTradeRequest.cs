using Vinconomy.Trading;
using Vintagestory.API.Common;

namespace Vinconomy.Trading
{
    internal class MealTradeRequest : GenericTradeRequest
    {
        public MealTradeRequest(ICoreAPI api, IPlayer player) : base(api, player)
        {
        }
    }
}
