using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Viconomy.Renderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.src.Renderer
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

        public MeshData createMesh(BEViconStall stall, ItemStack stack, int index)
        {
            ICoreClientAPI coreClientAPI = (ICoreClientAPI)stall.Api;
            return coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
        }

        

    }
}
