using Viconomy.Inventory;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.src.BlockEntities
{
    public abstract class BEViconBase : BlockEntityDisplay
    {
        protected ViconomyCore modSystem;
        protected Block block;
        public string Owner;
        public string OwnerName;
        public string RegisterID;
        public bool isAdminShop;

        protected ViconomyInventory inventory;
        public override InventoryBase Inventory { get { return this.inventory; } }
        public override string InventoryClassName { get { return "VinconomyInventory"; } }

        public virtual int StallSlotCount { get; protected set; } = 4;
        public virtual int StacksPerSlot { get; protected set; } = 9;
        public virtual int BulkPurchaseAmount { get; protected set; } = 5;

        protected AssetLocation OpenSound;
        protected AssetLocation CloseSound;

        public override void Initialize(ICoreAPI api)
        {
            modSystem = api.ModLoader.GetModSystem<ViconomyCore>();

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

        public abstract bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);
    }
}
