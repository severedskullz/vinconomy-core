using System.Text;
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
        VinconomyCoreSystem modSystem;



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
                    //clientAPI.ShowChatMessage(Lang.Get("vinconomy:ledger-not-set"));
                    ShopCatalogRequestPacket packet = new ShopCatalogRequestPacket();
                    clientAPI.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL).SendPacket(packet);
                }
               
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            int shopID = inSlot.Itemstack.Attributes.GetInt("ShopId", -1);
            if (shopID > 0)
            {
                if (modSystem == null)
                {
                    modSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
                }

                ShopRegistration shop = modSystem.GetRegistry().GetShop(inSlot.Itemstack.Attributes.GetInt("ShopId"));
                dsc.AppendLine("Bound to shop: " + shop.Name);
            }
            

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            
        }
    }
}
