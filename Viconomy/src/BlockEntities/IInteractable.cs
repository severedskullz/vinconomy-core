using Vintagestory.API.Common;

namespace Vinconomy.BlockEntities
{
    public interface IInteractable
    {
        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);
    }
}