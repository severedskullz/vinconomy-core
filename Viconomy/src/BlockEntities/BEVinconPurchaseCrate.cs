using Viconomy.BlockEntities.TextureSwappable;
using Viconomy.BlockTypes;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
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
                if (this.label == null)
                {
                    return null;
                }
                LabelProps prop;
                this.ownBlock.Props.Labels.TryGetValue(this.label, out prop);
                return prop;
            }
        }

        public override int DisplayedItems => 0; // Dont display the 
        public override string InventoryClassName { get { return "VinconomyInventory"; } }
        protected ViconomyInventory inventory;
        public override InventoryBase Inventory => inventory;


        public bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            return true;
        }

        protected override float[][] genTransformationMatrices()
        {
            throw new System.NotImplementedException();
        }

        private void genLabelMesh()
        {
            LabelProps labelProps = this.LabelProps;
            if (((labelProps != null) ? labelProps.EditableShape : null) == null || this.labelStack == null || this.requested)
            {
                return;
            }
            if (this.labelCacheSys == null)
            {
                this.labelCacheSys = this.Api.ModLoader.GetModSystem<ModSystemLabelMeshCache>(true);
            }
            this.requested = true;
            this.labelCacheSys.RequestLabelTexture(this.labelColor, this.Pos, this.labelStack, delegate (int texSubId)
            {
                this.GenLabelMeshWithItemStack(texSubId);
                this.MarkDirty(true, null);
                this.requested = false;
            });
        }

        private void GenLabelMeshWithItemStack(int textureSubId)
        {
            TextureAtlasPosition texPos = this.capi.BlockTextureAtlas.Positions[textureSubId];
            this.labelMesh = this.ownBlock.GenLabelMesh(this.capi, this.label, texPos, true, null);
            //this.labelMesh.Rotate(BlockVPurchaseCrate.origin, 0f, this.rotAngleY + 3.1415927f, 0f).Scale(BlockVPurchaseCrate.origin, this.rndScale, this.rndScale, this.rndScale);
        }

        public void SetOwner(IPlayer owner)
        {
            throw new System.NotImplementedException();
        }

        public void SetOwner(string playerUUID, string playerName)
        {
            throw new System.NotImplementedException();
        }

        public void SetRegisterID(int registerID)
        {
            throw new System.NotImplementedException();
        }

        public void SetIsAdminShop(bool isAdminShop)
        {
            throw new System.NotImplementedException();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (base.OnTesselation(mesher, tesselator))
            {
                return true;
            }
            if (this.labelMesh == null)
            {
                this.genLabelMesh();
            }
            mesher.AddMeshData(this.labelMesh, 1);
            return base.OnTesselation(mesher,tesselator);
        }
    }
}
