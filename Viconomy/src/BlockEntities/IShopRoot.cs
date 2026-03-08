using Vintagestory.API.Common;

namespace Vinconomy.BlockEntities
{
    public interface IShopRoot
    {
        public string Owner { get; }
        public string OwnerName { get; }
        public int ID { get; }

        public void SetOwner(IPlayer owner);
        public void SetOwner(string playerUUID, string playerName);

        public void UpdateShop(string Owner, string OwnerName, int ID, string Name);
    }
}