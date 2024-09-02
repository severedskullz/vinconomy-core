using Viconomy.GUI;
using Viconomy.Network;
using Viconomy.Registry;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Viconomy.ItemTypes
{
    public class ItemCatalog : Item
    {
        //GuiDialogGeneric ledgerGUI;
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
           
            if (this.api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI clientAPI = ((ICoreClientAPI)api);
                int shopID = slot.Itemstack.Attributes.GetInt("ShopId", -1);
                if (shopID > 0)
                {
                    ShopCatalogRequestPacket packet = new ShopCatalogRequestPacket();
                    packet.ShopId = shopID;
                    clientAPI.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL).SendPacket(packet);
                } else {
                    clientAPI.ShowChatMessage(Lang.Get("vinconomy:ledger-not-set"));
                }
               
            }
        }
    }
}
