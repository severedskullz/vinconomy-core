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
        public bool shouldCache(ItemStack stack) => true;

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
            string hashkey = string.Concat(new string[] { cprops.HashKey, "-", rotX.ToString(), "-", rotY.ToString(), "-", rotZ.ToString(), "-", otcode });
            MeshRef meshref;
            if (clutterMeshRefs.TryGetValue(hashkey, out meshref))
            {
                MeshData modeldataold = (stack.Block as BlockShapeFromAttributes).GetOrCreateMesh(cprops, null, otcode);
                return modeldataold.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotX, rotY, rotZ);
            }


            MeshData modeldata = (stack.Block as BlockShapeFromAttributes).GetOrCreateMesh(cprops, null, otcode);
            return modeldata.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), rotX, rotY, rotZ);

        }

        //From: BlockShapeFromAttributes
        /*
        private MeshData getClutter(IShapeTypeProps cprops, ITexPositionSource overrideTexturesource = null, string overrideTextureCode = null)
        {
            Dictionary<string, MeshData> orCreate = ObjectCacheUtil.GetOrCreate(api, ClassType + "Meshes", () => new Dictionary<string, MeshData>());
            ICoreClientAPI coreClientAPI = api as ICoreClientAPI;
            if (overrideTexturesource != null || !orCreate.TryGetValue(cprops.Code + "-" + overrideTextureCode, out var modeldata))
            {
                modeldata = new MeshData(4, 3);
                Shape shapeResolved = cprops.ShapeResolved;
                ITexPositionSource texPositionSource = overrideTexturesource;
                if (texPositionSource == null)
                {
                    ShapeTextureSource shapeTextureSource = new ShapeTextureSource(coreClientAPI, shapeResolved);
                    texPositionSource = shapeTextureSource;
                    if (blockTextures != null)
                    {
                        foreach (KeyValuePair<string, CompositeTexture> blockTexture in blockTextures)
                        {
                            if (blockTexture.Value.Baked == null)
                            {
                                blockTexture.Value.Bake(coreClientAPI.Assets);
                            }

                            shapeTextureSource.textures[blockTexture.Key] = blockTexture.Value;
                        }
                    }

                    if (cprops.Textures != null)
                    {
                        foreach (KeyValuePair<string, CompositeTexture> texture in cprops.Textures)
                        {
                            CompositeTexture compositeTexture = texture.Value.Clone();
                            compositeTexture.Bake(coreClientAPI.Assets);
                            shapeTextureSource.textures[texture.Key] = compositeTexture;
                        }
                    }

                    if (overrideTextureCode != null && cprops.TextureFlipCode != null && OverrideTextureGroups[cprops.TextureFlipGroupCode].TryGetValue(overrideTextureCode, out var value))
                    {
                        shapeTextureSource.textures[cprops.TextureFlipCode] = value;
                        value.Bake(coreClientAPI.Assets);
                    }
                }

                if (shapeResolved == null)
                {
                    return modeldata;
                }

                coreClientAPI.Tesselator.TesselateShape(ClassType + "block", shapeResolved, out modeldata, texPositionSource, null, 0, 0, 0);
                if (cprops.TexPos == null)
                {
                    api.Logger.Warning("No texture previously loaded for clutter block " + cprops.Code);
                    cprops.TexPos = (texPositionSource as ShapeTextureSource)?.firstTexPos;
                    cprops.TexPos.RndColors = new int[30];
                }

                if (overrideTexturesource == null)
                {
                    orCreate[cprops.Code + "-" + overrideTextureCode] = modeldata;
                }
            }

            return modeldata;
        }
        */
    }
}
