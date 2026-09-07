using Commercially.Common;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Inventory.StallSlots;
using Commercially.Vinconomy.Trading;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Commercially.Vinconomy.BlockEntityBehaviors
{
    public class BEGachaStallBehavior : BEStallBehavior
    {


        public BEGachaStallBehavior(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            return false;
        }

        public override bool TryPurchaseItem(IPlayer player, int _IGNORED, int numPurchases)
        {
            IShopComponent shop = null;

            if (_Ownable?.ParentID != null && _Ownable?.OwnerUID != null)
                shop = VinconomyCore.GetShop(_Ownable.OwnerUID, _Ownable.ParentID);


            List<Tuple<int, int>> purchases = new List<Tuple<int, int>>(StallCount);
            int totalWeight = 0;
            for (int i = 0; i < StallCount; i++)
            {
                GachaStallSlot slot = _InventoryProvider.GetStallSlot<GachaStallSlot>(i);
                if (slot.GetNumPurchasesRemaining() > 0)
                {
                    int weight = slot.GetStallWeight();
                    totalWeight += weight;
                    Tuple<int, int> option = new Tuple<int, int>(i, weight);
                    purchases.Add(option);
                }
            }

            int stallSlot = 0;
            int rolledWeight = Random.Shared.Next(0, totalWeight);
            foreach (var item in purchases)
            {
                if (rolledWeight < item.Item2)
                {
                    stallSlot = item.Item1;
                    break;
                }
                rolledWeight -= item.Item2;
            }

            

            if (CanPurchaseItem(player, shop, stallSlot, numPurchases))
            {
                PurchaseResult result = PurchaseItem(player, stallSlot, numPurchases, shop);
                return result.ErrorMsg != null;
            }

            return false;
        }

    }
}
