using Commercially.Common.Interfaces;
using Commercially.Common.ModSystems;
using Commercially.Common.Util;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.BlockEntityBehaviors.DisplayProviders
{
    public abstract class BaseDisplayContentsBehavior : BlockEntityBehavior, IShapeTesselator
    {
        protected CommerciallyModSystem CommerciallyCore;
        protected IStallInventoryProvider _InventoryProvider;
        protected string AttributeTransformCode;
        protected bool BypassShelvableAttributes;
        protected bool ShouldRenderInventory = true;
        public virtual string ClassCode => _InventoryProvider.Inventory.ClassName;
        protected Dictionary<string, MeshData> MeshCache => ObjectCacheUtil.GetOrCreate(Api, "stallMeshesDisplay", () => new Dictionary<string, MeshData>());
        public TransformationData[] TfData { get; protected set; }

        protected CollectibleObject nowTesselatingObj = null;
        protected Shape nowTesselatingShape = null;
        public Size2i AtlasSize => ((ICoreClientAPI)Api)?.BlockTextureAtlas.Size;

        //Copy/Pasted from Tyron, so hopefully this wont break with every goddamn update like it has in the past.
        public virtual TextureAtlasPosition this[string textureCode]
        {
            get
            {
                //if (texSource != null) return texSource[textureCode];

                IDictionary<string, CompositeTexture> textures = nowTesselatingObj is Item item ? item.Textures : (nowTesselatingObj as Block).Textures;
                AssetLocation texturePath = null;

                // Prio 1: Get from collectible textures
                if (textures.TryGetValue(textureCode, out CompositeTexture tex))
                {
                    texturePath = tex.Baked.BakedName;
                }

                // Prio 2: Get from collectible textures, use "all" code
                if (texturePath == null && textures.TryGetValue("all", out tex))
                {
                    texturePath = tex.Baked.BakedName;
                }

                // Prio 3: Get from currently tesselating shape
                if (texturePath == null)
                {
                    nowTesselatingShape?.Textures.TryGetValue(textureCode, out texturePath);
                }

                // Prio 4: The code is the path
                if (texturePath == null)
                {
                    texturePath = new AssetLocation(textureCode);
                }

                return GetOrCreateTexPos(texturePath);
            }
        }

        public BaseDisplayContentsBehavior(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            _InventoryProvider = this.GetComponent<IStallInventoryProvider>();
            CommerciallyCore = api.ModLoader.GetModSystem<CommerciallyModSystem>();

            TransformationData gTransform = properties["globalTransform"]?.AsObject(new TransformationData());


            TfData = new TransformationData[_InventoryProvider.StallCount];
            JsonObject[] displayTransforms = properties["displayTransforms"]?.AsArray();
            for (int i = 0; i < _InventoryProvider.StallCount; i++)
            {
                TransformationData tdata;
                if (displayTransforms != null && i < displayTransforms.Length)
                {
                    JsonObject prop = displayTransforms[i];
                    tdata = prop.AsObject(new TransformationData());

                }
                else
                {
                    tdata = new TransformationData();
                    tdata.Reset();
                }

                if (gTransform != null)
                {
                    tdata.globalTransform = gTransform;
                }

                tdata.preRotate += (float)((Block.Shape.rotateY));
                TfData[i] = tdata;



            }
            AttributeTransformCode = properties["displayTransformType"]?.AsString();
            //api.Event.RegisterEventBusListener()
        }

        public void SetNowTesselatingObj(CollectibleObject collectible)
        {
            nowTesselatingObj = collectible;
            nowTesselatingShape = null;
        }

        public void SetNowTesselatingShape(Shape shape)
        {
            nowTesselatingShape = shape;
            nowTesselatingObj = null;
        }

        protected MeshData GetDefaultMesh(ItemStack stack)
        {
            MeshData mesh;
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (stack.Class == EnumItemClass.Block)
            {
                mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
            }
            else
            {
                nowTesselatingObj = stack.Collectible;
                nowTesselatingShape = null;
                if (stack.Item.Shape?.Base != null)
                {
                    nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
                }
                capi.Tesselator.TesselateItem(stack.Item, out mesh, this);

                mesh.RenderPassesAndExtraBits.Fill((short)EnumChunkRenderPass.BlendNoCull);
            }

            return mesh;
        }


        protected TextureAtlasPosition GetOrCreateTexPos(AssetLocation texturePath)
        {
            ICoreClientAPI capi = (ICoreClientAPI)Api;
            TextureAtlasPosition texPos = capi.BlockTextureAtlas[texturePath];
            if (texPos == null && !capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texPos))
            {
                capi.World.Logger.Warning("For render in block " + Block.Code?.ToString() + ", item {0} defined texture {1}, no such texture found.", nowTesselatingObj.Code, texturePath);
                return capi.BlockTextureAtlas.UnknownTexturePosition;
            }

            return texPos;
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            TesselateDisplayedItems(mesher, tessThreadTesselator);
            return false;
        }

        protected virtual void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (mesher == null)
                return;


            if (ShouldRenderInventory)
            {
                MeshData mesh = null;
                ItemSlot slot = null;
                for (int i = 0; i < _InventoryProvider.StallCount; i++)
                {
                    try
                    {
                        BaseStallSlot stall = _InventoryProvider.GetStallSlot(i);
                        slot = stall.Product;

                        if (slot?.Itemstack != null && stall.GetNumPurchasesRemaining() > 0 && TfData != null)
                        {
                            mesh = GetOrCreateMesh(slot, i);
                            if (mesh != null)
                            {
                                float[] matricies = TfData[i].BuildMatrix();
                                mesher.AddMeshData(mesh, matricies);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        CommerciallyCore.Mod.Logger.Error($"Had some trouble rendering mesh in a stall @ {Pos.X} {Pos.Y} {Pos.Z} for slot {i}. Exception was {e.Message}");
                    }

                }
            }

        }

        protected virtual MeshData GetOrCreateMesh(ItemSlot slot, int index)
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;

            MeshData mesh = GetMesh(slot, index);
            if (mesh != null) return mesh;

            mesh = GenMesh(slot, index);

            if (mesh != null)
            {
                string key = GetMeshCacheKey(slot);
                MeshCache[key] = mesh;
            }

            return mesh;
        }

        protected virtual MeshData GenMesh(ItemSlot slot, int index)
        {
            MeshData mesh = null;
            ICoreClientAPI capi = Api as ICoreClientAPI;

            var stack = slot.Itemstack;
            CompositeShape customShape = stack.Collectible.Attributes?["displayedShape"].AsObject<CompositeShape>(null, stack.Collectible.Code.Domain);
            if (customShape != null)
            {
                string customkey = "displayedShape-" + customShape.ToString();
                mesh = ObjectCacheUtil.GetOrCreate(capi, customkey, () =>
                    capi.TesselatorManager.CreateMesh(
                        "displayed item shape",
                        customShape,
                        (shape, name) => new ContainedTextureSource(capi, capi.BlockTextureAtlas, shape.Textures, string.Format("For displayed item {0}", stack.Collectible.Code)),
                        null
                ));
            }
            else
            {
                IContainedMeshSource meshSource = stack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();

                if (meshSource != null)
                {
                    mesh = meshSource.GenMesh(slot, capi.BlockTextureAtlas, Pos);
                }
            }

            if (mesh == null)
            {
                mesh = GetDefaultMesh(stack);
            }

            ApplyDefaultTranforms(stack, mesh);

            return mesh;
        }

        /*
        protected virtual float[][] GenTransformationMatrices()
        {
            int stallCount = _InventoryProvider.StallCount;
            float[][] tfMatrices = new float[stallCount][];
            for (int index = 0; index < stallCount; index++)
            {
                TransformationData data = TfData[index];
                tfMatrices[index] = data.BuildMatrix();
            }
            return tfMatrices;
        }
        */

        protected virtual string GetMeshCacheKey(ItemSlot slot)
        {
            ItemStack stack = slot.Itemstack;
            if (stack == null)
                return null;

            if (stack.Collectible is IContainedMeshSource containedMeshSource)
            {
                return containedMeshSource.GetMeshCacheKey(slot);
            }

            return stack.Collectible.Code.ToString();
        }

        protected virtual MeshData GetMesh(ItemSlot stack, int stallSlot)
        {
            string meshCacheKey = GetMeshCacheKey(stack);
            MeshCache.TryGetValue(meshCacheKey, out var value);
            return value;
        }



        protected virtual void ApplyDefaultTranforms(ItemStack stack, MeshData mesh)
        {
            ModelTransform transform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
            if (AttributeTransformCode == "onshelfTransform") // special logic because shelves a little more complicated
            {
                transform = stack.Collectible.GetCollectibleInterface<IShelvable>()?.GetOnShelfTransform(stack) ?? transform;
                transform ??= stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();
            }
            if (transform != null)
            {
                transform.EnsureDefaultValues();
                mesh.ModelTransform(transform);
            }

            if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
            {
                mesh.Rotate(GameMath.PIHALF, 0, 0);
                mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.33f, 0.33f, 0.33f);
                //mesh.Translate(0, -7.5f / 16f, 0f);
            }
            else if (stack.Class == EnumItemClass.Block)
            {
                mesh.Scale(new Vec3f(0.5f, 0, 0.5f), 0.33f, 0.33f, 0.33f);
            }
        }

        public BlockEntity GetBlockEntity()
        {
            return this.Blockentity;
        }
    }
}
