using Vinconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vinconomy.Renderer
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

        public MeshData createMesh(BEVinconBase stall, ItemSlot slot, int index)
        {
            //ICoreClientAPI coreClientAPI = (ICoreClientAPI)stall.Api;

            //Dictionary<string, MultiTextureMeshRef> clutterMeshRefs = ObjectCacheUtil.GetOrCreate(coreClientAPI, "viconClutterMeshesInventory", () => new Dictionary<string, MultiTextureMeshRef>());
            ItemStack stack = slot.Itemstack;
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
