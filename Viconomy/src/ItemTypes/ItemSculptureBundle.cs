using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viconomy.Trading;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace Viconomy.ItemTypes
{
    public class ItemSculptureBundle : Item
    {

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
  
            IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
            Console.WriteLine(player.Entity.Api.Side);
            Console.WriteLine(byEntity.Api.Side);



            if (byEntity is EntityPlayer)
            {
                if (byEntity.Controls.Sneak)
                {
                    //IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);

                    
                    ITreeAttribute treeAttr = slot.Itemstack.Attributes;
                    int sizeX = treeAttr.GetAsInt("SizeX");
                    int sizeY = treeAttr.GetAsInt("SizeY");
                    int sizeZ = treeAttr.GetAsInt("SizeZ");
                    ITreeAttribute contents = (TreeAttribute)treeAttr.GetTreeAttribute("Contents");

                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int z = 0; z < sizeZ; z++)
                        {
                            for (int x = 0; x < sizeX; x++)
                            {
                                ItemStack blockStack = contents.GetItemstack(String.Format("{0}-{1}-{2}", x, y, z));
                                if (blockStack != null)
                                {
                                    blockStack.ResolveBlockOrItem(byEntity.World);

                                    player.InventoryManager.TryGiveItemstack(blockStack, true);
                                    if (blockStack.StackSize > 0)
                                    {
                                        //Console.WriteLine("Should have spawned item for " + String.Format("{0}-{1}-{2}", x, y, z));
                                        byEntity.World.SpawnItemEntity(blockStack, player.Entity.SidedPos.XYZ.Add(0.0f, 0.5f, 0.0f), null);
                                    }
                                }
                                
                            }
                        }
                    }
                    slot.TakeOut(1);
                }

            }
        }
    }
}
