using ProtoBuf;
using Vintagestory.API.Common;


namespace Vinconomy.Registry
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ShopAccess
    {
        public string PlayerName;
        public string PlayerUID;

        public ShopAccess() { }

        public ShopAccess(string playerUID, string playerName)
        {
            PlayerUID = playerUID;
            PlayerName = playerName;
        }

        public ShopAccess(IPlayer player)
        {
            PlayerName = player.PlayerName;
            PlayerUID = player.PlayerUID;
        }
    }
}
