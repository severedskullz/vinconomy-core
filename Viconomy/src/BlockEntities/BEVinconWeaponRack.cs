using Vintagestory.API.Client;
using Vinconomy.Filters;
using Vintagestory.API.Common;
using Vinconomy.Inventory.Impl;
using System;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Vinconomy.BlockEntities
{
    public class BEVinconWeaponRack : BEVinconContainer
    {
        public override string AttributeTransformCode => "toolrackTransform";

        public override int StallSlotCount => 3;

        public override void ConfigureInventory()
        {
            VinconItemInventory inv = new VinconItemInventory(this, null, null, StallSlotCount, ProductStacksPerSlot);
            inv.SetSlotFilter(0, VinconomyFilters.IsToolOrWeapon);
            inv.SetSlotBackground(0, "vicon-toolrack");
            inv.SetSlotFilter(1, VinconomyFilters.IsShield);
            inv.SetSlotBackground(1, "vicon-shield");
            inv.SetSlotFilter(2, VinconomyFilters.IsToolOrWeapon);
            inv.SetSlotBackground(2, "vicon-toolrack");
            inventory = inv;
        }

        protected override MeshData getOrCreateMesh(ItemSlot slot, int index)
        {

            //TODO: Im sick of fighting with matricies. If it works, it works... Dont cache because then everything breaks again


            //MeshData modeldata = GetMesh(stack);
            //if (modeldata != null)
            //{
            //    return modeldata;
            //}

            MeshData modeldata = null;
            ItemStack stack = slot.Itemstack;
            if (stack.Collectible is IContainedMeshSource containedMeshSource)
            {
                modeldata = containedMeshSource.GenMesh(slot, capi.BlockTextureAtlas, Pos);
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
            Vec3f origin = new Vec3f(0.5f,0.5f,0.5f);
            modeldata.Rotate(origin, 0, 90 * GameMath.DEG2RAD, 0);
            if (index == 0) // Right Side
            {
                modeldata.Rotate(origin, 0, 90 * GameMath.DEG2RAD, 0);
                modeldata.Rotate(origin, 90 * GameMath.DEG2RAD,0,0);
                modeldata.Rotate(origin, 0, 0, 225 * GameMath.DEG2RAD);
                modeldata.Translate(0.25f, 0.25f, 0);
            } else if (index == 1) // Shield
            {
                modeldata.Rotate(origin, 95*GameMath.DEG2RAD, 0, 0);
                modeldata.Translate(0, 1.15f, -.35f);
            } else if (index == 2) // Left Side
            {
                modeldata.Rotate(origin, 0, 270 * GameMath.DEG2RAD, 0);
                modeldata.Rotate(origin, -90 * GameMath.DEG2RAD, 0, 0);
                modeldata.Rotate(origin, 0, 0, 135 * GameMath.DEG2RAD);
                modeldata.Translate(-0.25f, 0.25f, 0);
            }

            //string meshCacheKey = GetMeshCacheKey(stack) + index;
            //MeshCache[meshCacheKey] = modeldata;
            return modeldata;
        }

        protected override void TransformModel(MeshData modeldata, ItemStack stack)
        {
            if (stack.Collectible.Attributes?[AttributeTransformCode].Exists ?? false)
            {
                ModelTransform modelTransform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
                modelTransform.EnsureDefaultValues();
                modeldata.ModelTransform(modelTransform);
            }
        }

        protected override float[][] GenTransformationMatrices()
        {
            float[][] tfMatrices = new float[3][];
            ItemSlot wepLeftItem = inventory.FindFirstNonEmptyStockSlot(0);
            if (wepLeftItem != null)
            {

                Matrixf wepLeft = new Matrixf()
                    .Translate(0.5f, 0.5f, 0.5f)
                    .RotateYDeg(Block.Shape.rotateY)
                    .Translate(0, 0, -0.4f)
                    .Scale(0.6f, 0.6f, 0.6f)
                    .Translate(-0.5f, -0.5f, -0.5f);
                tfMatrices[0] = wepLeft.Values;
            }


            ItemSlot shieldItem = inventory.FindFirstNonEmptyStockSlot(1);
            if (shieldItem != null && shieldItem.Itemstack.ItemAttributes.KeyExists("toolrackTransform") || true)
            {
                Matrixf shield = new Matrixf()
                    .Translate(0.5f, 0f, 0.5f)
                    .RotateYDeg(Block.Shape.rotateY +180)
                    .Scale(0.6f, 0.6f, 0.6f)
                    .Translate(-0.5f, -0.5f, 0.5f);

                tfMatrices[1] = shield.Values;
            }

            ItemSlot wepRightItem = inventory.FindFirstNonEmptyStockSlot(2);
            if (wepRightItem != null)
            {
                Matrixf wepRight = new Matrixf()
                    .Translate(0.5f, 0.5f, 0.5f)
                    .RotateYDeg(Block.Shape.rotateY)
                    .Translate(0,0,-0.4f)
                    .Scale(0.6f, 0.6f, 0.6f)
                    .Translate(-0.5f, -0.5f, -0.5f);

                tfMatrices[2] = wepRight.Values;
            }
            return tfMatrices;
        }
    }

}
