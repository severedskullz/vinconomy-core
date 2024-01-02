using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.GUI;
using Viconomy.Registry;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Viconomy.ItemTypes
{
    public class ItemLedger : Item
    {
        GuiDialogGeneric ledgerGUI;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
           
            if (this.api.Side == EnumAppSide.Client)
            {

                int shopID = slot.Itemstack.Attributes.GetInt("ShopId", -1);
                if (shopID > 0)
                {
                    if (ledgerGUI == null || (ledgerGUI != null && !ledgerGUI.IsOpened()))
                    {
                        ViconomyCoreSystem modSys = api.ModLoader.GetModSystem<ViconomyCoreSystem>();

                        string owner = slot.Itemstack.Attributes.GetString("Owner");
                        ShopRegistration shop = modSys.GetRegistry().GetShop(owner, shopID);
                        if (shop != null)
                        {
                            ledgerGUI = new GuiViconLedger(shop.Name, shopID, (ICoreClientAPI)this.api);
                            ledgerGUI.TryOpen();
                        } else
                        {
                            ((ICoreClientAPI)this.api).ShowChatMessage(Lang.Get("vinconomy:ledger-shop-not-found"));
                        }
                        
                    }
                    else
                    {
                        ledgerGUI.TryClose();
                        ledgerGUI = null;
                    }
                } else {
                    ((ICoreClientAPI)this.api).ShowChatMessage(Lang.Get("vinconomy:ledger-not-set"));
                }
               
            }
        }
    }
}
