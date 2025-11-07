using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.BlockTypes
{
    public class BlockVDualLiquidContainer : BlockVLiquidContainer
    {
        public override AssetLocation liquidContentsShape { get; protected set; } = AssetLocation.Create("shapes/block/basic/cube.json");

        #region Mesh generation
        public override MeshData GenMesh(ItemStack contentStack, ItemStack liquidContentStack, bool issealed, BlockPos forBlockPos = null)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
           
            var containerProps = liquidContentStack?.ItemAttributes?["waterTightContainerProps"];
            MeshData contentMesh = getContentMeshLiquids(contentStack, liquidContentStack, forBlockPos, containerProps);

            return contentMesh;
        }

        protected override MeshData getContentMesh(ItemStack stack, BlockPos forBlockPos, AssetLocation shapefilepath)
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
                //fillHeight = GameMath.Min(1f, stack.StackSize / props.ItemsPerLitre / Math.Max(50, props.MaxStackSize)) * 10f / 16f;
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

                //contentMesh.Translate(0, fillHeight, 0);

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

        #endregion
    }
}
