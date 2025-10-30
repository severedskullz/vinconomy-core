using System;
using System.Text.Json.Nodes;

namespace Viconomy.Network.Api
{
    public class ShopPurchaseUpdate
    {
        public string Name { get; set; }
        public string PlayerGuid { get; set; }
        public long ShopId { get; set; }
        public string NodeGuid { get; set; }
        public int Amount { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public int StallSlot { get; set; }

        public JsonObject ToJsonString()
        {
            return new JsonObject()
            {
                { "name", Name },
                { "playerGuid", PlayerGuid },
                { "shopId", ShopId },
                { "nodeGuid", NodeGuid },
                { "amount", Amount },
                { "x", X },
                { "y", Y },
                { "z", Z },
                { "stallSlot", StallSlot },
            };

        }
    }
}
