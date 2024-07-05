using System.Collections.Generic;
using Viconomy.BlockTypes;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.BlockEntities.TextureSwappable
{
    public class BETextureSwappableBlock : BlockEntity
    {
        public string PrimaryMaterial { get; set; }
        public string SecondaryMaterial { get; set; }
        public string DecoMaterial { get; set; }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {

            MeshData mesh = getMesh(tesselator);
            if (mesh == null)
            {
                return false;
            }
            string part = Block.LastCodePart(0);
            float MeshAngle = 0;
            mesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);

            mesher.AddMeshData(mesh, 1);


            return true;
        }

        protected MeshData getMesh(ITesselatorAPI tesselator)
        {
            Dictionary<string, MeshData> stallMeshes = ObjectCacheUtil.GetOrCreate(Api, "vinconSwappableMeshes", () => new Dictionary<string, MeshData>());
            MeshData mesh = null;
            TextureSwappableBlock block = Api.World.BlockAccessor.GetBlock(Pos) as TextureSwappableBlock;
            if (block == null)
            {
                return null;
            }
            string key = $"{block.Code.Path}-{PrimaryMaterial}-{SecondaryMaterial}-{DecoMaterial}";
            if (stallMeshes.TryGetValue(key, out mesh))
            {
                return mesh;
            }

            mesh = block.GenMesh(Api as ICoreClientAPI, PrimaryMaterial, SecondaryMaterial, DecoMaterial, null, tesselator);
            stallMeshes[key] = mesh;
            return mesh;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (PrimaryMaterial != "default")
            {
                tree.SetString("PrimaryMaterial", PrimaryMaterial);
            }
            if (SecondaryMaterial != "default")
            {
                tree.SetString("SecondaryMaterial", SecondaryMaterial);
            }
            if (DecoMaterial != "default")
            {
                tree.SetString("DecoMaterial", DecoMaterial);
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            PrimaryMaterial = tree.GetString("PrimaryMaterial", "default");
            SecondaryMaterial = tree.GetString("SecondaryMaterial", "default");
            DecoMaterial = tree.GetString("DecoMaterial", "default");

            if (Api != null && Api.Side == EnumAppSide.Client)
            {
                MarkDirty(true, null);
            }

        }

    }
}
