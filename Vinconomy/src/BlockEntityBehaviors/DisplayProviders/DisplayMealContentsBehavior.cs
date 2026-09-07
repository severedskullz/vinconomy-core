using Commercially.Vinconomy.Inventory.StallSlots;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.BlockEntityBehaviors.DisplayProviders
{
    public class DisplayMealContentsBehavior : BaseDisplayContentsBehavior
    {


        public DisplayMealContentsBehavior(BlockEntity blockentity) : base(blockentity)
        {
        }


        protected override MeshData GenMesh(ItemSlot stack, int stallSlot)
        {
            MealMeshCache mesher = (this.Api as ICoreClientAPI).ModLoader.GetModSystem<MealMeshCache>(true);
            Block block = stack.Itemstack?.Block;
            MealStallSlot mealStall = _InventoryProvider.GetStallSlot<MealStallSlot>(stallSlot);
            string recipe = mealStall.GetRecipeCode();
            ItemStack[] contents = mealStall.GetProductContents();

            CookingRecipe fromRecipe = Api.GetCookingRecipe(recipe);
            MeshData mesh = mesher.GenMealMesh(fromRecipe, contents);
            if (mesh != null)
                ApplyDefaultTranforms(stack.Itemstack, mesh);

            return mesh;
        }
        
    }
}
