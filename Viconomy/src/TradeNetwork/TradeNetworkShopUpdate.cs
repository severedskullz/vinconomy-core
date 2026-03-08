using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using Vinconomy.BlockEntities;
using Vinconomy.Network.JavaApi;
using Vinconomy.Registry;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vinconomy.TradeNetwork
{
    //TODO: Refactor this based on Block Pos instead. Getting duplicate ID constraint violations when they change shops from one to another and back again.
    // ID is 1, adds an update for 1, then change shop to 2, ID is 2 and adds an update for 2.... Giving us 2 updates for the same block position with 2 different IDs.
    public class TradeNetworkShopUpdate : Dictionary<int, ShopUpdate>
    {
        private ShopRegistry shopRegistry;

        public TradeNetworkShopUpdate(ShopRegistry shopRegistry)
        {
            this.shopRegistry = shopRegistry;
        }

        public void AddShopUpdate(BEVinconBase stall, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            int shopId = stall.ShopId;
            BlockPos pos = stall.Pos;

            //Ignore Admin shops not connected to a register
            if (shopId < 0)
                return;

            //If it is an admin shop, we want to remove the products entirely
            if (stall.IsAdminShop)
            {
                this[shopId].RemoveAll = true;
                this[shopId].Clear();
                return;
            }

            ShopRegistration reg = shopRegistry.GetShop(shopId);
            if (reg == null)
            {
                throw new Exception($"Shop with shop ID {shopId} does not exist");
            }

            if (!ContainsKey(shopId))
            {
                AddShopUpdate(reg);
            }

            this[shopId].AddStallUpdate(pos, stallSlot, product, numItemsPerPurchase, currency);
        }

        public void AddShopUpdate(ShopRegistration reg) {

            int id = reg.ID;

            if (!ContainsKey(id))
            {
                Add(id, new ShopUpdate(id));
            }

            ShopUpdate upd = this[id];
            upd.Name = reg.Name;
            upd.Owner = reg.OwnerName;
            upd.Description = reg.Description;
        }

        public int GetUpdateCount()
        {
            // TODO: Actually loop over values and get the size of each update?
            return Keys.Count;
        }

        public string ToJsonString()
        {
            JsonArray shops = new JsonArray();
            foreach (ShopUpdate shopUpdate in this.Values)
            {
                shops.Add(shopUpdate.ToJson());
            }

            return shops.ToString();
        }
    }
}