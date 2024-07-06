using Viconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

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

        public MeshData createMesh(BEVinconBase stall, ItemStack stack, int index)
        {
            ICoreClientAPI coreClientAPI = (ICoreClientAPI)stall.Api;

            IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
            if (containedMeshSource != null)
            {
                MeshData modeldata = containedMeshSource.GenMesh(stack, coreClientAPI.BlockTextureAtlas, stall.Pos);
                if (modeldata != null)
                {
                    return modeldata;
                }

            }

            return coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
        }

        

    }
}
