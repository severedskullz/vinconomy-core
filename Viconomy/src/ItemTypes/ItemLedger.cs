using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.ItemTypes
{
    public class ItemLedger : Item
    {
        GuiDialogGeneric ledgerGUI;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
           
            if (this.api.Side == EnumAppSide.Client)
            {
                if (ledgerGUI == null || (ledgerGUI != null && !ledgerGUI.IsOpened()))
                {
                    ledgerGUI = new GuiViconLedger("Ledger Name Here", (ICoreClientAPI)this.api);
                    ledgerGUI.TryOpen();
                } else
                {
                    ledgerGUI.TryClose();
                    ledgerGUI = null;
                }
            }
        }
    }
}
