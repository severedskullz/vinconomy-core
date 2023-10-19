using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Viconomy.Renderer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.src.Renderer
{
    public class ClutterBlockRenderer : IItemRenderer
    {
        public EnumItemClass getRendererClass() => EnumItemClass.Block;
        public int getPriority() => 1;
        public bool shouldCache(ItemStack stack) => false;

        public bool canHandle(ItemStack stack)
        {
            return stack.Block is BlockClutter;
        }

        public MeshData createMesh(BEViconStall stall, ItemStack stack, int index)
        {
            ICoreClientAPI coreClientAPI = (ICoreClientAPI)stall.Api;


            Dictionary<string, MeshRef> clutterMeshRefs = ObjectCacheUtil.GetOrCreate<Dictionary<string, MeshRef>>(coreClientAPI, (stack.Block as BlockShapeFromAttributes).ClassType + "MeshesInventory", () => new Dictionary<string, MeshRef>());
            string type = stack.Attributes.GetString("type", "");
            IShapeTypeProps cprops = (stack.Block as BlockShapeFromAttributes).GetTypeProps(type, stack, null);
            if (cprops == null)
            {
                return null;
            }
            float rotX = stack.Attributes.GetFloat("rotX", 0f);
            float rotY = stack.Attributes.GetFloat("rotY", 0f);
            float rotZ = stack.Attributes.GetFloat("rotZ", 0f);
            string otcode = stack.Attributes.GetString("overrideTextureCode", null);
            MeshData modeldata = (stack.Block as BlockShapeFromAttributes).GetOrCreateMesh(cprops, null, otcode);
            return modeldata.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotX, rotY, rotZ);

        }
    }
}
