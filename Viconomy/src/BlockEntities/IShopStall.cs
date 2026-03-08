using Vintagestory.API.Common;

namespace Vinconomy.BlockEntities
{
    public interface IShopStall
    {
        public string Owner { get; }
        public string OwnerName { get; }
        public int ShopId { get; }
        public void SetOwner(IPlayer owner);
        public void SetOwner(string playerUUID, string playerName);
        public bool IsAdminShop { get; }
        public void SetRegisterID(int registerID);
        public void SetIsAdminShop(bool isAdminShop);
    }
}