using System;
using Viconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.Renderer
{
    public class ItemRenderer : IItemRenderer
    {
        public EnumItemClass getRendererClass() => EnumItemClass.Item;
        public int getPriority() => 0;
        public bool shouldCache(ItemStack stack) => true;

        public bool canHandle(ItemStack stack)
        {
            return stack.Class == EnumItemClass.Item;
        }

        public MeshData createMesh(BEVinconBase stall, ItemStack stack, int index)
        {
            MeshData modeldata = null;
            try
            {
                ICoreClientAPI coreClientAPI = (ICoreClientAPI)stall.Api;
                IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
                if (containedMeshSource != null)
                {
                    modeldata = containedMeshSource.GenMesh(stack, coreClientAPI.BlockTextureAtlas, stall.Pos);
                    if (modeldata != null)
                    {
                        return modeldata;
                    }

                }


                stall.SetNowTesselatingObj(stack.Collectible);
                stall.SetNowTesselatingShape(null);

                if (stack.Item.Shape?.Base != null)
                {
                    stall.SetNowTesselatingShape(coreClientAPI.TesselatorManager.GetCachedShape(stack.Item.Shape.Base));
                }

                coreClientAPI.Tesselator.TesselateItem(stack.Item, out modeldata, stall);
                modeldata.RenderPassesAndExtraBits.Fill((short)2);
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return modeldata;
        }


    }
}
