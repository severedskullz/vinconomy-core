using Vintagestory.API.Common;

namespace Viconomy.BlockEntities
{
    public interface IInteractableStall
    {
        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);
    }
}