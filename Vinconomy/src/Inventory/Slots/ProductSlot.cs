using Vintagestory.API.Common;

namespace Vinconomy.Inventory.Slots
{
    public class ProductSlot : VinconCloningSlot
    {
        
        public ProductSlot(InventoryBase inventory) : base(inventory)
        {
            //this.HexBackgroundColor = "#B625FF";
            this.BackgroundIcon = "commercially-general2";
        }
    }

}
