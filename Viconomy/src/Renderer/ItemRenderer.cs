using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Viconomy.Renderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Viconomy.src.Renderer
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

        public MeshData createMesh(BEViconBase stall, ItemStack stack, int index)
        {
            MeshData modeldata = null;

            ICoreClientAPI coreClientAPI = (ICoreClientAPI) stall.Api;
            stall.SetNowTesselatingObj(stack.Collectible);
            stall.SetNowTesselatingShape(null);

            if (stack.Item.Shape?.Base != null)
            {
                stall.SetNowTesselatingShape(coreClientAPI.TesselatorManager.GetCachedShape(stack.Item.Shape.Base));
            }

            coreClientAPI.Tesselator.TesselateItem(stack.Item, out modeldata, stall);
            modeldata.RenderPassesAndExtraBits.Fill((short)2);
            return modeldata;
        }


    }
}
