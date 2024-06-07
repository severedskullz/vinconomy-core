using System;
using System.Collections.Generic;
using Viconomy.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.BlockTypes
{
    public class BlockVGacha : BlockVContainer
    {
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            BEViconBase be = world.BlockAccessor.GetBlockEntity(selection.Position) as BEViconBase;
            List<WorldInteraction> interactions = new List<WorldInteraction>();
            if (be != null)
            {
                int index = selection.SelectionBoxIndex;
                ItemSlot product = be.FindFirstNonEmptyStockSlotForStall(index);
                ItemSlot currency = be.GetCurrencyForStall(index);

                //StallSlot slot = slots[selection.SelectionBoxIndex];

                if (currency.Itemstack != null && product != null)
                {
                    interactions.Add(new WorldInteraction
                    {
                        ActionLangCode = "vinconomy:stall-purchase",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = new ItemStack[] { currency.Itemstack }

                    });
                }

                interactions.Add(new WorldInteraction
                {
                    ActionLangCode = "vinconomy:stall-open-menu",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = null
                });

            }

            return interactions.ToArray();
        }

    }
}
