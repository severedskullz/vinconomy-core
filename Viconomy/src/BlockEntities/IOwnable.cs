using Vintagestory.API.Common;

namespace Viconomy.BlockEntities
{
    public interface IOwnable
    {
        public void SetOwner(IPlayer owner);
        public void SetOwner(string playerUUID, string playerName);
        public string Owner { get; }
        public string OwnerName { get; }
    }
}