using System;
using Vinconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Vinconomy.Renderer
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

        public MeshData createMesh(BEVinconBase stall, ItemSlot slot, int index)
        {
            ItemStack stack = slot.Itemstack;
            ICoreClientAPI coreClientAPI = (ICoreClientAPI)stall.Api;
            try
            {
                
                IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
                if (containedMeshSource != null)
                {
                    MeshData modeldata = containedMeshSource.GenMesh(slot, coreClientAPI.BlockTextureAtlas, stall.Pos);
                    if (modeldata != null)
                    {
                        return modeldata;
                    }

                }

                if (stack.Block is BlockGenericTypedContainer)
                {
                    BlockGenericTypedContainer container =  stack.Block as BlockGenericTypedContainer;
                    string type = stack.Attributes.GetAsString("type");
                    MeshData mesh =  container.GenMesh(coreClientAPI, type, stack.ItemAttributes["shape"][type].AsString());
                    return mesh;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            return coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
        }

        

    }
}
