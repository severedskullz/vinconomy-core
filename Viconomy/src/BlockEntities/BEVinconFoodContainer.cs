using System;
using System.Collections.Generic;
using System.IO;
using Vinconomy.GUI;
using Vinconomy.Inventory.Impl;
using Vinconomy.Inventory.StallSlots;
using Vinconomy.Trading;
using Vinconomy.Trading.TradeHandlers;
using Vinconomy.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Vinconomy.BlockEntities
{
    public class BEVinconFoodContainer : BEVinconContainer
    {

        public override int ProductStacksPerSlot => 1;
        public override int BulkPurchaseAmount => 6;

        public override void ConfigureInventory()
        {
            inventory = new VinconMealInventory(this, null, Api, StallSlotCount, 1);
        }

        public MeshData GenMesh(int stallSlot)
        {
            if (Block == null) return null;
            MealStallSlot stall = inventory.GetStall<MealStallSlot>(stallSlot);
            if (stall.Meal != null)
            {
                ItemStack[] stacks = stall.GetMealContents();

                CookingRecipe recipe = stall.GetMatchingCookingRecipe(Api);
                Block potBlock = Api.World.GetBlock(new AssetLocation("game:claypot-blue-fired"));
                if (inventory.ChiselDecoSlot.Itemstack != null)
                {
                    return Api.ModLoader.GetModSystem<MealMeshCache>().GenMealInContainerMesh(potBlock, recipe, stacks, new Vec3f(0, 2.5f / 16f, 0));
                }
                else
                {
                    return Api.ModLoader.GetModSystem<MealMeshCache>().GenMealMesh(recipe, stacks, new Vec3f(0, 1 - (2.5f / 16f), 0));
                }
            }
            return null;
            
        }

        protected override void TesselateDisplayedItems(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);
            float y = inventory.ChiselDecoSlot.Itemstack != null ? 1 - (4.79f / 16f) : 0;
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -.25f : .25f;
                float z = (i >= 2) ? .25f : -.25f;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(Block.Shape.rotateY).Translate(x, y, z).Translate(-0.5f, 0f, -0.5f);

                MeshData data = GenMesh(i);
                if (data != null)
                {
                    data = data.Clone(); //TODO: Figure out why this affects pot meshes when I dont clone.
                    data.Rotate(origin, 0, GameMath.DEG2RAD * i * 90, 0);
                    mesher.AddMeshData(data, matrix.Values);
                }

            }
        }

        private bool CanMergeContents(ItemSlot handSlot, int stallSlot)
        {
            if (handSlot?.Itemstack == null) return false;

            if (handSlot.Itemstack.Block is IBlockMealContainer meal)
            {
                return VinUtils.IsMergableContents(meal.GetNonEmptyContents(Api.World,handSlot.Itemstack), inventory.GetStall<MealStallSlot>(stallSlot).GetMealContents());
            }

            return false;
        }

        private int TransferContentsFromStall(ItemSlot source, int stallSlot, int amount)
        {
            if (source?.Itemstack?.Block is IBlockMealContainer meal)
            {
                int curServings = (int)Math.Ceiling(meal.GetQuantityServings(Api.World,source.Itemstack)); // Only transfer whole servings, so count 1.3 servings as 2, for sake of math
                int capacity = source.Itemstack.Block.Attributes["servingCapacity"].AsInt();

                int toTransfer = Math.Min(amount, capacity - curServings);
                inventory.GetStall<MealStallSlot>(stallSlot).RemoveServings(toTransfer);
                ItemStack item = source.Itemstack;

                meal.SetContents(meal.GetRecipeCode(Api.World, item), item, meal.GetNonEmptyContents(Api.World, item), curServings + toTransfer);
                source.Itemstack.Attributes.RemoveAttribute("sealed");
                return toTransfer;
            }
            return 0;
        }

        
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            int slotIndex = GetStallSlotForSelectionIndex(blockSel.SelectionBoxIndex);

            bool sneakMod = byPlayer.Entity.Controls.Sneak;
            bool sprintMod = byPlayer.Entity.Controls.Sprint;

            ItemSlot hotbarslot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (CanAccess(byPlayer) && !VinconomyCoreSystem.ShouldForceCustomerScreen)
            {
                // If we are holding an empty food container, and sneaking => take from stall
                // Empty pots and crocks dont have the recipe code or contents set, so not the same as TransferContentsToStall()
                if (VinUtils.IsEmptyContainer(hotbarslot.Itemstack,Api) && sneakMod)
                {
                    TakeFromStallSlot(byPlayer, slotIndex, sprintMod, hotbarslot);
                }

                // If we are holding a filled container, and it matches
                // Filled containers already have the contents and recipe code set, so we just update the servings
                else if (CanMergeContents(hotbarslot, slotIndex)) {
                    // if we are holding Sneak, take from stall
                    if (sneakMod)
                    {
                        TransferContentsFromStall(hotbarslot, slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                    } else
                    {
                        TryPut(byPlayer, hotbarslot, slotIndex, sprintMod);
                    }
                } else if (sneakMod && TradingUtil.isMatchingItem(inventory.GetCurrencyForStallSlot(slotIndex).Itemstack, hotbarslot.Itemstack, Api.World)) {
                    RequestPurchaseItem(slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                } else {
                    OpenShopForPlayer(byPlayer, slotIndex);
                }
            }
            else
            {
                if (sneakMod)
                {
                    //Purchase the items.
                    RequestPurchaseItem(slotIndex, sprintMod ? BulkPurchaseAmount : 1);
                }
                else
                {
                    // Open the shop inventory for that block selection
                    OpenShopForPlayer(byPlayer, slotIndex);
                }
            }
            return true;
        }

        private void TakeFromStallSlot(IPlayer byPlayer, int slotIndex, bool ctrlMod, ItemSlot hotbarslot)
        {
            MealStallSlot stallSlot = inventory.GetStall<MealStallSlot>(slotIndex);
            ItemSlot firstStack = stallSlot.FindFirstNonEmptyStockSlot();
            if (firstStack == null) return;

            int capacity = hotbarslot.Itemstack.Block.Attributes["servingCapacity"].AsInt();
            int toTake = Math.Min(firstStack.StackSize, ctrlMod ? BulkPurchaseAmount : 1);
            toTake = Math.Min(capacity, toTake);

            int transfered = MealTradeHandler.TransferToMealBlock(byPlayer, hotbarslot, stallSlot.RecipeCode, stallSlot.GetMealContents(), toTake);
            if (transfered != toTake)
            {
                modSystem.Mod.Logger.Error("Somehow generated differing quantity of food. Wanted " + toTake + " but took " + transfered);
            }
            stallSlot.RemoveServings(toTake);
        }

        protected override bool TryAddItemToStall(IPlayer byPlayer, ItemSlot activeSlot, int stallSlot, bool bulk)
        {

            if (activeSlot.Itemstack?.Block is IBlockMealContainer)
            {
                return ((VinconMealInventory)inventory).AddMealToStall(stallSlot, activeSlot, bulk ? BulkPurchaseAmount : 1);
            }
            return false;
        }

        public override AggregatedSlots GetRequiredTools(IPlayer player, int stallSlot)
        {
            ServingCapacityAggregatedSlots aggregatedSlots = new ServingCapacityAggregatedSlots(Api);
            ItemStack[] mealStacks = inventory.GetStall<MealStallSlot>(stallSlot).GetMealContents();


            ItemSlot handItem = player.InventoryManager.ActiveHotbarSlot;
            if (CanHoldMeal(mealStacks, handItem.Itemstack))
            {
                aggregatedSlots.Add(handItem);
            }

            IInventory hotbarInv = player.InventoryManager.GetHotbarInventory();
            foreach (ItemSlot itemSlot in hotbarInv)
            {
                if (handItem == itemSlot || itemSlot.Itemstack == null) { continue; }
                if (CanHoldMeal(mealStacks, itemSlot.Itemstack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }

            IInventory characterInv = player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            foreach (ItemSlot itemSlot in characterInv)
            {
                if (handItem == itemSlot) { continue; }
                if (CanHoldMeal(mealStacks, itemSlot.Itemstack))
                {
                    aggregatedSlots.Add(itemSlot);
                }
            }
            return aggregatedSlots;
        }

        public override GenericTradeResult PurchaseItem(GenericTradeRequest request)
        {
            return MealTradeHandler.TryPurchaseItem(request);
        }

        private bool CanHoldMeal(ItemStack[] mealStacks, ItemStack dest)
        {
            return VinUtils.IsMealContainer(dest, Api) 
                && VinUtils.IsMergableContents(mealStacks, VinUtils.GetContainerContents(dest, Api));
        }

        public override int GetStallSlotForSelectionIndex(int index)
        {
            if (index > 0 && index < 5) return index-1;
            return 0;
        }


        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //Console.WriteLine(Api.Side + ": OnRecievedClientPacket " + packetid);
            //PrintClientMessage(player, Api.Side + ": OnRecievedClientPacket");
            IPlayerInventoryManager inventoryManager = player.InventoryManager;
            int stallSlot;
            int amount;
            switch (packetid)
            {
                case VinConstants.ACTIVATE_BLOCK:
                    using (MemoryStream memoryStream = new MemoryStream(data))
                    {
                        BinaryReader binaryReader = new BinaryReader(memoryStream);
                        stallSlot = binaryReader.ReadInt32();
                        amount = binaryReader.ReadInt32();
                    }
                    VinconMealInventory inv = (VinconMealInventory)inventory;

                    if (amount < 0)
                    {
                        inv.AddMealToStall(stallSlot, inv.TransferSlot, Math.Abs(amount));
                    } else
                    {
                        inv.RemoveMealFromStall(stallSlot, inv.TransferSlot, amount);
                    }

                        
                    break;
                default:
                    base.OnReceivedClientPacket(player, packetid, data);
                    break;
            }
        }

        protected override GuiDialogBlockEntity GetCustomerGui(string dialogTitle, int stallSelection)
        {
            return new GuiVinconMealStallCustomer(dialogTitle, this.Inventory, this.Pos, this.Api as ICoreClientAPI, stallSelection);
        }

        
        protected override GuiDialogBlockEntity GetOwnerGui(string dialogTitle, bool isOwner, int stallSelection)
        {
            return new GuiVinconFoodStallOwner(dialogTitle, this.Inventory, isOwner, this.Pos, this.Api as ICoreClientAPI, stallSelection);
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(BlockSelection selection, IPlayer forPlayer)
        {
            List<WorldInteraction> interactions = new List<WorldInteraction>();

            int index = GetStallSlotForSelectionIndex(selection.SelectionBoxIndex);
            ItemStack product = FindFirstNonEmptyStockStack(index);
            ItemSlot currency = GetCurrencyForStall(index);

            ItemSlot hotbar = forPlayer.InventoryManager.ActiveHotbarSlot;

            bool isEmptyPot = VinUtils.IsEmptyContainer(hotbar.Itemstack, Api);
            bool isMeal = VinUtils.IsMealContainer(hotbar.Itemstack, Api);

            if (Owner != forPlayer.PlayerUID || VinconomyCoreSystem.ShouldForceCustomerScreen)
            {
                if (currency.Itemstack != null && product != null)
                {
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = [currency.Itemstack]

                    });

                    ItemStack fiveStack = currency.Itemstack.Clone();
                    fiveStack.StackSize = 5 * fiveStack.StackSize;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase-bulk",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sneak", "sprint"],
                        Itemstacks = [fiveStack]
                    });
                }
            }
            else
            {
                
                if (isMeal)
                {
                    ItemStack helpStack = hotbar.Itemstack.Clone();
                    helpStack.StackSize = 1;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-add-servings",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = [helpStack]
                    });

                    ItemStack helpSlotStack = helpStack.Clone();
                    helpSlotStack.StackSize = BulkPurchaseAmount;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-add-bulk-servings",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sprint"],
                        Itemstacks = [helpSlotStack]
                    });
                }

                if (isMeal || isEmptyPot)
                {
                    ItemStack helpStack = hotbar.Itemstack.Clone();
                    helpStack.StackSize = 1;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-remove-servings",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sneak"],
                        Itemstacks = [helpStack]
                    });

                    ItemStack helpSlotStack = helpStack.Clone();
                    helpSlotStack.StackSize = BulkPurchaseAmount;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-remove-bulk-servings",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sneak", "sprint"],
                        Itemstacks = [helpSlotStack]
                    });
                }

                if (currency.Itemstack != null)
                {
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = [currency.Itemstack]

                    });

                    ItemStack fiveStack = currency.Itemstack.Clone();
                    fiveStack.StackSize = 5 * fiveStack.StackSize;
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase-bulk",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCodes = ["sneak", "sprint"],
                        Itemstacks = [fiveStack]
                    });
                }
                

            }

            interactions.Add(new WorldInteraction
            {
                ActionLangCode = "vinconomy:stall-open-menu",
                MouseButton = EnumMouseButton.Right,
                HotKeyCode = null
            });

            return interactions.ToArray();
        }
        

    }
}
