using System.Text;
using Viconomy.Network;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Viconomy.ItemTypes
{
    public class ItemLedger : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
           
            if (this.api.Side == EnumAppSide.Client)
            {
                ICoreClientAPI clientAPI = ((ICoreClientAPI)api);

                IPlayer player = (byEntity as EntityPlayer).Player;
                int shopID = slot.Itemstack.Attributes.GetInt("ShopId", -1);
                if (shopID > 0 || player.WorldData.CurrentGameMode == EnumGameMode.Creative)
                {
                    //clientAPI.Network.GetChannel(VinConstants.VINCONOMY_CHANNEL).SendPacket(new LedgerReadRequestPacket() { shopId = shopID });
                    VinconomyLedgerSystem modSys = api.ModLoader.GetModSystem<VinconomyLedgerSystem>();
                    modSys.RequestToReadLedgerData(shopID);
                } else {
                    clientAPI.ShowChatMessage(Lang.Get("vinconomy:ledger-not-set"));
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
