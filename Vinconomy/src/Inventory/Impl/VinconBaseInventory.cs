using Commercially.Common.Interfaces;
using Commercially.Common.Inventory;
using Commercially.Vinconomy.Config;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.ModSystems;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Commercially.Vinconomy.Inventory
{
    public abstract class VinconBaseInventory : InventoryBase, ILateInitInventory, IStallStockUpdater
    {
        public BlockEntity BlockEntity { get; protected set; }
        public ItemSlot[] InternalSlots { get; protected set; }
        public BaseStallSlot[] StallSlots { get; protected set; }
        public string StallType { get; protected set; }
        public int SlotsPerStall {  get; protected set; }

        public bool IsInternalSlotsInitialized => InternalSlots != null;
        public bool IsSlotsInitialized => StallSlots != null;

        public VinconomyCoreSystem modSystem { get; protected set; }
        public event OnStockUpdatedDelegate OnStockUpdated;

        public IStallComponent StallComponent { get; protected set; }
        public bool DiscardCurrency;

        public override int Count
        {
            get
            {
                int i = InternalSlots?.Length ?? 0;

                if (StallSlots == null)
                    return i;

                foreach (BaseStallSlot stall in StallSlots)
                {
                    i += stall.TotalItemSlots;
                }

                return i;
            }
        }

        public VinconBaseInventory(BlockEntity entity,ICoreAPI api) : base("-", api)
        {
            //modSystem = Api.ModLoader.GetModSystem<VinconomyModSystem>();

            BlockEntity = entity;

            // A Non-Instantiated inventory. Will error out unless Initialize is called
            InitializeInternalSlots();

        }


        public T GetStall<T>(int slot) where T : BaseStallSlot
        {
            return (T)GetStall(slot);
        }

        public BaseStallSlot GetStall(int slot)
        {
            if (slot >= StallSlots.Length || slot < 0)
            {
                throw new System.IndexOutOfRangeException($"Stall slot index {slot} is out of range for inventory {InventoryID} with {StallSlots.Length} stall slots.");
            }

            return StallSlots[slot];
        }

        protected Type GetStallType(string className)
        {
            /*
            Type type = Type.GetType(className);

            if (type == null)
            {
                throw new InvalidCastException($"Class {className} not found as a valid class for Stall Slot");
            }

            if (type != null && typeof(StallSlotBase).IsAssignableFrom(type))
            {
                return type;
            }
            else
            {
                throw new InvalidCastException("Stall Slot Type must be of StallSlotBase");
            }
            */
            return VinconomyCoreSystem.GetStallType(className);
        }

        protected BaseStallSlot InstantiateStallType(Type type, int stallSlot)
        {
            BaseStallSlot instance = (BaseStallSlot)Activator.CreateInstance(type, [this, stallSlot]);
            return instance;
        }

        /// <summary>
        /// Initialize the Internal Stall Slot array. This method should keep already existing arrays in tact if IsInternalSlotsInitialized is true (in this case, it was instantiated in FromTreeAttributes)
        /// otherwise it should initialize the array with empty item slots;
        /// </summary>
        public virtual void InitializeInternalSlots()
        {
            if (!IsInternalSlotsInitialized)
                InternalSlots = Array.Empty<ItemSlot>();
        }

        /*
        /// <summary>
        /// Converts a the specified stall index into the appropriate stall type. If stallTypes is provided, it will return that index's entry from the array, otherwise it will return stallType for all types.
        /// </summary>
        protected string GetStallTypeForStall(int stallIndex, string[] stallTypes, string stallType)
        {
            if (stallTypes == null)
            {
                return stallType;
            }

            return stallTypes[stallIndex];
        }
        */

        public virtual void InitializeSlots(JsonObject properties, ICoreAPI api)
        {


            StallComponent = BlockEntity.GetBehavior<IStallComponent>();
            /*
             string[] stallTypes = properties["stallTypes"].AsArray<string>();
             if (stallTypes != null && stallTypes.Length != numStalls)
             {
                 throw new ArgumentException($"Number of stall types present in the array must match the length of numStalls of {numStalls}");
             }
             else if (stallTypes != null && stallType != null)
             {
                 throw new ArgumentException($"Choose either stallTypes or stallType - not both");
             }
             string curStallType = GetStallTypeForStall(0, stallTypes, stallType);
             */

            InitializeInternalSlots();
            InitializeStallSlots(properties);

            // This has bit us in the but more times than I can count, so lets triple check that InvNetworkUtil is initialized
            if (api != null && InvNetworkUtil == null)
            {
                InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
            }

            //Lastly, be sure to resolve the collectible IDs into actual blocks/items 
            AfterBlocksLoaded(api.World);
        }

        
        public override void OnItemSlotModified(ItemSlot slot)
        {
            base.OnItemSlotModified(slot);
            this.OnStockModified(slot);
            this.BlockEntity.MarkDirty(true);
        }
        

        public virtual void InitializeFromProperties(JsonObject properties, string className, string instanceID, ICoreAPI api)
        {
            //Api = api;
            //this.instanceID = instanceID;
            //this.className = className;
            LateInitialize($"{className}-{instanceID}", api);

            modSystem = Api.ModLoader.GetModSystem<VinconomyCoreSystem>();

            InitializeSlots(properties, api);
            ApplyFilters(properties);
            ApplyBackgrounds(properties);
        }

        protected virtual void ApplyBackgrounds(JsonObject properties)
        {
            string stallBackground = properties["stallBackground"].AsString(null);
            string[] stallBackgrounds = properties["stallBackgrounds"].AsArray<string>(null);
            if (stallBackground != null)
            {
                foreach (BaseStallSlot stall in StallSlots)
                {
                    stall.SetStallBackground(stallBackground);
                }
            }
            else if (stallBackgrounds != null)
            {
                if (stallBackgrounds.Length != StallSlots.Length)
                {
                    throw new ArgumentException($"Number of stall backgrounds present in the array must match the length of numStalls of {StallSlots.Length}");
                }
                for (int i = 0; i < StallSlots.Length; i++)
                {
                    StallSlots[i].SetStallBackground(stallBackgrounds[i]);
                }
            }

            // TODO: Tyron's patented "Jank" in GuiDialogCharacter prevents Armor icons from being rendered until you open the character screen atleast once.
            // Rather than just register them wherever the rest of the icons were registered, he made a whole new method "RegisterArmorIcons" to do so and checks if they exist
            // in capi.Gui.Icons.CustomIcons when we open the dialog and then register them if they dont... What in the literal FUCK?
            // Now I need to figure out how to invoke this method automatically so the thing shows up, or just go ahead and register them manually in the Vinconomy Mod System?

        }

        protected virtual void ApplyFilters(JsonObject properties)
        {
            string stallFilter = properties["stallFilter"].AsString(null);
            string[] stallFilters = properties["stallFilters"].AsArray<string>(null);
            if (stallFilter != null && stallFilters != null)
            {
                throw new ArgumentException($"Choose either stallFilter or stallFilters - not both");
            }

            if (stallFilter != null)
            {
                Vintagestory.API.Common.Func<ItemSlot, bool> filter = modSystem.CommerciallySystem.GetFilter(stallFilter);
                foreach (BaseStallSlot stall in StallSlots)
                {

                    stall.SetStallFilter(filter);
                }
            }
            else if (stallFilters != null)
            {
                if (stallFilters.Length != StallSlots.Length)
                {
                    throw new ArgumentException($"Number of stall filters present in the array must match the length of numStalls of {StallSlots.Length}");
                }
                for (int i = 0; i < StallSlots.Length; i++)
                {
                    Vintagestory.API.Common.Func<ItemSlot, bool> filter = modSystem.CommerciallySystem.GetFilter(stallFilters[i]);
                    StallSlots[i].SetStallFilter(filter);
                }
            }
        }

        public override void ResolveBlocksOrItems()
        {
            int id = 0;
            using IEnumerator<ItemSlot> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                ItemSlot current = enumerator.Current;
                if (current?.Itemstack != null && !current.Itemstack.ResolveBlockOrItem(Api.World))
                {
                    current.Itemstack = null;
                }
                id++;
            }
        }

        /// <summary>
        /// Initialize the Stall Slot array. This method should keep already existing arrays in tact if IsSlotsInitialized is true (in this case, it was instantiated in FromTreeAttributes)
        /// otherwise it should initialize the array with empty item slots;
        /// </summary>
        public virtual void InitializeStallSlots(JsonObject properties)
        {
            StallType = properties["stallType"]?.AsString("GenericStallSlot");



            if (StallType == null)
                return;

             Type stallType = GetStallType(StallType);

            int numStalls = properties["numStalls"].AsInt(4);
            int numSlotsPerStall = properties["numSlotsPerStall"].AsInt(16);

            if (!IsSlotsInitialized)
            {
                StallSlots = new BaseStallSlot[numStalls];
                for (int i = 0; i < numStalls; i++)
                {
                    BaseStallSlot instance =  InstantiateStallType(stallType, i);
                    instance.Initialize(this, i, numSlotsPerStall);
                    StallSlots[i] = instance;
                }
            }

            // If we already have the slot array, make sure it is atleast greater than numSlots. I don't expect people to be resizing inventories mid-playthrough, but better safe than sorry
            // We *DO NOT* support shrinking inventories. In that case, the items are lost - so *DON'T DO IT*.
            else
            {
                if (StallSlots.Length < numStalls)
                {
                    int amount = numStalls - StallSlots.Length;
                    AddStallSlots(stallType, amount, numSlotsPerStall);
                }

                //TODO: Figure out how to grow individual stall's product slot size for numSlotsPerStall
            }
        }

        public void AddStallSlots(Type type, int amount, int numSlotsPerStall)
        {
            while (amount-- > 0)
            {
                BaseStallSlot instance = InstantiateStallType(type, StallSlots.Length);
                instance.Initialize(this, StallSlots.Length, numSlotsPerStall);

                //TODO: Inneffecient - Store to list and add the whole thing in one pass instead of allocating new array and copying over each time
                StallSlots = StallSlots.Append(instance);
            }
        }

        /// <summary>
        /// Retrieves the item slot corresponding to the specified global slot index.
        /// </summary>
        /// <remarks>The global index spans both internal slots and all stall slots in order. Use this
        /// method to access an item slot without needing to know whether it is in the internal or stall
        /// collection.</remarks>
        /// <param name="index">The zero-based global index of the item slot to retrieve. Must be within the range of available slots.</param>
        /// <returns>The item slot at the specified global index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the specified index is less than zero or greater than or equal to the total number of slots.</exception>
        //TODO: Im sure this can be done better for stalls of equal size, but I do want to support variable sized stalls at some point in the future.
        //If thats the case, then this becomes entirely neccesary
        public ItemSlot GetItemSlotFromID(int index)
        {
            int i = index;
            if (i < InternalSlots.Length)
            {
                return InternalSlots[i];
            }
            else
            {
                i -= InternalSlots.Length;
            }

            foreach (BaseStallSlot stall in StallSlots)
            {
                if (i < stall.TotalItemSlots)
                {
                    return stall[i];
                }
                else
                {
                    i -= stall.TotalItemSlots;
                }
            }

            throw new IndexOutOfRangeException($"Index {index} out of bounds for stall. It only has {this.Count} total slots");
        }

        public override ItemSlot this[int slotId] {
            get { return GetItemSlotFromID(slotId); }
            set {
                int i = slotId;
                if (i < InternalSlots.Length)
                {
                    InternalSlots[i] = value;
                }
                else
                {
                    i -= InternalSlots.Length;
                }

                foreach (BaseStallSlot stall in StallSlots)
                {
                    if (i < stall.TotalItemSlots)
                    {
                        stall[i] = value;
                    }
                    else
                    {
                        i -= stall.TotalItemSlots;
                    }
                }

                throw new IndexOutOfRangeException($"Index {slotId} out of bounds for stall. It only has {this.Count} total slots");
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            DiscardCurrency = tree.GetBool("discardCurrency");
            int numStalls = tree.GetInt("numStalls");
            if (!IsSlotsInitialized)
            {

                SlotsPerStall = tree.GetInt("numSlotsPerStall", 9);
                StallType = tree.GetString("stallType");
                Type stallType = GetStallType(tree.GetString("stallType", "GenericStallSlot"));

                StallSlots = new BaseStallSlot[numStalls];
                for (int i = 0; i < numStalls; i++)
                {
                    BaseStallSlot stall = InstantiateStallType(stallType, i);
                    ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                    stall.PreInitialize(this, i);
                    stall.FromTreeAttributes(stallTree);
                    StallSlots[i] = stall;
                }

                //TODO: How to handle resizing of internal slots? Can we even support this?
                ITreeAttribute internalSlots = tree.GetOrAddTreeAttribute("internalSlots");
                //int numInternalSlots = internalSlots.GetInt("numSlots",0);
                for (int i = 0; i < InternalSlots.Length; i++)
                {
                    ItemStack stack = internalSlots.GetItemstack("slot" + i);
                    InternalSlots[i].Itemstack = stack;

                    if (Api?.World == null)
                    {
                        continue;
                    }

                    stack?.ResolveBlockOrItem(Api.World);
                }
            } else
            {
                for (int i = 0; i < numStalls; i++)
                {
                    BaseStallSlot stall = GetStall(i);
                    ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                    stall.FromTreeAttributes(stallTree);
                }

                ITreeAttribute internalSlots = tree.GetOrAddTreeAttribute("internalSlots");
                for (int i = 0; i < InternalSlots.Length; i++)
                {
                    ItemStack stack = internalSlots.GetItemstack("slot" + i);
                    InternalSlots[i].Itemstack = stack;

                    if (Api?.World == null)
                    {
                        continue;
                    }

                    stack?.ResolveBlockOrItem(Api.World);
                }
            }

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("numStalls", StallSlots.Length);
            tree.SetString("stallType", StallType);
            tree.SetInt("numSlotsPerStall", SlotsPerStall);
            tree.SetBool("discardCurrency", DiscardCurrency);

            for (int i = 0; i < StallSlots.Length; i++)
            {
                ITreeAttribute stallTree = tree.GetOrAddTreeAttribute("stall" + i);
                StallSlots[i].ToTreeAttributes(stallTree);
            }

            ITreeAttribute internalSlots = tree.GetOrAddTreeAttribute("internalSlots");
            //internalSlots.SetInt("numSlots", InternalSlots.Length);
            for (int i = 0; i < InternalSlots.Length; i++)
            {
                internalSlots.SetItemstack("slot" + i, InternalSlots[i].Itemstack);
                internalSlots.SetString("slot" + i + "-name", InternalSlots[i].Itemstack?.ToString());
            }
        }

        public virtual void OnStockModified(ItemSlot slot)
        {
            //if (Api.Side == EnumAppSide.Client) return;

            if (slot is ITrackedItemSlot stallProductSlot)
            {
                int stallSlot = stallProductSlot.GetStallIndex();
                BaseStallSlot stall = this.GetStall(stallSlot);

                // Make sure the slot isnt the Product slot, otherwise we will StackOverflow when we call UpdateProductSlot, which will call OnStockModified again
                if (stall is IGeneratedProductStall generatedStall  && slot != stall.Product)
                {
                    generatedStall.RegenProduct();
                } 
                else if (stall is IProductUpdater productUpdater) {
                    if (productUpdater.ShouldUpdateProductSlot(stall.Product) &&  productUpdater.ItemMatchesProduct(stall.Product))
                    {
                        productUpdater.UpdateProductSlot(stall);
                    }

                }

                ItemStack product = stall.Product?.Itemstack?.Clone();
                ItemStack currency = stall.Currency?.Itemstack?.Clone();
                int stockCount = stall.GetTotalProductAvailable();

                UpdateStockForSlot(StallComponent, stallSlot, product, stockCount, currency);
            }
        }

        public virtual void UpdateStockForSlot(IStallComponent shop, int stallSlot, ItemStack product, int stockCount, ItemStack currency)
        {
            // Really strange way of doing this, I know. I wanted to keep the inventory decoupled from the block entity as much as possible
            // This was the simplest way I could think of without having to pass a reference to the BE into each inventory instance.

            // TODO: This is now redundant, as the BE is now passed into the inventory on to check if the stall is an admin shop or not... Whoops! Remove this and just call modSystem.UpdateStockForSlot directly from the BE
            // Do I even need that event anymore? would it be useful to have a generic event for when stock is updated? Could be useful for other mods to hook into

            //TODO: Taking OUT results in 2 events, whereas inserting generates 1... why?
            OnStockUpdated?.Invoke(shop, stallSlot, product, stockCount, currency);
            if (this.Api.Side == EnumAppSide.Server)
            modSystem.UpdateStockForSlot(shop, stallSlot, product, stockCount, currency);
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            // Clones should never transition.
            // Tyron, it would be fucking GREAT if you gave us the ItemSlot instead!!! That way I can check the class, damnit!
            foreach (BaseStallSlot stall in StallSlots)
            {
                if (stall.Currency.Itemstack == stack || stall.Product.Itemstack == stack) return 0;
            }

            VinconomyConfig config = modSystem.Config;
            bool fooldDecaysInShops = config?.FoodDecaysInShops ?? false;
            bool isAdminOwned = StallComponent?.Ownable?.IsAdminOwned ?? false;
            if (fooldDecaysInShops && !isAdminOwned)
            {
                return base.GetDefaultTransitionSpeedMul(transType) * modSystem.Config.StallPerishRate;
            }
            else
            {
                return 0;
            }

        }

        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        {
            // I didnt mark the slots as dirty as to not trigger all of the Updater logic. It is also easier to clear the ownable inventory
            // in one go rather than triggering the event for each slot dozens of times.
            IStallComponent ownable = this.BlockEntity.GetBehavior<IStallComponent>();
            if (ownable != null && BlockEntity.Api.Side == EnumAppSide.Server)
            {
                modSystem.DB.ClearAllStock(ownable);
            }

            for (int i = 0; i < StallSlots.Length; i++)
            {
                GetStall(i).DropInventory(pos, maxStackSize, ownable == null);
            }
        }
    }
}
