using Vinconomy.BlockTypes;
using Vintagestory.API.Client;

namespace Vinconomy.BlockTypes
{
    public class BlockVPurchaseContainer : BlockVContainer
    {
        public override TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (this.tmpTextureSource == null)
                {
                    return null;
                }

                if (textureCode == "primary")
                {
                    return this.tmpTextureSource[this.PrimaryMaterial+"-sides"];
                }
                if (textureCode == "secondary")
                {
                    return this.tmpTextureSource[this.SecondaryMaterial];
                }
                if (textureCode == "deco")
                {
                    return this.tmpTextureSource[this.DecoMaterial];
                }

                if (this.tmpTextureSource[textureCode] != null)
                {
                    return this.tmpTextureSource[textureCode];
                }
                else
                {
                    return this.tmpTextureSource["default"];
                }
            }
        }
    }
}
