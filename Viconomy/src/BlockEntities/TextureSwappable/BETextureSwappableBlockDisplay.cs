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
                UpdateMeshes();
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
                    string meshCacheKey = GetMeshCacheKey(Inventory[i].Itemstack);
                    MeshCache.Remove(meshCacheKey);
                }
            }

            UpdateMeshes();
            MarkDirty(redrawOnClient: true);
        }

        protected virtual void RedrawAfterReceivingTreeAttributes(IWorldAccessor worldForResolving)
        {
            if (worldForResolving.Side == EnumAppSide.Client && Api != null)
            {
                UpdateMeshes();
                MarkDirty(redrawOnClient: true);
            }
        }

        public virtual void UpdateMeshes()
        {
            if (Api != null && Api.Side != EnumAppSide.Server && DisplayedItems != 0)
            {
                for (int i = 0; i < DisplayedItems; i++)
                {
                    UpdateMesh(i);
                }

                tfMatrices = GenTransformationMatrices();
            }
        }

        protected virtual void UpdateMesh(int index)
        {
            if (Api != null && Api.Side != EnumAppSide.Server && !Inventory[index].Empty)
            {
                getOrCreateMesh(Inventory[index].Itemstack, index);
            }
        }

        protected virtual string GetMeshCacheKey(ItemStack stack)
        {
            if (stack == null)
                return null;

            if (stack.Collectible is IContainedMeshSource containedMeshSource)
            {
                return containedMeshSource.GetMeshCacheKey(stack);
            }

            return stack.Collectible.Code.ToString();
        }

        protected MeshData GetMesh(ItemStack stack)
        {
            string meshCacheKey = GetMeshCacheKey(stack);
            MeshCache.TryGetValue(meshCacheKey, out var value);
            return value;
        }

        protected virtual MeshData getOrCreateMesh(ItemStack stack, int index)
        {
            MeshData modeldata = GetMesh(stack);
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

            TransformModel(modeldata, stack);
           

            if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
            {
                
                modeldata.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), MathF.PI / 2f, 0f, 0f);
                modeldata.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
                modeldata.Translate(0f, -15f / 32f, 0f);
            }

            string meshCacheKey = GetMeshCacheKey(stack);
            MeshCache[meshCacheKey] = modeldata;
            return modeldata;
        }

        protected virtual void TransformModel(MeshData modeldata, ItemStack stack)
        {
            if (stack.Collectible.Attributes?[AttributeTransformCode].Exists ?? false)
            {
                ModelTransform modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                modelTransform.EnsureDefaultValues();
                modeldata.ModelTransform(modelTransform);
            }
            else if (AttributeTransformCode == "onShelfTransform" && (stack.Collectible.Attributes?["onDisplayTransform"].Exists ?? false))
            {
                ModelTransform modelTransform2 = stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();
                modelTransform2.EnsureDefaultValues();
                modeldata.ModelTransform(modelTransform2);
            }
            else if (stack.Collectible.Attributes?["groundStorageTransform"].Exists ?? false)
            {
                ModelTransform modelTransform = stack.Collectible.Attributes?["groundStorageTransform"].AsObject<ModelTransform>();
                modelTransform.EnsureDefaultValues();
                modeldata.ModelTransform(modelTransform);
            }
        }


        protected abstract float[][] GenTransformationMatrices();

        /**
         * Draws any items specific for the display of this block. If the method returns false, then the base OnTesslation will
         * not be executed and the block's default mesh will be used.
         */
        protected virtual void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            for (int i = 0; i < DisplayedItems; i++)
            {
                ItemSlot itemSlot = Inventory[i];
                if (!itemSlot.Empty && tfMatrices != null)
                {
                    mesher.AddMeshData(GetMesh(itemSlot.Itemstack), tfMatrices[i]);
                }
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            TesselateDisplayedItems(mesher, tessThreadTesselator);
            return base.OnTesselation(mesher, tessThreadTesselator);
        }
    }
}
