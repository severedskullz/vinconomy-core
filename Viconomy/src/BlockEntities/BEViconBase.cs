using System.IO;
using Viconomy.Trading;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public abstract class BEViconBase : BlockEntityDisplay
    {
        protected ViconomyCoreSystem modSystem;
        protected Block block;
        public string Owner { get; protected set; }
        public string OwnerName { get; protected set; }
        public int RegisterID { get; protected set; }
        public bool isAdminShop { get; protected set; }

        public override string InventoryClassName { get { return "VinconomyInventory"; } }

        public virtual int StallSlotCount { get; protected set; } = 4;
        public virtual int StacksPerSlot { get; protected set; } = 9;
        public virtual int BulkPurchaseAmount { get; protected set; } = 5;

        protected AssetLocation OpenSound;
        protected AssetLocation CloseSound;

        public override void Initialize(ICoreAPI api)
        {
            modSystem = api.ModLoader.GetModSystem<ViconomyCoreSystem>();

            this.block = api.World.BlockAccessor.GetBlock(this.Pos);

            base.Initialize(api);

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

        public abstract bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);

        public virtual void PurchaseItem(IPlayer player, int stallSlot, int desiredAmount, BEVRegister shopRegister)
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
            request.isAdminShop = this.isAdminShop;

            TradeResult result = TradingUtil.TryPurchaseItem(request);
            if (result.error != null)
            {
                ViconomyCoreSystem.PrintClientMessage(player, result.error);
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
                ViconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            if (!byPlayer.HasPrivilege("gamemode"))
            {
                ViconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.NO_PRIVLEGE, new object[] { });
                return;
            }

            this.isAdminShop = isAdmin;

            //PrintClientMessage(byPlayer, "set Admin Shop to " + this.isAdminShip);
            this.MarkDirty();
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("Owner", this.Owner);
            tree.SetString("OwnerName", this.OwnerName);
            tree.SetInt("RegisterID", this.RegisterID);
            tree.SetBool("isAdminShop", this.isAdminShop);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
        {
            base.FromTreeAttributes(tree, world);
            this.Owner = tree.GetString("Owner");
            this.OwnerName = tree.GetString("OwnerName");
            this.RegisterID = tree.GetInt("RegisterID");
            this.isAdminShop = tree.GetBool("isAdminShop");
        }

        protected void SetStallRegisterID(IPlayer byPlayer, byte[] data)
        {
            if (byPlayer.PlayerUID != this.Owner)
            {
                ViconomyCoreSystem.PrintClientMessage(byPlayer, TradingConstants.DOESNT_OWN, new object[] { });
                return;
            }

            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);
                int ID = reader.ReadInt32();
                RegisterID = ID;
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
