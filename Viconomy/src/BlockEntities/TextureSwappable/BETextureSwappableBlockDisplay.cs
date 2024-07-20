using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities.TextureSwappable
{
    public abstract class BETextureSwappableBlockDisplay : BETextureSwappableBlockContainer, ITexPositionSource
    {
        protected CollectibleObject nowTesselatingObj;

        protected Shape nowTesselatingShape;

        protected ICoreClientAPI capi;

        protected float[][] tfMatrices;

        public virtual string ClassCode => InventoryClassName;

        public virtual int DisplayedItems => Inventory.Count;

        protected bool ShouldRenderDisplayedItems;

        public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

        public virtual string AttributeTransformCode => "onDisplayTransform";

        public virtual TextureAtlasPosition this[string textureCode]
        {
            get
            {
                IDictionary<string, CompositeTexture> dictionary;
                if (!(nowTesselatingObj is Item item))
                {
                    dictionary = (nowTesselatingObj as Block).Textures;
                }
                else
                {
                    IDictionary<string, CompositeTexture> textures = item.Textures;
                    dictionary = textures;
                }

                IDictionary<string, CompositeTexture> dictionary2 = dictionary;
                AssetLocation value = null;
                if (dictionary2.TryGetValue(textureCode, out var value2))
                {
                    value = value2.Baked.BakedName;
                }

                if (value == null && dictionary2.TryGetValue("all", out value2))
                {
                    value = value2.Baked.BakedName;
                }

                if (value == null)
                {
                    nowTesselatingShape?.Textures.TryGetValue(textureCode, out value);
                }

                if (value == null)
                {
                    value = new AssetLocation(textureCode);
                }

                return getOrCreateTexPos(value);
            }
        }

        protected Dictionary<string, MeshData> MeshCache => ObjectCacheUtil.GetOrCreate(Api, "meshesDisplay-" + ClassCode, () => new Dictionary<string, MeshData>());

        protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
        {
            TextureAtlasPosition texPos = capi.BlockTextureAtlas[texturePath];
            if (texPos == null && !capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texPos))
            {
                capi.World.Logger.Warning("For render in block " + Block.Code?.ToString() + ", item {0} defined texture {1}, no such texture found.", nowTesselatingObj.Code, texturePath);
                return capi.BlockTextureAtlas.UnknownTexturePosition;
            }

            return texPos;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            capi = api as ICoreClientAPI;
            if (capi != null)
            {
                updateMeshes();
                api.Event.RegisterEventBusListener(OnEventBusEvent);
            }
        }

        private void OnEventBusEvent(string eventname, ref EnumHandling handling, IAttribute data)
        {
            if (eventname != "genjsontransform" && eventname != "oncloseedittransforms" && eventname != "onapplytransforms" || Inventory.Empty)
            {
                return;
            }

            for (int i = 0; i < DisplayedItems; i++)
            {
                if (!Inventory[i].Empty)
                {
                    string meshCacheKey = getMeshCacheKey(Inventory[i].Itemstack);
                    MeshCache.Remove(meshCacheKey);
                }
            }

            updateMeshes();
            MarkDirty(redrawOnClient: true);
        }

        protected virtual void RedrawAfterReceivingTreeAttributes(IWorldAccessor worldForResolving)
        {
            if (worldForResolving.Side == EnumAppSide.Client && Api != null)
            {
                updateMeshes();
                MarkDirty(redrawOnClient: true);
            }
        }

        public virtual void updateMeshes()
        {
            if (Api != null && Api.Side != EnumAppSide.Server && DisplayedItems != 0)
            {
                for (int i = 0; i < DisplayedItems; i++)
                {
                    updateMesh(i);
                }

                tfMatrices = genTransformationMatrices();
            }
        }

        protected virtual void updateMesh(int index)
        {
            if (Api != null && Api.Side != EnumAppSide.Server && !Inventory[index].Empty)
            {
                getOrCreateMesh(Inventory[index].Itemstack, index);
            }
        }

        protected virtual string getMeshCacheKey(ItemStack stack)
        {
            if (stack.Collectible is IContainedMeshSource containedMeshSource)
            {
                return containedMeshSource.GetMeshCacheKey(stack);
            }

            return stack.Collectible.Code.ToString();
        }

        protected MeshData getMesh(ItemStack stack)
        {
            string meshCacheKey = getMeshCacheKey(stack);
            MeshCache.TryGetValue(meshCacheKey, out var value);
            return value;
        }

        protected virtual MeshData getOrCreateMesh(ItemStack stack, int index)
        {
            MeshData modeldata = getMesh(stack);
            if (modeldata != null)
            {
                return modeldata;
            }

            if (stack.Collectible is IContainedMeshSource containedMeshSource)
            {
                modeldata = containedMeshSource.GenMesh(stack, capi.BlockTextureAtlas, Pos);
            }

            if (modeldata == null)
            {
                ICoreClientAPI coreClientAPI = Api as ICoreClientAPI;
                if (stack.Class == EnumItemClass.Block)
                {
                    modeldata = coreClientAPI.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                }
                else
                {
                    nowTesselatingObj = stack.Collectible;
                    nowTesselatingShape = null;
                    if (stack.Item.Shape?.Base != null)
                    {
                        nowTesselatingShape = coreClientAPI.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                    }

                    coreClientAPI.Tesselator.TesselateItem(stack.Item, out modeldata, this);
                    modeldata.RenderPassesAndExtraBits.Fill((short)2);
                }
            }

            JsonObject attributes = stack.Collectible.Attributes;
            if (attributes != null && attributes[AttributeTransformCode].Exists)
            {
                ModelTransform modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                modelTransform.EnsureDefaultValues();
                modeldata.ModelTransform(modelTransform);
            }
            else if (AttributeTransformCode == "onshelfTransform")
            {
                JsonObject attributes2 = stack.Collectible.Attributes;
                if (attributes2 != null && attributes2["onDisplayTransform"].Exists)
                {
                    ModelTransform modelTransform2 = stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();
                    modelTransform2.EnsureDefaultValues();
                    modeldata.ModelTransform(modelTransform2);
                }
            }

            if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
            {
                modeldata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), MathF.PI / 2f, 0f, 0f);
                modeldata.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
                modeldata.Translate(0f, -15f / 32f, 0f);
            }

            string meshCacheKey = getMeshCacheKey(stack);
            MeshCache[meshCacheKey] = modeldata;
            return modeldata;
        }

        protected abstract float[][] genTransformationMatrices();

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (ShouldRenderDisplayedItems) {
                for (int i = 0; i < DisplayedItems; i++)
                {
                    ItemSlot itemSlot = Inventory[i];
                    if (!itemSlot.Empty && tfMatrices != null)
                    {
                        mesher.AddMeshData(getMesh(itemSlot.Itemstack), tfMatrices[i]);
                    }
                }
            }

            return base.OnTesselation(mesher, tessThreadTesselator);
        }
    }
}
