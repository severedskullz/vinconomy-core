using Viconomy.Network.Api;
using Vintagestory.API.Common;

namespace Viconomy.Inventory
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
    }
}
