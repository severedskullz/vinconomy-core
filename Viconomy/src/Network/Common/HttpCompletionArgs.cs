
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Viconomy.Network.Common
{
    public class HttpCompletionArgs : CompletedArgs
    {
        public IPlayer RequestingPlayer { get; internal set; }
    }
}
