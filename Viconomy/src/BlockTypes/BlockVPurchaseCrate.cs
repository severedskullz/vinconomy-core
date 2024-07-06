using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.BlockTypes
{
    public class BlockVPurchaseCrate : Block, ITexPositionSource
    {
        private string curType;

        // Token: 0x040008A2 RID: 2210
        private LabelProps nowTeselatingLabel;

        // Token: 0x040008A3 RID: 2211
        private ITexPositionSource tmpTextureSource;

        // Token: 0x040008A4 RID: 2212
        private TextureAtlasPosition labelTexturePos;

        // Token: 0x040008A5 RID: 2213
        public CrateProperties Props;

        // Token: 0x040008A6 RID: 2214
        public static Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);

        // Token: 0x040008A7 RID: 2215
        private Cuboidf[] closedCollBoxes = new Cuboidf[]
        {
            new Cuboidf(0.0625f, 0f, 0.0625f, 0.9375f, 0.9375f, 0.9375f)
        };
        // Token: 0x170001B9 RID: 441
        // (get) Token: 0x06000D42 RID: 3394 RVA: 0x000840B4 File Offset: 0x000822B4
        public Size2i AtlasSize
        {
            get
            {
                return this.tmpTextureSource.AtlasSize;
            }
        }

        // Token: 0x170001BA RID: 442
        // (get) Token: 0x06000D43 RID: 3395 RVA: 0x000840C1 File Offset: 0x000822C1
        public string Subtype
        {
            get
            {
                if (this.Props.VariantByGroup != null)
                {
                    return this.Variant[this.Props.VariantByGroup];
                }
                return "";
            }
        }

        // Token: 0x170001BB RID: 443
        // (get) Token: 0x06000D44 RID: 3396 RVA: 0x000840EC File Offset: 0x000822EC
        public string SubtypeInventory
        {
            get
            {
                CrateProperties props = this.Props;
                if (((props != null) ? props.VariantByGroupInventory : null) != null)
                {
                    return this.Variant[this.Props.VariantByGroupInventory];
                }
                return "";
            }
        }

        // Token: 0x170001BC RID: 444
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (this.nowTeselatingLabel != null)
                {
                    return this.labelTexturePos;
                }
                TextureAtlasPosition pos = this.tmpTextureSource[this.curType + "-" + textureCode];
                if (pos == null)
                {
                    pos = this.tmpTextureSource[textureCode];
                }
                if (pos == null)
                {
                    pos = (this.api as ICoreClientAPI).BlockTextureAtlas.UnknownTexturePosition;
                }
                return pos;
            }
        }

        public MeshData GenLabelMesh(ICoreClientAPI capi, string label, TextureAtlasPosition texPos, bool editableVariant, Vec3f rotation = null)
        {
            LabelProps labelProps;
            this.Props.Labels.TryGetValue(label, out labelProps);
            if (labelProps == null)
            {
                throw new ArgumentException("No label props found for this label");
            }
            AssetLocation shapeloc = (editableVariant ? labelProps.EditableShape : labelProps.Shape).Base.Clone().WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
            Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc);
            Vec3f rot = (rotation == null) ? new Vec3f(labelProps.Shape.rotateX, labelProps.Shape.rotateY, labelProps.Shape.rotateZ) : rotation;
            this.nowTeselatingLabel = labelProps;
            this.labelTexturePos = texPos;
            this.tmpTextureSource = capi.Tesselator.GetTextureSource(this, 0, true);
            MeshData meshLabel;
            capi.Tesselator.TesselateShape("cratelabel", shape, out meshLabel, this, rot, 0, 0, 0, null, null);
            this.nowTeselatingLabel = null;
            return meshLabel;
        }
    }
}
