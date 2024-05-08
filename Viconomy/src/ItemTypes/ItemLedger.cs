using Viconomy.GUI;
using Viconomy.Registry;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Viconomy.ItemTypes
{
    public class ItemLedger : Item
    {
        //GuiDialogGeneric ledgerGUI;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
           
            if (this.api.Side == EnumAppSide.Client)
            {

                int shopID = slot.Itemstack.Attributes.GetInt("ShopId", -1);
                if (shopID > 0)
                {
                    /*
                    if (ledgerGUI == null || (ledgerGUI != null && !ledgerGUI.IsOpened()))
                    {
                        ViconomyCoreSystem modSys = api.ModLoader.GetModSystem<ViconomyCoreSystem>();

                        ShopRegistration shop = modSys.GetRegistry().GetShop(shopID);
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
                    */
                    ViconomyLedgerSystem modSys = api.ModLoader.GetModSystem<ViconomyLedgerSystem>();
                    modSys.RequestToReadLedgerData(shopID);
                } else {
                    ((ICoreClientAPI)this.api).ShowChatMessage(Lang.Get("vinconomy:ledger-not-set"));
                }
               
            }
        }
    }
}
