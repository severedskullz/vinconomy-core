using System.Collections.Generic;
using System.Text.Json.Nodes;
using Viconomy.Network;
using Viconomy.Network.Api;
using Vintagestory.API.MathTools;
using static Viconomy.VinconomyDiscordIntegrationSystem;

namespace Viconomy.TradeNetwork
{
     public class TradeNetworkPurchaseUpdate : Dictionary<string, ShopPurchaseUpdate>
    {

        public void AddPurchaseUpdate(string nodeId, int shopId, BlockPos pos, int stallSlot, int numPurchases)
        {
            string key = $"{nodeId}{shopId}{pos.X}{pos.Y}{pos.Z}{stallSlot}";
            if (!ContainsKey(key))
            {
                ShopPurchaseUpdate update = new ShopPurchaseUpdate();
                update.StallSlot = stallSlot;
                update.NodeId = nodeId;
                update.ShopId = shopId;
                update.X = pos.X;
                update.Y = pos.Y;
                update.Z = pos.Z;
                update.Amount = numPurchases;
                Add(key, update);
            }
            else
            {
                this[key].Amount += numPurchases;
            }
        }

        public void AddPurchaseUpdate(string nodeId, int shopId, TradeNetworkPurchasePacket purchase)
        {
            string key = $"{nodeId}{shopId}{purchase.X}{purchase.Y}{purchase.Z}{purchase.StallSlot}";
            if (!ContainsKey(key))
            {
                ShopPurchaseUpdate update = new ShopPurchaseUpdate();
                update.StallSlot = purchase.StallSlot;
                update.NodeId = nodeId;
                update.ShopId = shopId;
                update.X = purchase.X;
                update.Y = purchase.Y;
                update.Z = purchase.Z;
                update.Amount = purchase.Amount;
                Add(key, update);
            } else
            {
                this[key].Amount += purchase.Amount;
            }

            
        }

        public void AddPurchaseUpdate(string nodeId, int shopId, ShopPurchaseUpdate purchase)
        {
            string key = $"{nodeId}{shopId}{purchase.X}{purchase.Y}{purchase.Z}{purchase.StallSlot}";
            if (!ContainsKey(key))
            {
                Add(key, purchase);
            } else
            {
                this[key].Amount += purchase.Amount;
            }

        }

        public string ToJsonString()
        {
            JsonArray json = new JsonArray();
            foreach (ShopPurchaseUpdate product in this.Values)
            {
                json.Add(product.ToJsonString());
            }

            return json.ToString();
        }
    }
}