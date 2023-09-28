using Viconomy.BlockEntities;
using Vintagestory.API.Common;

namespace Viconomy.Delegates
{
    public delegate bool OnPurchasedItemDelegate(IPlayer player, BEViconStall stall, BEVRegister register, ItemStack product, ItemStack payment);
    public delegate bool CanPurchaseItemDelegate(IPlayer player, BEViconStall stall, BEVRegister register, ItemSlot product, ItemSlot payment);

}