using Vinconomy.Network.JavaApi;
using Vintagestory.API.Common;

namespace Vinconomy.Inventory
{
    public class VinconNetworkItemSlot : ItemSlot
    {
        public ItemStack Product {  get; set; }
        public ItemStack Currency { get; set; }

        public int StallSlot { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }     
        public int TotalStock { get; set; }

        public VinconNetworkItemSlot(InventoryBase inventory) : base(inventory)
        {
        }

        public override bool DrawUnavailable { get => Product == null || Currency == null || Product.StackSize > TotalStock ; set => base.DrawUnavailable = value; }
    }
}
