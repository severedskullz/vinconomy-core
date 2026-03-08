using System;
using Vinconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Vinconomy.Renderer
{
    public class CoinItemRenderer : IItemRenderer
    {
        public EnumItemClass getRendererClass() => EnumItemClass.Item;
        public int getPriority() => 10;
        public bool shouldCache(ItemStack stack) => false;

        public bool canHandle(ItemStack stack)
        {
            return stack.Class == EnumItemClass.Item && stack.Item.Code.Domain.Equals("coinage") && stack.Item.Code.PathStartsWith("coin-");
        }

        public MeshData createMesh(BEVinconBase stall, ItemSlot slot, int index)
        {
            MeshData modeldata = null;
            ItemStack stack = slot.Itemstack;
            try
            {
                ICoreClientAPI coreClientAPI = (ICoreClientAPI)stall.Api;
                
                // For some reason this is now blowing up on 1.21 on the item renderer, so we will duplicate it and just skip this part and return the base model.
                /*
                IContainedMeshSource containedMeshSource = stack.Collectible as IContainedMeshSource;
                if (containedMeshSource != null)
                {
                    modeldata = containedMeshSource.GenMesh(stack, coreClientAPI.BlockTextureAtlas, stall.Pos);
                    if (modeldata != null)
                    {
                        return modeldata;
                    }

                }
                */


                stall.SetNowTesselatingObj(stack.Collectible);
                stall.SetNowTesselatingShape(null);

                if (stack?.Item.Shape?.Base != null)
                {
                    stall.SetNowTesselatingShape(coreClientAPI.TesselatorManager.GetCachedShape(stack.Item.Shape.Base));
                }

                coreClientAPI.Tesselator.TesselateItem(stack.Item, out modeldata, stall);
                modeldata.RenderPassesAndExtraBits.Fill((short)2);
                modeldata.Scale(new Vintagestory.API.MathTools.Vec3f(0.5f, 0, 0.5f), .4f, .4f, .4f);
            } catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return modeldata;
        }


    }
}
