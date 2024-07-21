using Viconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.Renderer
{
    public interface IItemRenderer
    {
        public MeshData createMesh(BEVinconBase stall, ItemStack stack, int index);

        public bool canHandle(ItemStack stack);
        public int getPriority();
        public EnumItemClass getRendererClass();

        public bool shouldCache(ItemStack stack);

    }
}
