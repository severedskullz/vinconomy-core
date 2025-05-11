using Viconomy.BlockEntities;
using Viconomy.BlockEntities.TextureSwappable;
using Viconomy.src.BlockTypes.Unfinished;
using Viconomy.Inventory.Impl;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities.Unfinished
{
    public class BEVinconPurchaseCrate : BETextureSwappableBlockDisplay, IOwnableStall
    {
        public string Owner => throw new System.NotImplementedException();
        private bool requested;
        private ModSystemLabelMeshCache labelCacheSys;
        private MeshData labelMesh;
        private int labelColor;
        private ItemStack labelStack;
        public string label;
        private BlockVPurchaseCrate ownBlock;
        private MeshData ownMesh;

        public LabelProps LabelProps
        {
            get
            {
                if (label == null) return null;
                ownBlock.Props.Labels.TryGetValue(label, out var prop);
                return prop;
            }
        }

        public override int DisplayedItems => 0; // Dont display the 
        public override string InventoryClassName { get { return "VinconomyInventory"; } }
        protected ViconItemInventory inventory;
        public override InventoryBase Inventory => inventory;


        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            return true;
        }

        protected override float[][] GenTransformationMatrices()
        {
            return null;
        }

        private void GenLabelMeshWithItemStack(int textureSubId)
        {
            TextureAtlasPosition texPos = capi.BlockTextureAtlas.Positions[textureSubId];
            labelMesh = ownBlock.GenLabelMesh(capi, label, texPos, true, null);
            //this.labelMesh.Rotate(BlockVPurchaseCrate.origin, 0f, this.rotAngleY + 3.1415927f, 0f).Scale(BlockVPurchaseCrate.origin, this.rndScale, this.rndScale, this.rndScale);
        }



        void genLabelMesh()
        {
            if (LabelProps?.EditableShape == null || labelStack == null || requested) return;

            if (labelCacheSys == null) labelCacheSys = Api.ModLoader.GetModSystem<ModSystemLabelMeshCache>();

            requested = true;
            labelCacheSys.RequestLabelTexture(labelColor, Pos, labelStack, (texSubId) =>
            {
                GenLabelMeshWithItemStack(texSubId);
                MarkDirty(true);
                requested = false;
            });
        }

        public void SetOwner(IPlayer owner)
        {
        }

        public void SetOwner(string playerUUID, string playerName) { 
        }

        public void SetRegisterID(int registerID)
        {
        }

        public void SetIsAdminShop(bool isAdminShop)
        {
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (base.OnTesselation(mesher, tesselator))
            {
                return true;
            }
            if (labelMesh == null)
            {
                genLabelMesh();
            }
            mesher.AddMeshData(labelMesh, 1);
            return base.OnTesselation(mesher, tesselator);
        }


    }
}
