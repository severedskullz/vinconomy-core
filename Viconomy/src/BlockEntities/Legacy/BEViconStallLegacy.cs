using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Viconomy.BlockEntities.Legacy
{
    public class BEViconStallLegacy : BEVinconContainer
    {

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (shouldRenderInventory)
            {
                for (int i = 0; i < StallSlotCount; i++)
                {
                    try
                    {
                        ItemSlot slot = inventory.FindFirstNonEmptyStockSlot(i);
                        if (slot != null && !slot.Empty && tfMatrices != null)
                        {
                            mesher.AddMeshData(getOrCreateMesh(slot.Itemstack, i), tfMatrices[i]);
                        }
                    }
                    catch
                    {
                        Console.WriteLine("Had some trouble rendering a mesh in a stall for item");
                    }

                }
            }
            return false;
        }

    }

}
