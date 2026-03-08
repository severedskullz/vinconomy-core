
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Vinconomy.Network.Common
{
    public class HttpCompletionArgs : CompletedArgs
    {
        public IPlayer RequestingPlayer { get; internal set; }
    }
}
