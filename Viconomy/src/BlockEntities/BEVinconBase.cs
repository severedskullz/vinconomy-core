using System.IO;
using Viconomy.BlockEntities.TextureSwappable;
using Viconomy.Inventory;
using Viconomy.Renderer;
using Viconomy.Trading;
using Viconomy.Trading.TradeHandlers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.BlockEntities
{
    public abstract class BEVinconBase : BETextureSwappableBlockDisplay, IOwnableStall
    {
        protected VinconomyCoreSystem modSystem;
        protected GuiDialogBlockEntity invDialog;

        public string Owner { get; protected set; }
        public string OwnerName { get; protected set; }
        public int RegisterID { get; protected set; } = -1;
        public bool IsAdminShop { get; protected set; }

        public override string InventoryClassName { get { return "VinconomyInventory"; } }

        public virtual int StallSlotCount => 4;
        public virtual int StacksPerSlot => 9;
        public virtual int BulkPurchaseAmount => 5;

        public bool shouldRenderInventory;
        protected DistanceRenderer distanceRenderer;

        //protected AssetLocation OpenSound;
        //protected AssetLocation CloseSound;

        public override void Initialize(ICoreAPI api)
        {
            modSystem = api.ModLoader.GetModSystem<VinconomyCoreSystem>();
            //this.block = api.World.BlockAccessor.GetBlock(this.Pos);
            base.Initialize(api);

            if (Inventory is IStallSlotUpdater && api.Side == EnumAppSide.Server)
            {
                Inventory.SlotModified += UpdateStallForSlot;
            }

            /*
            JsonObject attributes = block.Attributes;
            string openSound = null;
            if (attributes != null && attributes["openSound"] != null)
            {
                openSound = attributes["openSound"].AsString(null);
            }

            string closeSound = null;
            if (attributes != null && attributes["openSound"] != null)
            {
                closeSound = attributes["openSound"].AsString(null); ;
            }

            this.OpenSound = (openSound == null) ? null : AssetLocation.Create(openSound, block.Code.Domain);
            this.CloseSound = (closeSound == null) ? null : AssetLocation.Create(closeSound, block.Code.Domain);
            */
            if (api.Side == EnumAppSide.Client)
            {
                this.distanceRenderer = new DistanceRenderer(this);
            }
        }

        protected virtual void UpdateStallForSlot(int index)
        {
            IStallSlotUpdater inv = (Inventory as  IStallSlotUpdater);
            if (inv != null)
            {
                modSystem.Mod.Logger.Debug("Got a product slot update for slot: " + index);
                int stallSlot = inv.GetStallForSlot(index);
                ItemSlot[] slots = inv.GetSlotsForStallSlot(stallSlot);

                ItemStack product = null;
                foreach (var item in slots)
                {
                    if (item.Itemstack != null)
                    {
                        if (product == null)
                        {
                            product = item.Itemstack.Clone();
                        } else
                        {
                            product.StackSize += item.StackSize;
                        }

                    }
                }

                ItemStack currency = inv.GetCurrencyForStallSlot(stallSlot).Itemstack;
                modSystem.UpdateStallProductForStall(this.RegisterID, this.Pos, stallSlot,  product, GetNumItemsPerPurchaseForStall(stallSlot), currency);
            }
        }

        public void SetOwner(IPlayer player)
        {
            Owner = player.PlayerUID;
            OwnerName = player.PlayerName;
        }
        public void SetOwner(string playerUUID, string playerName)
        {
            Owner = playerUUID;
            OwnerName = playerName;
        }

        public void SetRegisterID(int registerID)
        {
            RegisterID = registerID;

            IStallSlotUpdater inv = Inventory as IStallSlotUpdater;
            if (inv != null && Api.Side == EnumAppSide.Server)
            {
                int slots = inv.GetStallSlotCount();
                for (int i = 0; i < slots; i++)
                {
                    int index = Inventory.GetSlotId(inv.GetCurrencyForStallSlot(i));
                    if (index >= 0)
                    {
                        UpdateStallForSlot(index);
                    }
                }
            }
        }

        public void SetIsAdminShop(bool adminShop)
        {
            IsAdminShop = adminShop;
        }

        public abstract bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);

        public virtual int GetStallSlotForSelectionIndex(int index)
        {
            return index;
        }

        public abstract ItemSlot[] GetSlotsForStall(int stallSlot);
        
        public abstract ItemSlot GetCurrencyForStall(int stallSlot);
        
        public abstract int GetNumItemsPerPurchaseForStall(int stallSlot);

        public virtual ItemSlot FindFirstNonEmptyStockSlotForStall(int stallSlot)
        {
            ItemSlot[] slots = GetSlotsForStall(stallSlot);
            foreach (ItemSlot slot in slots)
            {
                if (slot.Itemstack != null)
                    return slot;
            }
            return null;
        }

        protected void SetAdminShop(IPlayer byPlayer, bool isAdmin)
        {
            if (byPlayer.PlayerUID != this.Owner)
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            if (!byPlayer.HasPrivilege("gamemode"))
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.NO_PRIVLEGE, new object[] { });
                return;
            }

            this.IsAdminShop = isAdmin;

            //PrintClientMessage(byPlayer, "set Admin Shop to " + this.isAdminShip);
            this.MarkDirty();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("Owner", this.Owner);
            tree.SetString("OwnerName", this.OwnerName);
            tree.SetInt("RegisterID", this.RegisterID);
            tree.SetBool("isAdminShop", this.IsAdminShop);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            this.Owner = tree.GetString("Owner");
            this.OwnerName = tree.GetString("OwnerName");
            this.RegisterID = tree.GetInt("RegisterID");
            this.IsAdminShop = tree.GetBool("isAdminShop");

        }

        protected void SetStallRegisterID(IPlayer byPlayer, byte[] data)
        {
            if (byPlayer.PlayerUID != this.Owner)
            {
                VinconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                int ID = reader.ReadInt32();
                SetRegisterID(ID);
            }

            //PrintClientMessage(byPlayer, "set ID to " + this.RegisterID);
            this.MarkDirty();
        }

        public void SetNowTesselatingObj(CollectibleObject collectible)
        {
            this.nowTesselatingObj = collectible;
        }

        public void SetNowTesselatingShape(Shape shape)
        {
            this.nowTesselatingShape = shape;
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.invDialog != null)
            {
                if (invDialog.IsOpened())
                {
                    this.invDialog.TryClose();
                }

                this.invDialog?.Dispose();
                this.invDialog = null;
            }

        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (this.invDialog != null)
            {
                if (this.invDialog.IsOpened())
                {
                    this.invDialog.TryClose();
                }

            }

            this.invDialog?.Dispose();
            this.invDialog = null;
        }

        protected virtual AggregatedSlots GetAggregateProductSlots(int stallSlot)
        {
            AggregatedSlots slots = new AggregatedSlots();
            foreach (ItemSlot slot in GetSlotsForStall(stallSlot))
            {
                slots.Add(slot);
            }
            return slots;
        }

        public virtual void PurchaseItem(IPlayer player, int stallSlot, int numPurchases, BEVinconRegister shopRegister)
        {
            GenericTradeRequest request = new GenericTradeRequest(Api, player);

            ItemStack currencyStack = GetCurrencyForStall(stallSlot).Itemstack;
            request.WithShop(shopRegister, this, stallSlot, IsAdminShop);
            request.WithPurchases(numPurchases);
            request.WithCurrency(currencyStack, TradingUtil.GetAllValidSlotsFor(player, currencyStack), currencyStack.StackSize);
            request.WithProduct(TradingUtil.GetItemStackClone(FindFirstNonEmptyStockSlotForStall(stallSlot), 1), GetAggregateProductSlots(stallSlot), GetNumItemsPerPurchaseForStall(stallSlot));
            
            //request.WithCoupons(null, null, false, false, 0,0);
            //request.WithTools(null, 1);
            //request.WithTradePass(null, null);
            request.Build();

            GenericTradeResult result = GenericTradeHandler.TryPurchaseItem(request);
            if (result.Error != null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, result.Error);
            } else
            {
                MarkDirty(true, null);
                UpdateMeshes();
            }






            /*
            ItemSlot[] slots = GetSlotsForStall(stallSlot);
            TradeRequest request = new TradeRequest();
            request.customer = player;
            request.shopRegister = shopRegister;
            request.sellingEntity = this;
            request.productNeeded = TradingUtil.GetItemStackClone(FindFirstNonEmptyStockSlotForStall(stallSlot), GetNumItemsPerPurchaseForStall(stallSlot)); ;
            request.productSourceSlots = slots;
            request.numPurchases = numPurchases;
            request.coreApi = this.Api;
            request.currencyNeeded = TradingUtil.GetItemStackClone(GetCurrencyForStall(stallSlot));
            request.isAdminShop = this.IsAdminShop;
            //request.shouldConsumeTool = ShouldConsumeTool(stallSlot);
            //request.requiredToolType = GetRequiredToolType(stallSlot);
            //request.tool = GetRequiredTool(player, stallSlot, desiredAmount);

            TradeResult result = TradingUtil.TryPurchaseItem(request);
            if (result.error != null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, result.error);
            }
            else
            {
                this.MarkDirty(true, null);
                this.UpdateMeshes();
            }
            */

        }
        public virtual ItemStack GetProductForStall(int stallSlot)
        {
            return TradingUtil.GetItemStackClone(FindFirstNonEmptyStockSlotForStall(stallSlot), GetNumItemsPerPurchaseForStall(stallSlot)); ;
        }

        /*
        protected virtual bool ShouldConsumeTool(int stallSlot)
        {
            return false;
        }

        protected virtual ToolType GetRequiredToolType( int stallSlot)
        {
            return ToolType.NONE;
        }

        protected virtual ItemSlot GetRequiredTool(IPlayer player, int stallSlot, int numPurchases)
        {
            ToolType type = GetRequiredToolType(stallSlot);
            switch(type)
            {
                case ToolType.NONE:
                    return null;

                case ToolType.FOOD_CONTAINER:
                    break;

                case ToolType.DRINK_CONTAINER:
                case ToolType.LIQUID_COINTAINER:
                    break;

                case ToolType.CHOPPING:
                case ToolType.CUTTING:
                    // I'll implement these later. As of right now, I have no ideas as to what would even require these.
                    break;
            }

            return null;
        }
        */

    }
}
