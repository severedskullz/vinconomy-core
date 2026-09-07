using Commercially.Vinconomy.Network.Packets;
using Commercially.Vinconomy.Util;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vinconomy.ItemTypes
{
    public class ItemCatalog : Item
    {
        
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
           
            if (this.api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI clientAPI = ((ICoreClientAPI)api);
                int shopID = slot.Itemstack.Attributes.GetInt("ShopId", -1);
                if (shopID > 0)
                {
                    CatalogRequestPacket packet = new CatalogRequestPacket();
                    packet.ShopId = shopID;
                    clientAPI.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL).SendPacket(packet);
                } else {
                    //clientAPI.ShowChatMessage(Lang.Get("vinconomy:ledger-not-set"));
                    CatalogRequestPacket packet = new CatalogRequestPacket();
                    clientAPI.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL).SendPacket(packet);
                }
               
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            int shopID = inSlot.Itemstack.Attributes.GetInt("ShopId", -1);
            if (shopID > 0)
            {
                string shopName = inSlot.Itemstack?.Attributes?.GetString("ShopName");
                if (shopName != null)
                {
                    dsc.AppendLine(Lang.Get("vinconomy:gui-f-shop", [shopName]));
                }
            }
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            
        }
        
    }
}
