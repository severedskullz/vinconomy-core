using Commercially.Common.Database;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Network.Packets;
using Commercially.Vinconomy.Registry;
using Commercially.Vinconomy.Trading;
using Commercially.Vinconomy.Util;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using Vinconomy.Network.Packets;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Commercially.Vinconomy.Database
{
    public class VinconomyDatabase : BaseDatabase
    {

        Dictionary<long, ShopProductList> productListCache = new Dictionary<long, ShopProductList>();
        private long EXPIRE_TIME_MILLIS = 1000 * 60 * 10;

        public VinconomyDatabase(ICoreServerAPI api) : base(api, "commerce")
        {
        }

        public override void InitializeDB()
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Products ( Id INTEGER, X INTEGER, Y INTEGER, Z INTEGER, StallSlot INTEGER, ProductName TEXT, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes BLOB, TotalStock INTEGER, CurrencyName TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes BLOB, PRIMARY KEY (X,Y,Z, StallSlot));";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Sales (Id INTEGER, Customer TEXT, Month INTEGER, Year INTEGER, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes BLOB, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes BLOB);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS PendingSales (Id INTEGER PRIMARY KEY AUTOINCREMENT, X INTEGER, Y INTEGER, Z INTEGER, StallSlot INTEGER, ShopId INTEGER, Customer TEXT, ProductName TEXT, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes BLOB, CurrencyName TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes BLOB, Amount INTEGER);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS ShopPermissions (Id INTEGER, PlayerUid TEXT, PlayerName TEXT);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS ShopConfiguration (Id INTEGER, Description TEXT, ShortDescription TEXT, WebHook TEXT);";
                cmd.ExecuteNonQuery();

                connection.Close();
            }
        }

                

        public void SavePurchase(TradeResult purchaseResult) {
            TradeRequest req = purchaseResult.Request;
            SavePurchase(
                   req.ParentEntity?.Ownable.ID ?? 0,
                   req.Customer.PlayerUID,
                   req.ProductNeeded,
                   purchaseResult.TotalProductAmount,
                   req.CurrencyNeeded,
                   purchaseResult.TotalCurrencyAmount,
                   req.Api.World.Calendar.Month,
                   req.Api.World.Calendar.Year
               );
        
        }
        public void SavePurchase(PurchaseResult purchaseResult)
        {
            PurchaseRequest req = purchaseResult.Request;
            SavePurchase(
                req.ParentEntity?.Ownable.ID ?? 0,
                req.Customer.PlayerUID,
                req.ProductNeeded,
                purchaseResult.TotalProductAmount,
                req.CurrencyNeeded,
                purchaseResult.TotalCurrencyAmount,
                req.Api.World.Calendar.Month,
                req.Api.World.Calendar.Year
            );

        }

        public void SavePurchase(long ownableId, string customerUID, ItemStack product, int productAmount, ItemStack currency, int currencyAmount, int month, int year)
        {
            if (product == null || currency == null)
            {
                throw new ArgumentException("Could not persist purchase with no stock or currency");
            }

            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = ownableId;
                cmd.Parameters.Add("@Customer", SqliteType.Text).Value = customerUID;
                cmd.Parameters.Add("@Month", SqliteType.Integer).Value = month;
                cmd.Parameters.Add("@Year", SqliteType.Integer).Value = year;
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = product.Collectible.Code.ToString();
                cmd.Parameters.Add("@ProductQuantity", SqliteType.Text).Value = productAmount;
                cmd.Parameters.Add("@ProductAttributes", SqliteType.Text).Value = VinUtils.AttributesToBytes(product);
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = currency.Collectible.Code.ToString();
                cmd.Parameters.Add("@CurrencyQuantity", SqliteType.Text).Value = currencyAmount;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Text).Value = VinUtils.AttributesToBytes(currency);

                cmd.CommandText = @"SELECT Count(*) FROM Sales 
                                    WHERE Id = @Id 
                                        AND Customer = @Customer
                                        AND Month = @Month
                                        AND Year = @Year
                                        AND ProductCode = @ProductCode
                                        AND CurrencyCode = @CurrencyCode";

                int numRows = Convert.ToInt32(cmd.ExecuteScalar());
                if (numRows == 1)
                {
                    cmd.CommandText = @"UPDATE Sales 
                                    SET ProductQuantity = ProductQuantity + @ProductQuantity,
                                        CurrencyQuantity = CurrencyQuantity + @CurrencyQuantity 
                                    WHERE Id = @Id 
                                        AND Customer = @Customer
                                        AND Month = @Month
                                        AND Year = @Year
                                        AND ProductCode = @ProductCode
                                        AND CurrencyCode = @CurrencyCode";
                    cmd.ExecuteNonQuery();
                }
                else if (numRows == 0)
                {
                    cmd.CommandText = "INSERT INTO Sales VALUES (@Id, @Customer, @Month, @Year, @ProductCode, @ProductQuantity, @ProductAttributes, @CurrencyCode, @CurrencyQuantity, @CurrencyAttributes);";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Somehow have more than 1 sale record for purchase");
                }

                connection.Close();
            }
        }

        public Dictionary<string, List<LedgerEntry>> LoadSales(long shopId, int month, int year)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM Sales WHERE Id = @Id AND Month = @Month AND Year = @Year ORDER BY Customer";
                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = shopId;
                cmd.Parameters.Add("@Month", SqliteType.Integer).Value = month;
                cmd.Parameters.Add("@Year", SqliteType.Integer).Value = year;
                SqliteDataReader reader = cmd.ExecuteReader();

                Dictionary<string, List<LedgerEntry>> entries = new Dictionary<string, List<LedgerEntry>>();
                while (reader.Read())
                {
                    //@ShopId, @Customer, @Month, @Year, @ProductCode, @ProductQuantity, @ProductAttributes, @CurrencyCode, @CurrencyQuantity, @CurrencyAttributes
                    LedgerEntry entry = new LedgerEntry();
                    string uuid = reader.GetString(1);
                    IServerPlayerData player = Api.PlayerData.GetPlayerDataByUid(uuid);
                    if (player != null)
                    {
                        entry.Customer = player.LastKnownPlayername;
                    }
                    else
                    {
                        entry.Customer = "Unknown Player";
                    }

                    entry.ProductCode = reader.GetString(4);
                    entry.ProductQuantity = reader.GetInt32(5);
                    if (!reader.IsDBNull(6))
                    {
                        entry.ProductAttributes = (byte[]) reader.GetValue(6);
                    }

                    entry.CurrencyCode = reader.GetString(7);
                    entry.CurrencyQuantity = reader.GetInt32(8);
                    if (!reader.IsDBNull(9))
                    {
                        entry.CurrencyAttributes = (byte[])reader.GetValue(9);
                    }

                    if (!entries.ContainsKey(entry.Customer))
                    {
                        entries.Add(entry.Customer, new List<LedgerEntry>());
                    }
                    entries[entry.Customer].Add(entry);

                }

                connection.Close();

                return entries;
            }
        }

        public void SaveProductListing(IStallComponent shop, int stallSlot, ItemStack product, int productCount, ItemStack currency)
        {
            if (product == null || currency == null)
            {
                ClearStockForSlot(shop, stallSlot);
                return;
            }

            UpdateOrInsertStock(shop, stallSlot, product, productCount, currency);
        }


        public void ClearAllStock(IStallComponent shop)
        {
            if (shop != null)
            {
                using (SqliteConnection connection = GetConnection())
                {

                    connection.Open();
                    SqliteCommand cmd = connection.CreateCommand();
                    cmd.Parameters.Add("@Id", SqliteType.Integer).Value = shop.Ownable.ID;

                    cmd.CommandText = @"DELETE FROM Products 
                                    WHERE Id = @Id";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ClearStockForSlot(IStallComponent shop, int stallSlot)
        {
            if (shop != null)
            {
                using (SqliteConnection connection = GetConnection())
                {
                    BlockPos pos = shop.GetBlockEntity().Pos;

                    connection.Open();
                    SqliteCommand cmd = connection.CreateCommand();
                    cmd.Parameters.Add("@Id", SqliteType.Integer).Value = shop.Ownable.ID;
                    cmd.Parameters.Add("@StallSlot", SqliteType.Integer).Value = stallSlot;
                    cmd.Parameters.Add("@X", SqliteType.Integer).Value = pos.X;
                    cmd.Parameters.Add("@Y", SqliteType.Integer).Value = pos.Y;
                    cmd.Parameters.Add("@Z", SqliteType.Integer).Value = pos.Z;

                    cmd.CommandText = @"DELETE FROM Products 
                                    WHERE Id = @Id 
                                        AND X = @X
                                        AND Y = @Y
                                        AND Z = @Z
                                        AND StallSlot = @StallSlot";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateOrInsertStock(IStallComponent shop, int stallSlot, ItemStack product, int productCount, ItemStack currency)
        {
            using (SqliteConnection connection = GetConnection())
            {

                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();

                BlockPos pos = shop.GetBlockEntity().Pos;

                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = shop.Ownable.ID;
                cmd.Parameters.Add("@X", SqliteType.Integer).Value = pos.X;
                cmd.Parameters.Add("@Y", SqliteType.Integer).Value = pos.Y;
                cmd.Parameters.Add("@Z", SqliteType.Integer).Value = pos.Z;
                cmd.Parameters.Add("@StallSlot", SqliteType.Integer).Value = stallSlot;

                cmd.Parameters.Add("@ProductName", SqliteType.Text).Value = product.GetName();
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = product.Collectible.Code.ToString();
                cmd.Parameters.Add("@ProductQuantity", SqliteType.Integer).Value = product.StackSize;              
                cmd.Parameters.Add("@ProductAttributes", SqliteType.Blob).Value = VinUtils.AttributesToBytes(product);
                cmd.Parameters.Add("@TotalStock", SqliteType.Integer).Value = productCount;

                cmd.Parameters.Add("@CurrencyName", SqliteType.Text).Value = currency.GetName();
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = currency.Collectible.Code.ToString();
                cmd.Parameters.Add("@CurrencyQuantity", SqliteType.Integer).Value = currency.StackSize;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Blob).Value = VinUtils.AttributesToBytes(currency);



                cmd.CommandText = @"SELECT Count(*) FROM Products 
                                    WHERE Id = @Id 
                                        AND X = @X
                                        AND Y = @Y
                                        AND Z = @Z
                                        AND StallSlot = @StallSlot";

                int numRows = Convert.ToInt32(cmd.ExecuteScalar());
                if (numRows == 1)
                {
                    cmd.CommandText = @"UPDATE Products 
                                    SET ProductName = @ProductName,
                                        ProductCode = @ProductCode, 
                                        ProductAttributes = @ProductAttributes,
                                        ProductQuantity = @ProductQuantity,
                                        TotalStock = @TotalStock,
                                        CurrencyName = @CurrencyName,
                                        CurrencyCode = @CurrencyCode,
                                        CurrencyAttributes = @CurrencyAttributes,
                                        CurrencyQuantity = @CurrencyQuantity 
                                    WHERE Id = @Id 
                                        AND X = @X
                                        AND Y = @Y
                                        AND Z = @Z
                                        AND StallSlot = @StallSlot";
                    cmd.ExecuteNonQuery();
                }
                else if (numRows == 0)
                {
                    //X INTEGER, Y INTEGER, Z INTEGER, StallSlot INTEGER, ShopId INTEGER,
                    //ProductName TEXT, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes BLOB, TotalStock INTEGER,
                    //CurrencyName TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes BLOB
                    cmd.CommandText = "INSERT INTO Products VALUES (@Id, @X, @Y, @Z, @StallSlot,  @ProductName, @ProductCode, @ProductQuantity, @ProductAttributes, @TotalStock, @CurrencyName, @CurrencyCode, @CurrencyQuantity, @CurrencyAttributes);";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Somehow have more than 1 product record for stall");
                }

                connection.Close();
            }
        }

        public ShopProductList GetShopProducts(long ID)
        {
            long curTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (productListCache.ContainsKey(ID))
            {
                ShopProductList listing = productListCache[ID];
                // If the expiration timer is in the future, then simply return the cached copy.
                if (curTime <= listing.ExpiresAt)
                {
                    return listing;
                }
            }

            ShopProductList products = new ShopProductList();
            products.ExpiresAt = curTime + EXPIRE_TIME_MILLIS;
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM Products WHERE Id IN (SELECT Id FROM Ownables WHERE ParentId = @Id)";
                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = ID;
                SqliteDataReader reader = cmd.ExecuteReader();


                while (reader.Read())
                {
                    ShopProduct product = new ShopProduct
                    {
                        ProductName = reader.GetString(5),
                        ProductCode = reader.GetString(6),
                        ProductQuantity = reader.GetInt32(7),
                        ProductAttributes = (byte[])reader.GetValue(8),
                        TotalStock = reader.GetInt32(9),
                        CurrencyName = reader.GetString(10),
                        CurrencyCode = reader.GetString(11),
                        CurrencyQuantity = reader.GetInt32(12),
                        CurrencyAttributes = (byte[])reader.GetValue(13)
                    };
                    products.Products.Add(product);
                }

            }

            productListCache[ID] = products;
            return products;
        }

        public void SaveShopConfiguration(ShopConfiguration config)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = config.Id;
                cmd.Parameters.Add("@Description", SqliteType.Integer).Value = config.Description;
                cmd.Parameters.Add("@ShortDescription", SqliteType.Integer).Value = config.ShortDescription;
                cmd.Parameters.Add("@WebHook", SqliteType.Integer).Value = config.WebHook;

                cmd.CommandText = "SELECT Count(*) FROM ShopConfiguration WHERE Id = @Id";

                int numRows = Convert.ToInt32(cmd.ExecuteScalar());
                if (numRows == 1)
                {
                    cmd.CommandText = @"UPDATE ShopConfiguration 
                                    SET Description = @Description,
                                        ShortDescription = @ShortDescription, 
                                        Description = @Description,
                                        WebHook = @WebHook 
                                    WHERE Id = @Id";
                    cmd.ExecuteNonQuery();
                }
                else if (numRows == 0)
                {

                    cmd.CommandText = "INSERT INTO ShopConfiguration VALUES (@Id, @Description, @ShortDescription, @WebHook);";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Somehow have more than 1 product record for stall");
                }

            }
        }

        public Dictionary<long, ShopConfiguration> LoadShopConfiguration()
        {
            Dictionary<long, ShopConfiguration> configs = new Dictionary<long, ShopConfiguration>();
            using (SqliteConnection connection = GetConnection())
            {

                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();

                cmd.CommandText = "SELECT * FROM ShopConfiguration";
                SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ShopConfiguration product = new ShopConfiguration
                    {
                        Id = reader.GetInt32(0),
                        Description = reader.GetString(1),
                        ShortDescription = reader.GetString(2),
                        WebHook = reader.GetString(3)
                    };
                    configs.Add(product.Id, product);
                }

            }
            return configs;
        }
    }
}