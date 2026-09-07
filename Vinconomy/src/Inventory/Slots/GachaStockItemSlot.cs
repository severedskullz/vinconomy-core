using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Trading;
using Vintagestory.API.Common;

namespace Vinconomy.Inventory.Slots
{
    public class GachaStockItemSlot : StockItemSlot
    {
        public GachaStockItemSlot(InventoryBase inventory, int stallSlot, int itemSlot) : base(inventory, stallSlot, itemSlot)
        {
        }

        public override bool ItemMatchesProduct(ProductSlot product)
        {
            return TradingUtil.IsMatchingItem(product.Itemstack, this.Itemstack, inventory.Api.World);
        }

        public override bool ShouldUpdateProductSlot(ProductSlot product)
        {
            return product?.Itemstack == null;
        }

        public override void UpdateProductSlot(BaseStallSlot stall)
        {
            GachaStallSlot gachaStall = stall as GachaStallSlot;
            if (gachaStall != null)
            {
                gachaStall.RegenProduct();
            }
        }
    }

}
