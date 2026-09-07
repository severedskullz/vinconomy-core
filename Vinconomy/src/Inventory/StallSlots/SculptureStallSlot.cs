using Commercially.Common.Inventory.Slots;
using Commercially.Vinconomy.Inventory.Slots;
using Commercially.Vinconomy.Trading;
using System;
using Vinconomy.Filters;
using Vinconomy.Inventory.Slots;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Commercially.Vinconomy.Inventory.StallSlots
{
    public class SculptureStallSlot : BaseStallSlot
    {
        ToggledStockItemSlot[] Slots;
        public int SculptureHorizontalSize = MaxSculptureSize;
        public int SculptureVerticalSize = MaxSculptureSize;
        public string SculptureName = "Sculpture";
        public const int MaxSculptureSize = 5;

        public SculptureStallSlot(VinconBaseInventory inventory, int stallSlot) : base(inventory, stallSlot)
        {
            int slotLength = (int)Math.Pow(MaxSculptureSize, 3);
            Slots = new ToggledStockItemSlot[slotLength];
            for (int i = 0; i < slotLength; i++)
            {
                Slots[i] = new ToggledStockItemSlot(inventory, stallSlot, i)
                {
                    Filter = CommonFilters.IsBlock,
                };
            }
        }

        public override ItemSlot this[int slotId] {
            get
            {
                if (slotId == 0) return Currency;
                else if (slotId == 1) return Product;
                else return Slots[slotId - 2];
            }
            set
            {
                if (slotId == 0) Currency = (CurrencySlot) value;
                else if (slotId == 1) Product = (ProductSlot) value;
                else Slots[slotId - 2] = (ToggledStockItemSlot) value;
            }
        }

        public override bool IsInitialized => true;

        public override ItemSlot[] GetStockSlots()
        { 
            return Slots;
        }

        public void TransferProdutToPlayer(TradeResult result)
        {
            if (result.ProductStacks.TotalCount == 0) return;

            IPlayer player = result.Request.Customer;
            AssetLocation sound = null;
            while (result.ProductStacks.CanRemoveStack())
            {
                ItemStack stack = result.ProductStacks.RemoveStack();

                if (stack != null)
                {
                    this.Inventory.modSystem.Mod.Logger.Debug($"Adding {stack.StackSize}x {stack} product to Parent");
                    if (stack.Block?.Sounds?.Place.Location != null)
                    {
                        sound = stack.Block?.Sounds?.Place.Location;
                    }

                    player.InventoryManager.TryGiveItemstack(stack, true);
                    if (stack.StackSize > 0)
                    {
                        result.Request.Api.World.SpawnItemEntity(stack, player.Entity.Pos.XYZ.Add(0.5), null);
                    }
                }
            }

            result.Request.Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), result.Request.Customer.Entity, result.Request.Customer, true, 16f, 1f);
        }

        public  AggregatedSlots GetProducts()
        {
            ICoreAPI api = Inventory.Api;
            SculptureAggregatedSlots slots = new SculptureAggregatedSlots(api);

            for (int layer = 0; layer < SculptureVerticalSize; layer++)
            {
                for (int y = 0; y < SculptureHorizontalSize; y++)
                {
                    for (int x = 0; x < SculptureHorizontalSize; x++)
                    {
                        int layerOffset = layer * (SculptureHorizontalSize * SculptureHorizontalSize);
                        int yOffset = y * SculptureHorizontalSize;
                        int index = layerOffset + yOffset + x;

                        ToggledSlot slot = Slots[index];

                        // Only add enabled slots to the aggregated slots - EVEN IF the slot is empty. This would mean we dont have any product and the AggregatedSlot's total count should be set to 0.
                        if (slot.Enabled) 
                        {
                            slots.Add(slot);
                        }
                    }
                }
            }
            return slots;
        }

        public ToggledStockItemSlot GetSlotForGrid(int layer, int x, int y)
        {
            int layerOffset = layer * (MaxSculptureSize * MaxSculptureSize);
            int yOffset = y * MaxSculptureSize;
            int index = layerOffset + yOffset + x;
            return Slots[index];
        }


        public override AggregatedStacks ExtractProduct(int amount, int numPurchases, bool isAdminShop)
        {
            ItemStack bundle = GenNewSculptureBundle();

            if (!isAdminShop)
            {
                ItemSlot[] products = GetStockSlots();
                for (int layer = 0; layer < SculptureVerticalSize; layer++)
                {
                    for (int y = 0; y < SculptureHorizontalSize; y++)
                    {
                        for (int x = 0; x < SculptureHorizontalSize; x++)
                        {
                            ToggledStockItemSlot slot = GetSlotForGrid(layer, x, y);
                            if (!slot.Empty && slot.Enabled)
                            {
                                ItemStack takenStack = slot.TakeOut(numPurchases);
                                if (takenStack != null)
                                {
                                    this.Inventory.modSystem.Mod.Logger.Debug($"Took out {takenStack.StackSize}x {takenStack} product from Product Stacks");
                                    slot.MarkDirty();
                                }
                            }

                        }
                    }
                }
            }

            AggregatedStacks productStacks = new AggregatedStacks();
            
            if (numPurchases == 1)
                productStacks.Add(bundle);
            else
            {
                for (int i = 0; i < numPurchases; i++)
                {
                    // Max stack size for bundles is 1, so prevent getting more than 1 in a stack if they purchase in bulk.
                    productStacks.Add(bundle.Clone());
                }
            }
            return productStacks;
        }

        public ItemStack GenStubbedBundle()
        {
            ItemStack stack = new ItemStack(Inventory.Api.World.GetItem(new AssetLocation("vinconomy:sculpturebundle")), 1);
            TreeAttribute treeAttr = new TreeAttribute();
            if (SculptureName?.Length > 0 && !SculptureName.IsWhiteSpace())
                treeAttr.SetString("SculptureName", SculptureName);
            else
                treeAttr.SetString("SculptureName", Lang.Get("vinconomy:generic-sculpture"));

            // SetLong instead of SetInt per Tyron's documentation for JSON serialization (See SetInt)
            // Why is this a thing? More Tyron Jank.
            treeAttr.SetLong("SizeX", SculptureHorizontalSize);
            treeAttr.SetLong("SizeY", SculptureVerticalSize);
            treeAttr.SetLong("SizeZ", SculptureHorizontalSize);
            stack.Attributes = treeAttr;
            return stack;
        }

        public ItemStack GenNewSculptureBundle()
        {
            ItemStack stack = GenStubbedBundle();

            TreeAttribute contents = new TreeAttribute();

            //int i = 0;
            int numBlocks = 0;
            for (int layer = 0; layer < SculptureVerticalSize; layer++)
            {
                for (int y = 0; y < SculptureHorizontalSize; y++)
                {
                    for (int x = 0; x < SculptureHorizontalSize; x++)
                    {
                        ToggledStockItemSlot slot = GetSlotForGrid(layer, x, y);
                        if (!slot.Empty && slot.Enabled)
                        {
                            numBlocks++;
                            contents.SetItemstack(String.Format("x{0}-y{1}-z{2}", x, layer, y), TradingUtil.GetItemStackClone(slot, 1));
                            //i++;
                        }

                    }
                }
            }
            TreeAttribute treeAttr = stack.Attributes as TreeAttribute;

            treeAttr.SetLong("NumBlocks", numBlocks);
            treeAttr.SetAttribute("Contents", contents);

            stack.Attributes = treeAttr;
            return stack;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
           
            for (var i = 0; i < Slots.Length; i++)
            {
                tree.SetItemstack($"slot_{i}", Slots[i].Itemstack);
                tree.SetBool($"slot_{i}_enabled", Slots[i].Enabled);
            }

            tree.SetString("sculptureName", SculptureName);
            tree.SetInt("sculptureHorizontalSize", SculptureHorizontalSize);
            tree.SetInt("sculptureVerticalSize", SculptureVerticalSize);

        }
        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            base.FromTreeAttributes(tree);
            int length = (int)Math.Pow(MaxSculptureSize, 3);
            for (int i = 0; i < length; i++)
            {
                Slots[i].Itemstack = tree.GetItemstack($"slot_{i}");
                Slots[i].Enabled = tree.GetBool($"slot_{i}_enabled", true);
                if (Inventory.Api?.World != null)
                {
                    Slots[i].Itemstack?.ResolveBlockOrItem(Inventory.Api.World);
                }
            }

            SculptureName = tree.GetString("sculptureName", "Sculpture");
            SculptureHorizontalSize = tree.GetInt("sculptureHorizontalSize", 1);
            SculptureVerticalSize = tree.GetInt("sculptureVerticalSize", 1);

            //UpdateEnabledSlots();
        }

        public void UpdateEnabledSlots()
        {
            for (int layer = 0; layer < MaxSculptureSize; layer++)
            {
                for (int y = 0; y < MaxSculptureSize; y++)
                {
                    for (int x = 0; x < MaxSculptureSize; x++)
                    {
                        ToggledStockItemSlot slot = GetSlotForGrid(layer, x, y);
                        slot.Enabled = layer < SculptureVerticalSize && y < SculptureHorizontalSize && x < SculptureHorizontalSize;
                        //Slots[index].MarkDirty();
                    }
                }
            }
            this.UpdateProductSlot();
            this.Inventory.BlockEntity.MarkDirty();
        }

        public void UpdateProductSlot()
        {
            this.Product.Itemstack = GenStubbedBundle();
            this.Product.MarkDirty();
        }


        public override int GetTotalProductAvailable()
        {
            if (Product?.Itemstack == null) return 0;

            int amount = Int32.MaxValue;
            foreach (ToggledStockItemSlot item in Slots)
            {
                if (item.Enabled)
                {
                    if (item.StackSize <= 0) return 0;
                    amount = Math.Min(amount, item.StackSize);
                }
            }

            return amount;
        }

    }
}
