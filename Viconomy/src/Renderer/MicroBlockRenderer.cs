using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Viconomy.Renderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.src.Renderer
{
    public class MicroBlockRenderer : IItemRenderer
    {
        public EnumItemClass getRendererClass() => EnumItemClass.Block;
        public int getPriority() => 1;
        public bool shouldCache(ItemStack stack) => false;

        public bool canHandle(ItemStack stack)
        {
            return stack.Block is BlockMicroBlock;
        }

        public MeshData createMesh(BEViconBase stall, ItemStack stack, int index)
        {
            ICoreClientAPI coreClientAPI = stall.Api as ICoreClientAPI;

            ITreeAttribute treeAttribute = stack.Attributes;
            if (treeAttribute == null)
            {
                treeAttribute = new TreeAttribute();
            }
            int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(treeAttribute, coreClientAPI.World);
            IntArrayAttribute intArrayAttribute = treeAttribute["cuboids"] as IntArrayAttribute;
            uint[] array = (intArrayAttribute != null) ? intArrayAttribute.AsUint : null;
            if (array == null)
            {
                LongArrayAttribute longArrayAttribute = treeAttribute["cuboids"] as LongArrayAttribute;
                array = ((longArrayAttribute != null) ? longArrayAttribute.AsUint : null);
            }
            List<uint> voxelCuboids = (array == null) ? new List<uint>() : new List<uint>(array);
            return BlockEntityMicroBlock.CreateMesh(coreClientAPI, voxelCuboids, materials);
            
        }

        

    }
}
