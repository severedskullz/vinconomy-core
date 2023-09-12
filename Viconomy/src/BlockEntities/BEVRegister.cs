using System;
using Viconomy.BlockTypes;
using Viconomy.Registry;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Viconomy.BlockEntities
{
    public class BEVRegister : BlockEntityContainer
    {
        private BlockVContainer block;
        private InventoryGeneric inventory;

        public string Name { get; set; }
        public string ID { get; internal set; }
        public string Owner { get; internal set; }
        public string OwnerName { get; internal set; }

        public override InventoryBase Inventory => inventory;

        public override string InventoryClassName => "ViconomyRegisteryInventory";

        public override void Initialize(ICoreAPI api)
        {
            this.block = (BlockVContainer)api.World.BlockAccessor.GetBlock(this.Pos);

            base.Initialize(api);

            ViconomyModSystem modSystem = api.ModLoader.GetModSystem<ViconomyModSystem>();

            if (ID == null)
            {
                Console.WriteLine("Register block was placed and did not have ID Set...");
                ViconRegister register = modSystem.GetRegistry().AddRegister(Owner, OwnerName, OwnerName + "'s Shop", this.Pos);
                ID = register.ID;
            } else
            {
                Console.WriteLine("Register block was placed and had ID Set... Updating");
                ViconRegister register = modSystem.GetRegistry().UpdateRegister(Owner, ID, Name, this.Pos);
            }
                       
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            ID = tree.GetString("ID");
            Owner = tree.GetString("Owner");
            OwnerName = tree.GetString("OwnerName");

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("ID", ID);
            tree.SetString("Owner", Owner);
            tree.SetString("OwnerName", OwnerName);
        }
    }

}