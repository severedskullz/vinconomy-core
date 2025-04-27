using Vintagestory.API.Common;

namespace Viconomy.BlockEntities
{
    public interface IOwnableStall
    {
        public string Owner { get; }
        public void SetOwner(IPlayer owner);
        public void SetOwner(string playerUUID, string playerName);
        public void SetRegisterID(int registerID);
        public void SetIsAdminShop(bool isAdminShop);
    }
}