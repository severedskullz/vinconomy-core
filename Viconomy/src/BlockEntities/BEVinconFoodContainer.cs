using Viconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVinconFoodContainer : BEVinconContainer
    {
        public float QuantityServings { get; set; }
        public string RecipeCode { get; set; }

        internal BlockCookedContainer ownBlock;

        MeshData currentMesh;

        public CookingRecipe FromRecipe
        {
            //get { return Api.GetCookingRecipe(RecipeCode); }
            get { return Api.GetCookingRecipe("vegetablestew"); }
        }
        public string GetRecipeCode(IWorldAccessor world, ItemStack containerStack)
        {
            return containerStack.Attributes.GetString("recipeCode");
        }

        public MeshData GenMesh()
        {
            if (Block == null) return null;
            ItemStack[] stacks = GetFakeItemStack();
            if (stacks == null || stacks.Length == 0) return null;

            ICoreClientAPI capi = Api as ICoreClientAPI;
            //TODO: Cache Mesh
            return capi.ModLoader.GetModSystem<MealMeshCache>().GenMealMesh(FromRecipe, stacks, new Vec3f(0, 1 - (2.5f / 16f), 0));
        }

        private ItemStack[] GetFakeItemStack()
        {
            ItemStack[] stacks = new ItemStack[4];
            stacks[0] = VinUtils.ResolveBlockOrItem(Api, "game:vegetable-carrot", 1);
            stacks[1] = VinUtils.ResolveBlockOrItem(Api, "game:vegetable-onion", 1);
            stacks[2] = VinUtils.ResolveBlockOrItem(Api, "game:vegetable-turnip", 1);
            stacks[3] = VinUtils.ResolveBlockOrItem(Api, "game:vegetable-parsnip", 1);
            return stacks;
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
            for (int i = 0; i < 4; i++)
            {
                float left = -.25f;
                float right = .25f;
                float x = (i % 2 == 0) ? left : right;
                float y = 0f;
                float z = (i >= 2) ? left : right;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f);

                MeshData data = GenMesh();
                data.Rotate(origin, 0, GameMath.DEG2RAD * i * 90, 0);
                mesher.AddMeshData(data, matrix.Values);
            }

            return false;
        }



        /*
        public void ServeIntoBowl(Block selectedBlock, BlockPos pos, ItemSlot potslot, IWorldAccessor world)
        {
            if (world.Side == EnumAppSide.Client) return;

            string code = selectedBlock.Attributes["mealBlockCode"].AsString();
            Block mealblock = Api.World.GetBlock(new AssetLocation(code));

            world.BlockAccessor.SetBlock(mealblock.BlockId, pos);

            IBlockEntityMealContainer bemeal = Api.World.BlockAccessor.GetBlockEntity(pos) as IBlockEntityMealContainer;
            if (bemeal == null) return;

            if (tryMergeServingsIntoBE(bemeal, potslot)) return;

            bemeal.RecipeCode = GetRecipeCode(world, potslot.Itemstack);

            ItemStack[] myStacks = GetNonEmptyContents(Api.World, potslot.Itemstack);
            for (int i = 0; i < myStacks.Length; i++)
            {
                bemeal.inventory[i].Itemstack = myStacks[i].Clone();
            }

            float quantityServings = GetServings(world, potslot.Itemstack);
            float servingsToTransfer = Math.Min(quantityServings, selectedBlock.Attributes["servingCapacity"].AsFloat(1));

            bemeal.QuantityServings = servingsToTransfer;

            SetServingsMaybeEmpty(world, potslot, quantityServings - servingsToTransfer);

            potslot.MarkDirty();
            bemeal.MarkDirty(true);
        }
        

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot heldItem = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (heldItem?.Itemstack?.Collectible.Attributes?.IsTrue("mealContainer") == true)
            {
                if (heldItem.Itemstack.Block is BlockContainer)
                {
                    doStuff(heldItem);
                    //BlockContainer bc = heldItem.Itemstack.Block as BlockContainer;
                    //bc.SetContents(heldItem.Itemstack, GetFakeItemStack());
                    //heldItem.MarkDirty();
                }
            }
            return true;
        }
        */

        protected override int GetStallSlotForSelectionIndex(int index)
        {
            if (index > 0 && index < 5) return index-1;
            return -1;
        }

        private void doStuff(ItemSlot bowlSlot)
        {
            ItemStack[] stacks = GetFakeItemStack();
            string code = bowlSlot.Itemstack.Block.Attributes["mealBlockCode"].AsString();
            if (code == null) return;
            Block mealblock = Api.World.GetBlock(new AssetLocation(code));

            float servingsToTransfer = 1;//Math.Min(quantityServings, servingCapacity);

            ItemStack stack = new ItemStack(mealblock);
            (mealblock as IBlockMealContainer).SetContents("vegetablestew", stack, stacks, servingsToTransfer);

            //SetServingsMaybeEmpty(Api.World, potslot, quantityServings - servingsToTransfer);
            //potslot.Itemstack.Attributes.RemoveAttribute("sealed");
            //potslot.MarkDirty();

            bowlSlot.Itemstack = stack;
            bowlSlot.MarkDirty();
        }

        public virtual ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string defaultDomain)
        {
            CollectibleObject collObj;
            var loc = AssetLocation.Create(stackAttr.GetString("code"), defaultDomain);
            if (stackAttr.GetString("type") == "item")
            {
                collObj = world.GetItem(loc);
            }
            else
            {
                collObj = world.GetBlock(loc);
            }

            ItemStack stack = new ItemStack(collObj, (int)stackAttr.GetDecimal("quantity", 1));
            var attr = (stackAttr["attributes"] as TreeAttribute)?.Clone();
            if (attr != null) stack.Attributes = attr;

            return stack;
        }


    }
}
