using Vintagestory.API.Common;

namespace Viconomy.BlockEntities
{
    public interface IOwnableStall : IOwnable
    {
        public bool IsAdminShop { get; }
        public void SetRegisterID(int registerID);
        public void SetIsAdminShop(bool isAdminShop);
    }
}