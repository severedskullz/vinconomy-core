using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.BlockEntities
{
    public class BEViconJobboard : BlockEntity
    {
        private GuiViconJobboard gui;
        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                gui = new GuiViconJobboard("TEST",blockSel.Position, (ICoreClientAPI) Api);
                gui.TryOpen();
            }
            return true;
        }

    }
}
