using Vintagestory.API.Common;

namespace Vinconomy.BlockEntities
{
    public class BEVinconJobboard : BlockEntity
    {
        //private GuiViconJobboard gui;
        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                //gui = new GuiViconJobboard("TEST",blockSel.Position, (ICoreClientAPI) Api);
                //gui.TryOpen();
            }
            return true;
        }

    }
}
