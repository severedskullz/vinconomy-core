using System.IO;
using Viconomy.BlockEntities.TextureSwappable;
using Viconomy.Inventory;
using Viconomy.src.Renderer;
using Viconomy.Trading;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Viconomy.BlockEntities
{
    public abstract class BEVinconBase : BETextureSwappableBlockDisplay, IOwnableStall
    {
        protected VinconomyCoreSystem modSystem;
        
        public string Owner { get; protected set; }
        public string OwnerName { get; protected set; }
        public int RegisterID { get; protected set; } = -1;
        public bool IsAdminShop { get; protected set; }

        public override string InventoryClassName { get { return "VinconomyInventory"; } }

        public virtual int StallSlotCount { get; protected set; } = 4;
        public virtual int StacksPerSlot { get; protected set; } = 9;
        public virtual int BulkPurchaseAmount { get; protected set; } = 5;

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

        public virtual void PurchaseItem(IPlayer player, int stallSlot, int desiredAmount, BEVinconRegister shopRegister)
        {
            ItemSlot[] slots = GetSlotsForStall(stallSlot);

            TradeRequest request = new TradeRequest();
            request.customer = player;
            request.shopRegister = shopRegister;
            request.sellingEntity = this;
            request.productNeeded = TradingUtil.GetItemStackClone(FindFirstNonEmptyStockSlotForStall(stallSlot), GetNumItemsPerPurchaseForStall(stallSlot)); ;
            request.productSourceSlots = slots;
            request.numPurchases = desiredAmount;
            request.coreApi = this.Api;
            request.currencyNeeded = TradingUtil.GetItemStackClone(GetCurrencyForStall(stallSlot));
            request.isAdminShop = this.IsAdminShop;

            TradeResult result = TradingUtil.TryPurchaseItem(request);
            if (result.error != null)
            {
                VinconomyCoreSystem.PrintClientMessage(player, result.error);
            }
            else
            {
                this.MarkDirty(true, null);
                this.updateMeshes();
            }
        }

        public abstract ItemSlot[] GetSlotsForStall(int stallSlot);
        public abstract ItemSlot GetCurrencyForStall(int stallSlot);
        public abstract int GetNumItemsPerPurchaseForStall(int stallSlot);

        public ItemSlot FindFirstNonEmptyStockSlotForStall(int stallSlot)
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

    }
}
