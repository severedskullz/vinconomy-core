
using System;
using System.Linq;
using System.Text;
using Viconomy.BlockEntities;
using Viconomy.src.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Viconomy.BlockTypes
{
    public class BlockVContainer : Block
    {
        public BlockVContainer()
        {
            this.PriorityInteract = true;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool result = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
            if (result)
            {
                BEViconStall viconBlock = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconStall;
                if (viconBlock != null)
                {
                    viconBlock.Owner = byPlayer.PlayerUID;
                    viconBlock.OwnerName = byPlayer.PlayerName;
                }
            }

            return result;
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        /*
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
           
                List<Cuboidf> cubs = new List<Cuboidf>();
                
                cubs.Add(new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.1f));
                cubs.Add(new Cuboidf(0f, 0f, 0f, 1f, 0.0625f, 0.5f));
                cubs.Add(new Cuboidf(0f, 0.9375f, 0f, 1f, 1f, 0.5f));
                cubs.Add(new Cuboidf(0f, 0f, 0f, 0.0625f, 1f, 0.5f));
                cubs.Add(new Cuboidf(0.9375f, 0f, 0f, 1f, 1f, 0.5f));

            return cubs.ToArray();
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
          
            return new Cuboidf[]
            {
                new Cuboidf(0f, 0f, 0f, 1f, 1f, 1f).RotatedCopy(0f,0f ,0f, new Vec3d(0.5, 0.5, 0.5))
            };
        }
        */


        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            Console.WriteLine(api.Side + ": On interaction start was called!");
            BEViconStall be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEViconStall;
            if (be != null)
            {
                return be.OnPlayerRightClick(byPlayer, blockSel);
            }
            return false;
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-crate-add",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "shift"
                },
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-crate-remove",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                },
                new WorldInteraction
                {
                    ActionLangCode = "blockhelp-crate-removeall",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "ctrl"
                }
            });
        }

        /*
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            StringBuilder stringBuilder = new StringBuilder();

            string text = Code.Domain + ":" + ItemClass.ToString().ToLowerInvariant() + "desc-" + Code.Path;
            string matching = Lang.GetMatching(text);
            matching = ((matching != text) ? matching : "");
            stringBuilder.Append(matching);

            if (EntityClass != null)
            {
                world.BlockAccessor.GetBlockEntity(pos)?.GetBlockInfo(forPlayer, stringBuilder);
            }

            return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }
        */

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BEViconStall vEntity = world.BlockAccessor.GetBlockEntity(pos) as BEViconStall;
            if (vEntity != null && vEntity.Owner == byPlayer.PlayerUID)
                base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            else if (api.Side == EnumAppSide.Server)
                ((IServerPlayer)byPlayer).SendMessage(0, Lang.Get("You do not own this stall", new object[0]), EnumChatType.CommandError, null);
        }


    }

}
