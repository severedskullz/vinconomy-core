using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Viconomy.BlockTypes;
using Viconomy.Filters;
using Viconomy.GUI;
using Viconomy.Inventory;
using Viconomy.Renderer;
using Viconomy.Trading;
using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEViconStallNew : BEViconStall
    {
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (shouldRenderInventory)
            {
                for (int i = 0; i < StallSlotCount; i++)
                {
                    try
                    {
                        ItemSlot slot = this.inventory.FindFirstNonEmptyStockSlot(i);
                        if (slot != null && !slot.Empty && tfMatrices != null)
                        {
                            mesher.AddMeshData(getOrCreateMesh(slot.Itemstack, i), tfMatrices[i]);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Had some trouble rendering a mesh in a stall for item");
                    }

                }
            }

            MeshData mesh = this.getMesh(tesselator);
            if (mesh == null)
            {
                return false;
            }
            string part = base.Block.LastCodePart(0);
            float MeshAngle = 0;
            mesh = mesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);
            
            mesher.AddMeshData(mesh, 1);


            return true;
        }

        private MeshData getMesh(ITesselatorAPI tesselator)
        {
            Dictionary<string, MeshData> stallMeshes = ObjectCacheUtil.GetOrCreate(this.Api, "vinconStallMeshes", () => new Dictionary<string, MeshData>());
            MeshData mesh = null;
            BlockVContainer block = this.Api.World.BlockAccessor.GetBlock(this.Pos) as BlockVContainer;
            if (block == null)
            {
                return null;
            }
            string orient = block.LastCodePart(0);
            string key = $"{this.PrimaryMaterial}-{this.SecondaryMaterial}-{orient}";
            if (stallMeshes.TryGetValue(key, out mesh))
            {
                return mesh;
            }

            mesh = block.GenMesh(this.Api as ICoreClientAPI, this.PrimaryMaterial, this.SecondaryMaterial, this.DecoMaterial, null, tesselator);
            stallMeshes[key] = mesh;
            return mesh;
        }
    }

}
