using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Viconomy.BlockEntities;
using Viconomy.BlockEntities.TextureSwappable;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Viconomy.BlockTypes
{
    public class TextureSwappableBlock : Block , ITexPositionSource
    {
        private const string GUI_MESHES = "viconStallGuiMeshRefs";
        protected ITexPositionSource tmpTextureSource;

        public Size2i AtlasSize { get; private set; }

        /**
         * ITexPositionSource returns a given material for a specified texture code when applied to the tesselator.
         * What this means in laymens terms is whenever the Shape has a given "texture" code defined, it will swap out whats in the JSON
         * with what is provided by the Getter here.
         * 
         * As an example, saw we have the following in a JSON shapefile:
            ...
            "textures": {
		        "primary": "game:block/cloth/linen/white",
		        "secondary": "game:block/wood/planks/oak1"
	        },
	        "elements": [
		        {
			        "name": "Mat",
			        "from": [ 0.0, 0.0, 0.0 ],
			        "to": [ 16.0, 1.0, 16.0 ],
			        "faces": {
				        "north": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 1.0 ] },
				        "east": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 1.0 ] },
				        "south": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 1.0 ] },
				        "west": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 1.0 ] },
				        "up": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 16.0 ] },
				        "down": { "texture": "#primary", "uv": [ 0.0, 0.0, 16.0, 16.0 ] }
			        },
             ...
         * 
         *  Here, "primary" is a texture code. without ITextPositionSource implemented, it would always return the white linen texture.
         *  However, with the implementation, we can change this base on an attribute or other variable. In our case, we want to map it
         *  to the "textures" attribute on the block's JSON instead. So an Oak stall would have this.PrimaryMaterial = "oak". When
         *  the game goes to access the "primary" texture code to figure out what it is, we return "oak" which will then map to the oak
         *  texture specified in the block's JSON instead of the default white linnen texture defined in the shape file.
         */
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (this.tmpTextureSource == null)
                {
                    return null;
                }

                if (textureCode == "primary")
                {
                    return this.tmpTextureSource[this.PrimaryMaterial];
                }
                if (textureCode == "secondary")
                {
                    return this.tmpTextureSource[this.SecondaryMaterial];
                }
                if (textureCode == "deco")
                {
                    return this.tmpTextureSource[this.DecoMaterial];
                }

                if (this.tmpTextureSource[textureCode] != null)
                {
                    return this.tmpTextureSource[textureCode];
                }
                else
                {
                    return this.tmpTextureSource["default"];
                }
            }
        }

        protected string PrimaryMaterial;
        protected string SecondaryMaterial;
        protected string DecoMaterial;

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate(capi, GUI_MESHES, () => new Dictionary<string, MultiTextureMeshRef>());
            string primary = itemstack.Attributes.GetString("PrimaryMaterial", "default");
            string secondary = itemstack.Attributes.GetString("SecondaryMaterial", "default");
            string deco = itemstack.Attributes.GetString("DecoMaterial", "default");
            string key = $"{itemstack.Collectible.Code.Path}-{primary}-{secondary}-{deco}";
            MultiTextureMeshRef meshref;
            if (!meshrefs.TryGetValue(key, out meshref))
            {
                AssetLocation shapeloc = this.Shape.Base.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
                Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
                MeshData mesh = this.GenMesh(capi, primary, secondary, deco, shape, null);
                meshrefs[key] = capi.Render.UploadMultiTextureMesh(mesh);
                meshref = meshrefs[key];
            }
            renderinfo.ModelRef = meshref;
            renderinfo.CullFaces = false;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {

                PrimaryMaterial = byItemStack.Attributes.GetString("PrimaryMaterial", "default");
                SecondaryMaterial = byItemStack.Attributes.GetString("SecondaryMaterial", "default");
                DecoMaterial = byItemStack.Attributes.GetString("DecoMaterial", "default");


                BETextureSwappableBlock viconBlock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BETextureSwappableBlock;
                if (viconBlock != null)
                {
                    viconBlock.PrimaryMaterial = PrimaryMaterial;
                    viconBlock.SecondaryMaterial = SecondaryMaterial;
                    viconBlock.DecoMaterial = DecoMaterial;
                }
            }

            return result;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(world.GetBlock(base.CodeWithParts("north")), 1);
            BETextureSwappableBlock be = world.BlockAccessor.GetBlockEntity(pos) as BETextureSwappableBlock;
            if (be != null)
            {
                if (be.PrimaryMaterial != "default")
                {
                    stack.Attributes.SetString("PrimaryMaterial", be.PrimaryMaterial);
                }
                if (be.SecondaryMaterial != "default")
                {
                    stack.Attributes.SetString("SecondaryMaterial", be.SecondaryMaterial);
                }
                if (be.DecoMaterial != "default")
                {
                    stack.Attributes.SetString("DecoMaterial", be.DecoMaterial);
                }
            }
            return stack;
        }

        public MeshData GenMesh(ICoreClientAPI capi, string primaryMaterial, string secondaryMaterial, string decoMaterial, Shape shape = null, ITesselatorAPI tesselator = null)
        {
            if (tesselator == null)
            {
                tesselator = capi.Tesselator;
            }
            this.tmpTextureSource = tesselator.GetTextureSource(this, 0, false);
            if (shape == null)
            {
                shape = Vintagestory.API.Common.Shape.TryGet(capi, "vinconomy:shapes/" + this.Shape.Base.Path + ".json");
            }
            if (shape == null)
            {
                return null;
            }
            this.AtlasSize = capi.BlockTextureAtlas.Size;
            UpdateMaterials(primaryMaterial, secondaryMaterial, decoMaterial);
            MeshData mesh;
            tesselator.TesselateShape("viconStall", shape, out mesh, this, new Vec3f(this.Shape.rotateX, this.Shape.rotateY, this.Shape.rotateZ), 0, 0, 0, null, null);
            return mesh;
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null)
            {
                return;
            }
            object obj;
            if (capi.ObjectCache.TryGetValue(GUI_MESHES, out obj))
            {
                foreach (KeyValuePair<string, MultiTextureMeshRef> val in (obj as Dictionary<string, MultiTextureMeshRef>))
                {
                    val.Value.Dispose();
                }
                capi.ObjectCache.Remove(GUI_MESHES);
            }
        }

        public void UpdateMaterials(string primary, string secondary, string deco)
        {
            this.PrimaryMaterial = primary;
            this.SecondaryMaterial = secondary;
            this.DecoMaterial = deco;
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
        {
            if (Textures == null || Textures.Count == 0 || this.PrimaryMaterial == null)
            {
                return 0;
            }

            if (!Textures.TryGetValue(this.PrimaryMaterial, out var value))
            {
                value = Textures.First().Value;
            }

            if (value?.Baked == null)
            {
                return 0;
            }

            int num = capi.BlockTextureAtlas.GetRandomColor(value.Baked.TextureSubId, rndIndex);
            return num;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ITreeAttribute attrs = inSlot.Itemstack.Attributes;
            string primary = attrs.GetString("PrimaryMaterial");
            string secondary = attrs.GetString("SecondaryMaterial");
            string deco = attrs.GetString("DecoMaterial");
            if (primary != null)
            {
                dsc.AppendLine("Primary: " + Lang.Get("material-" + primary, Array.Empty<object>()));
            }
            if (secondary != null)
            {
                dsc.AppendLine("Secondary: " + Lang.Get("material-" + secondary, Array.Empty<object>()));
            }
            if (deco != null)
            {
                dsc.AppendLine("Decoration: " + Lang.Get("material-" + deco, Array.Empty<object>()));
            }
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);


        }

        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            if (player.Entity.Api.Side == EnumAppSide.Client)
            {
                BlockEntity entity = player.Entity.Api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
                if (entity != null)
                {
                    BEViconBase beVicon = (BEViconBase)entity;
                    if (beVicon != null)
                    {
                        //Small hack to update the texture before the block entity is removed and the particles are spaned.
                        UpdateMaterials(beVicon.PrimaryMaterial, beVicon.SecondaryMaterial, beVicon.DecoMaterial);
                    }
                }
            }

            return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt, counter);
        }

        //This is mostly just Vanilla "Lantern" code with a few redundant checks removed.
        protected void DoOnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            bool preventDefault = false;
            foreach (BlockBehavior blockBehavior in this.BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;
                blockBehavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventDefault)
                {
                    preventDefault = true;
                }
                if (handled == EnumHandling.PreventSubsequent)
                {
                    return;
                }
            }
            if (preventDefault)
            {
                return;
            }
            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack drops = this.OnPickBlock(world, pos);
                world.SpawnItemEntity(drops, new Vec3d((double)pos.X + 0.5, (double)pos.Y + 0.5, (double)pos.Z + 0.5), null);
                world.PlaySoundAt(this.Sounds.GetBreakSound(byPlayer), (double)pos.X, (double)pos.Y, (double)pos.Z, byPlayer, true, 32f, 1f);
            }



            if (this.EntityClass != null)
            {
                BETextureSwappableBlock entity = world.BlockAccessor.GetBlockEntity(pos) as BETextureSwappableBlock;
                if (entity != null)
                {
                    //Small hack to update the texture before the block entity is removed and the particles are spaned.
                    UpdateMaterials(entity.PrimaryMaterial, entity.SecondaryMaterial, entity.DecoMaterial);
                    
                    entity.OnBlockBroken(byPlayer);
                }
            }
            SpawnBlockBrokenParticles(pos);
            world.BlockAccessor.SetBlock(0, pos);
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            base.GetDropsForHandbook (handbookStack, forPlayer);
            return new BlockDropItemStack[]
            {
                new BlockDropItemStack(handbookStack, 1f)
            };
        }

        
    }
}
