using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.BlockEntities;
using Viconomy.GUI;
using Viconomy.Inventory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Viconomy.src.BlockEntities
{
    public class BEVContainer : BlockEntityDisplay
    {
        private ViconomyInventory inventory;
        private static int slotCount = 4;
        private Block block;
        private ICoreAPI api;
        public string owner;
        GuiDialogBlockEntityViconContainer clientDialog;

        public override InventoryBase Inventory
        {
            get
            {
                return this.inventory;
            }
        }

        public override string InventoryClassName
        {
            get
            {
                return "vinconomycontainer";
            }
        }
        public BEVContainer()
        {
            this.inventory = new ViconomyInventory(null, null, 4, 4);
        }

        public override void Initialize(ICoreAPI api)
        {
            this.block = api.World.BlockAccessor.GetBlock(this.Pos);

            base.Initialize(api);
            this.inventory.LateInitialize("vicon-1", api);

            api.Logger.Log(EnumLogType.Debug, "Initialized BEVContainer.");

            this.api = api;

        }

        protected override float[][] genTransformationMatrices()
        {
            float[][] tfMatrices = new float[BEVContainer.slotCount][];
            for (int index = 0; index < BEVContainer.slotCount; index++)
            {
                bool flag = this.Inventory[index].Itemstack.Class == EnumItemClass.Block;

                float x = (index % 2 == 0) ? 0.225f : 0.725f;
                float y = (index < 2) ? 0.3f : 0.225f;
                float z = (index / 2 == 0) ? 0.225f : 0.725f;
                Matrixf matrix = new Matrixf().Translate(0.5f, 0f, 0.5f).RotateYDeg(this.block.Shape.rotateY).Translate(x - 0.5f, y, z - 0.5f).Translate(-0.5f, 0f, -0.5f);
                tfMatrices[index] = matrix.Values;
            }
            return tfMatrices;
        }
    }
}
