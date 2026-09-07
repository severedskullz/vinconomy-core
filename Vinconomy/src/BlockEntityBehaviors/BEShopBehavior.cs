using Commercially.Common;
using Commercially.Common.Blocks.BlockEntityBehaviors;
using Commercially.Common.Interfaces;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.ModSystems;
using Commercially.Vinconomy.Util;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;


namespace Commercially.Vinconomy.BlockEntityBehaviors
{
    /// <summary>
    /// A generic Shop.
    /// </summary>
    public class BEShopBehavior : BEBehaviorOwnableRoot, IShopComponent
    {
        public IShopInventoryProvider ShopInventoryProvider => _ShopInventoryProvider;
        IShopInventoryProvider _ShopInventoryProvider;

        public IOwnableRoot Ownable => _Ownable;
        IOwnableRoot _Ownable;

        public override bool MaintainOwnershipOnBreak => true;

        public ItemSlot TradePass => _ShopInventoryProvider.TradePass;
        public ItemSlot[] CurrencySlots => _ShopInventoryProvider.CurrencySlots;
        public ItemSlot[] CouponSlots => _ShopInventoryProvider.CouponSlots;
        public override string DefaultLangCode => "vinconomy:default-shop-name";

        public BEShopBehavior(BlockEntity blockentity) : base(blockentity)
        {
           
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _ShopInventoryProvider = this.GetComponent<IShopInventoryProvider>();
            _Ownable = this.GetComponent<IOwnableRoot>();
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == VinConstants.SET_CONFIGURATION)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {

                    if (player.PlayerUID == OwnerUID)
                    {
                        BinaryReader reader = new BinaryReader(ms);
                        string name = reader.ReadString();
                        string description = reader.ReadString();
                        string shortDescription = reader.ReadString();
                        string webhook = reader.ReadString();
                        this.Name = name;
                        Api.ModLoader.GetModSystem<VinconomyCoreSystem>().UpdateShopConfiguration(this.Ownable.ID, description, shortDescription, webhook);
                        Ownable.UpdateOwnership(OwnerUID, OwnerName, name, IsAdminOwned);
                    }
                    else
                    {
                        ((IServerPlayer)player).SendMessage(0, Lang.Get("viconomy:doesnt-own", []), EnumChatType.OwnMessage);
                    }
                }
            }
            else
            {
                base.OnReceivedClientPacket(player, packetid, data);
            }
        }
    }
}
