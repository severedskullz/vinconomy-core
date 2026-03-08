using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vinconomy.ItemTypes
{
    public class ItemGachaBall : Item
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
  
            
            if (byEntity is EntityPlayer)
            {
                IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
                GiveBundledItems(slot, player);
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            ITreeAttribute attrs = itemStack.Attributes;
            if (attrs.GetTreeAttribute("Contents") != null)
            {
                return attrs.GetString("Name", Lang.Get("vinconomy:item-gacha"));
            } else
            {
                return Lang.Get("vinconomy:item-gacha-empty");
            }

        }


        private void GiveBundledItems(ItemSlot slot, IPlayer player)
        {
            ITreeAttribute treeAttr = slot.Itemstack.Attributes;
            ITreeAttribute contents = (TreeAttribute)treeAttr.GetTreeAttribute("Contents");

            if (contents != null)
            {
                EntityAgent ent = player.Entity;

                for (int i = 0; i < 6; i++)
                {
                    ItemStack blockStack = contents.GetItemstack($"Item{i}");
                    if (blockStack != null)
                    {
                        //Note: These console Writes were here because the spawning/giving of items was inconsistent. Sometimes it gave me nothing, some times it gave me the stack size AND spawned more.
                        // If it ever happens again, this is where the problem was. Still no idea why/how. Hunch is the actual attribute stack size was getting modified by TryGiveItemstack before cloned it,
                        // thus all subsequent calls gave me 0?
                        //Console.WriteLine($"From Item: {blockStack.StackSize}");
                        ItemStack newStack = blockStack.Clone();
                        newStack.ResolveBlockOrItem(ent.World);
                        //Console.WriteLine($"From New Item: {newStack.StackSize}");
                        player.InventoryManager.TryGiveItemstack(newStack, true);
                        //Console.WriteLine($"After Item: {blockStack.StackSize}");
                        //Console.WriteLine($"After New Item: {newStack.StackSize}");
                        if (newStack.StackSize > 0)
                        {
                            ent.World.SpawnItemEntity(newStack, ent.SidedPos.XYZ.Add(0.0f, 0.5f, 0.0f), null);
                        }

                    }
                }
            }

            slot.TakeOut(1);
            slot.MarkDirty();
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            ITreeAttribute treeAttr = inSlot.Itemstack.Attributes;
            ITreeAttribute contents = (TreeAttribute)treeAttr.GetTreeAttribute("Contents");

            if (contents != null)
            {
                dsc.AppendLine("Contents:");
                for (int i = 0; i < 6; i++)
                {
                    ItemStack blockStack = contents.GetItemstack($"Item{i}");
                    if (blockStack != null)
                    {
                        blockStack.ResolveBlockOrItem(world);

                        if (blockStack.StackSize > 0)
                        {
                            dsc.AppendLine($"{blockStack.StackSize}x {blockStack.GetName()}");
                        }

                    }
                }
            }
           

            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }
    }
}
