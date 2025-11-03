using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.BlockTypes
{
    public class BlockVLiquidContainer : BlockVContainer
    {
        public AssetLocation liquidContentsShape { get; protected set; } = AssetLocation.Create("shapes/block/wood/barrel/liquidcontents.json");

        #region Mesh generation
        public MeshData GenMesh(ItemStack contentStack, ItemStack liquidContentStack, bool issealed, BlockPos forBlockPos = null)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
           
            var containerProps = liquidContentStack?.ItemAttributes?["waterTightContainerProps"];
            MeshData contentMesh = getContentMeshLiquids(contentStack, liquidContentStack, forBlockPos, containerProps);

            return contentMesh;
        }

        private MeshData getContentMeshLiquids(ItemStack contentStack, ItemStack liquidContentStack, BlockPos forBlockPos, JsonObject containerProps)
        {
            bool isopaque = containerProps?["isopaque"].AsBool(false) == true;
            bool isliquid = containerProps?.Exists == true;
            if (liquidContentStack != null && (isliquid || contentStack == null))
            {
                AssetLocation shapefilepath = liquidContentsShape;

                return getContentMesh(liquidContentStack, forBlockPos, shapefilepath);
            }

            return null;
        }

        protected MeshData getContentMesh(ItemStack stack, BlockPos forBlockPos, AssetLocation shapefilepath)
        {
            if (stack == null) return null;
            ICoreClientAPI capi = api as ICoreClientAPI;

            WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(stack);
            ITexPositionSource contentSource;
            float fillHeight;

            if (props != null)
            {
                if (props.Texture == null) return null;

                contentSource = new ContainerTextureSource(capi, stack, props.Texture);
                fillHeight = GameMath.Min(1f, stack.StackSize / props.ItemsPerLitre / Math.Max(50, props.MaxStackSize)) * 10f / 16f;
            }
            else
            {
                contentSource = getContentTexture(capi, stack, out fillHeight);
            }


            if (contentSource != null)
            {
                Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapefilepath);
                if (shape == null)
                {
                    api.Logger.Warning(string.Format("Liquid Stall Block '{0}': Content shape {1} not found. Will try to default to another one.", Code, shapefilepath));
                    return null;
                }
                capi.Tesselator.TesselateShape("barrel", shape, out MeshData contentMesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), props?.GlowLevel ?? 0);

                contentMesh.Translate(0, fillHeight, 0);

                if (props?.ClimateColorMap != null)
                {
                    int col;
                    if (forBlockPos != null)
                    {
                        col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, false);
                    }
                    else
                    {
                        col = capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, ColorUtil.WhiteArgb, 196, 128, false);
                    }

                    byte[] rgba = ColorUtil.ToBGRABytes(col);
                    byte rgba0 = rgba[0];
                    byte rgba1 = rgba[1];
                    byte rgba2 = rgba[2];
                    byte rgba3 = rgba[3];

                    var meshRgba = contentMesh.Rgba;
                    for (int i = 0; i < meshRgba.Length; i += 4)
                    {
                        meshRgba[i + 0] = (byte)((meshRgba[i + 0] * rgba0) / 255);
                        meshRgba[i + 1] = (byte)((meshRgba[i + 1] * rgba1) / 255);
                        meshRgba[i + 2] = (byte)((meshRgba[i + 2] * rgba2) / 255);
                        meshRgba[i + 3] = (byte)((meshRgba[i + 3] * rgba3) / 255);
                    }
                }


                return contentMesh;
            }

            return null;
        }


        public static ITexPositionSource getContentTexture(ICoreClientAPI capi, ItemStack stack, out float fillHeight)
        {
            ITexPositionSource contentSource = null;
            fillHeight = 0;

            JsonObject obj = stack?.ItemAttributes?["inContainerTexture"];
            if (obj != null && obj.Exists)
            {
                contentSource = new ContainerTextureSource(capi, stack, obj.AsObject<CompositeTexture>());
                fillHeight = GameMath.Min(12 / 16f, 0.7f * stack.StackSize / stack.Collectible.MaxStackSize);
            }
            else
            {
                if (stack?.Block != null && (stack.Block.DrawType == EnumDrawType.Cube || stack.Block.Shape.Base.Path.Contains("basic/cube")) && capi.BlockTextureAtlas.GetPosition(stack.Block, "up", true) != null)
                {
                    contentSource = new BlockTopTextureSource(capi, stack.Block);
                    fillHeight = GameMath.Min(12 / 16f, 0.7f * stack.StackSize / stack.Collectible.MaxStackSize);
                }
                else if (stack != null)
                {

                    if (stack.Class == EnumItemClass.Block)
                    {
                        if (stack.Block.Textures.Count > 1) return null;

                        contentSource = new ContainerTextureSource(capi, stack, stack.Block.Textures.FirstOrDefault().Value);
                    }
                    else
                    {
                        if (stack.Item.Textures.Count > 1) return null;

                        contentSource = new ContainerTextureSource(capi, stack, stack.Item.FirstTexture);
                    }


                    fillHeight = GameMath.Min(12 / 16f, 0.7f * stack.StackSize / stack.Collectible.MaxStackSize);
                }
            }

            return contentSource;
        }

        #endregion
    }
}
