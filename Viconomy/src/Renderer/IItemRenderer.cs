using Vinconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vinconomy.Renderer
{
    public interface IItemRenderer
    {
        public MeshData createMesh(BEVinconBase stall, ItemSlot slot, int index);

        public bool canHandle(ItemStack stack);
        public int getPriority();
        public EnumItemClass getRendererClass();

        public bool shouldCache(ItemStack stack);

    }
}
