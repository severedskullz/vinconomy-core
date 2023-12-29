using Viconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.Renderer
{
    public class BlockRenderer : IItemRenderer
    {
        public EnumItemClass getRendererClass() => EnumItemClass.Block;
        public int getPriority() => 0;
        public bool shouldCache(ItemStack stack) => true;

        public bool canHandle(ItemStack stack)
        {
            return stack.Class == EnumItemClass.Block;
        }

        public MeshData createMesh(BEViconBase stall, ItemStack stack, int index)
        {
            ICoreClientAPI coreClientAPI = (ICoreClientAPI)stall.Api;
            return coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
        }

        

    }
}
