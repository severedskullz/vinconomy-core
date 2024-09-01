using System;
using System.IO;
using Vintagestory.API.Config;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Server;
using Viconomy.Registry;
using Viconomy.Trading;
using Viconomy.Network;
using System.Collections.Generic;
using Microsoft.Win32;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Viconomy.Database
{
    public class ViconDatabase
    {
        private string ConnStr;
        ICoreServerAPI api;

        public ViconDatabase(ICoreServerAPI api)
        {
            this.api = api;
            string filePath = Path.Combine(GamePaths.DataPath, "ModData", api.World.SavegameIdentifier);
            string path = Path.Combine(filePath, "vinconomy.db");

            this.ConnStr = "Data Source=" + path;
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(filePath);
                File.WriteAllBytes(path, new byte[0]);
            }

            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Shops (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Owner TEXT, OwnerName TEXT, X INTEGER, Y INTEGER, Z INTEGER, BroadcastWaypoint INTEGER, WaypointIcon TEXT, WaypointColor INTEGER);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Sales (ShopId INTEGER, Customer TEXT, Month INTEGER, Year INTEGER, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes TEXT);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Products ( X INTEGER, Y INTEGER, Z INTEGER, StallSlot INTEGER, ShopId INTEGER, ProductName TEXT, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes TEXT, TotalStock INTEGER, CurrencyName TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes TEXT, PRIMARY KEY (X,Y,Z, StallSlot));";
                cmd.ExecuteNonQuery();

                connection.Close();
            }
        }

        protected SqliteConnection GetConnection()
        {
            if (string.IsNullOrEmpty(this.ConnStr))
            {
                throw new ArgumentNullException("No Connection String Provided");
            }
            return new SqliteConnection(this.ConnStr);
        }


        public ShopRegistration UpdateShop(ShopRegistration shop)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Shops SET Name = @Name, Owner = @Owner, OwnerName = @OwnerName, X = @X, Y = @Y, Z = @Z, BroadcastWaypoint = @BroadcastWaypoint, WaypointIcon = @WaypointIcon, WaypointColor = @WaypointColor  WHERE ID = @ID;";
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = shop.ID;
                cmd.Parameters.Add("@Name", SqliteType.Text).Value = shop.Name;
                cmd.Parameters.Add("@Owner", SqliteType.Text).Value = shop.Owner;
                cmd.Parameters.Add("@OwnerName", SqliteType.Text).Value = shop.OwnerName;
                cmd.Parameters.Add("@BroadcastWaypoint", SqliteType.Integer).Value = shop.IsWaypointBroadcasted;


                if (shop.Position == null)
                {
                    cmd.Parameters.Add("@X", SqliteType.Integer).Value = DBNull.Value;
                    cmd.Parameters.Add("@Y", SqliteType.Integer).Value = DBNull.Value;
                    cmd.Parameters.Add("@Z", SqliteType.Integer).Value = DBNull.Value;
                }
                else
                {
                    cmd.Parameters.Add("@X", SqliteType.Integer).Value = shop.X;
                    cmd.Parameters.Add("@Y", SqliteType.Integer).Value = shop.Y;
                    cmd.Parameters.Add("@Z", SqliteType.Integer).Value = shop.Z;
                }

                if (!shop.IsWaypointBroadcasted)
                {
                    cmd.Parameters.Add("@WaypointIcon", SqliteType.Text).Value = DBNull.Value;
                    cmd.Parameters.Add("@WaypointColor", SqliteType.Integer).Value = DBNull.Value;
                }
                else
                {
                    cmd.Parameters.Add("@WaypointIcon", SqliteType.Text).Value = shop.WaypointIcon;
                    cmd.Parameters.Add("@WaypointColor", SqliteType.Integer).Value = shop.WaypointColor;
                }

                cmd.ExecuteNonQuery();
            }
            return shop;
        }

        public ShopRegistration AddShop(ShopRegistration shop)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO Shops VALUES (@ID, @Name, @Owner, @OwnerName, @X, @Y, @Z, false, NULL, NULL); SELECT last_insert_rowid();";
                cmd.Parameters.Add("@Name", SqliteType.Text).Value = shop.Name;
                cmd.Parameters.Add("@Owner", SqliteType.Text).Value = shop.Owner;
                cmd.Parameters.Add("@OwnerName", SqliteType.Text).Value = shop.OwnerName;

                if (shop.ID > 0) {
                    cmd.Parameters.Add("@ID", SqliteType.Integer).Value = shop.ID;
                }
                else
                {
                    cmd.Parameters.Add("@ID", SqliteType.Integer).Value = DBNull.Value;
                }


                if (shop.Position == null)
                {
                    cmd.Parameters.Add("@X", SqliteType.Integer).Value = DBNull.Value;
                    cmd.Parameters.Add("@Y", SqliteType.Integer).Value = DBNull.Value;
                    cmd.Parameters.Add("@Z", SqliteType.Integer).Value = DBNull.Value;
                } else
                {
                    cmd.Parameters.Add("@X", SqliteType.Integer).Value = shop.X;
                    cmd.Parameters.Add("@Y", SqliteType.Integer).Value = shop.Y;
                    cmd.Parameters.Add("@Z", SqliteType.Integer).Value = shop.Z;
                }
 

                int lastId = Convert.ToInt32(cmd.ExecuteScalar());
                shop.ID = lastId;
            }
            return shop;
        }

        public void SavePurchase(TradeResult purchaseResult)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = purchaseResult.sellingEntity.RegisterID;
                cmd.Parameters.Add("@Customer", SqliteType.Text).Value = purchaseResult.customer.PlayerUID;
                cmd.Parameters.Add("@Month", SqliteType.Integer).Value = purchaseResult.coreApi.World.Calendar.Month;
                cmd.Parameters.Add("@Year", SqliteType.Integer).Value = purchaseResult.coreApi.World.Calendar.Year;
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = purchaseResult.purchasedItems.Collectible.Code.ToString();
                cmd.Parameters.Add("@ProductQuantity", SqliteType.Text).Value = purchaseResult.purchasedItems.StackSize;
                cmd.Parameters.Add("@ProductAttributes", SqliteType.Text).Value = purchaseResult.purchasedItems.Attributes.ToJsonToken();
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = purchaseResult.purchasedCurrencyUsed.Collectible.Code.ToString();
                cmd.Parameters.Add("@CurrencyQuantity", SqliteType.Text).Value = purchaseResult.purchasedCurrencyUsed.StackSize;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Text).Value = purchaseResult.purchasedCurrencyUsed.Attributes.ToJsonToken();

                cmd.CommandText = @"SELECT Count(*) FROM Sales 
                                    WHERE ShopId = @ShopId 
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
                                    WHERE ShopId = @ShopId 
                                        AND Customer = @Customer
                                        AND Month = @Month
                                        AND Year = @Year
                                        AND ProductCode = @ProductCode
                                        AND CurrencyCode = @CurrencyCode";
                    cmd.ExecuteNonQuery();
                } else if (numRows == 0) {
                    cmd.CommandText = "INSERT INTO Sales VALUES (@ShopId, @Customer, @Month, @Year, @ProductCode, @ProductQuantity, @ProductAttributes, @CurrencyCode, @CurrencyQuantity, @CurrencyAttributes);";
                    cmd.ExecuteNonQuery();
                } else
                {
                    throw new ArgumentOutOfRangeException("Somehow have more than 1 sale record for purchase");
                }

                connection.Close();
            }
        }

        public Dictionary<string, List<LedgerEntry>> LoadSales(int shopId, int month, int year)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM Sales WHERE ShopId = @ShopId AND Month = @Month AND Year = @Year ORDER BY Customer";
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = shopId;
                cmd.Parameters.Add("@Month", SqliteType.Integer).Value = month;
                cmd.Parameters.Add("@Year", SqliteType.Integer).Value = year;
                SqliteDataReader reader = cmd.ExecuteReader();

                Dictionary<string, List<LedgerEntry>> entries = new Dictionary<string, List<LedgerEntry>>();
                while (reader.Read())
                {
                    //@ShopId, @Customer, @Month, @Year, @ProductCode, @ProductQuantity, @ProductAttributes, @CurrencyCode, @CurrencyQuantity, @CurrencyAttributes
                    LedgerEntry entry = new LedgerEntry();
                    string uuid = reader.GetString(1);
                    IServerPlayerData player = api.PlayerData.GetPlayerDataByUid(uuid);
                    if (player != null )
                    {
                        entry.Customer = player.LastKnownPlayername;
                    } else
                    {
                        entry.Customer = "Unknown Player";
                    }
                    
                    entry.ProductCode = reader.GetString(4);
                    entry.ProductQuantity = reader.GetInt32(5);
                    if (!reader.IsDBNull(6)) {
                        entry.ProductAttributes = reader.GetString(6);
                    }
                   
                    entry.CurrencyCode = reader.GetString(7);
                    entry.CurrencyQuantity = reader.GetInt32(8);
                    if (!reader.IsDBNull(9))
                    {
                        entry.CurrencyAttributes = reader.GetString(9);
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

        public void LoadShops(ShopRegistry registry)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM Shops";
                SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ShopRegistration reg = new ShopRegistration();
                    reg.ID = reader.GetInt32(0);
                    reg.Name = reader.GetString(1);
                    reg.Owner = reader.GetString(2);
                    reg.OwnerName = reader.GetString(3);
                   
                    if (reader.IsDBNull(4))
                    {
                        reg.Position = null;
                    } else
                    {
                        reg.X = reader.GetInt32(4);
                        reg.Y = reader.GetInt32(5);
                        reg.Z = reader.GetInt32(6);
                    }

                    reg.IsWaypointBroadcasted = reader.GetBoolean(7);
                    if (reg.IsWaypointBroadcasted)
                    {
                        reg.WaypointIcon = reader.GetString(8);
                        reg.WaypointColor = reader.GetInt32(9);
                    }
                    

                    registry.AddShop(reg);
                }

                connection.Close();
            }
        }

        public void DeleteShop(int ID)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Shops WHERE ID = @ID";
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = ID;
                int numAffected = cmd.ExecuteNonQuery();
            }
        }

        public void CleanupShops()
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Shops WHERE X IS NULL AND Y IS NULL AND Z IS NULL";
                int numAffected = cmd.ExecuteNonQuery();
            }
        }

        public ShopRegistration GetShop(int ID)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = ID;
                cmd.CommandText = "SELECT * FROM Shops WHERE ID = @ID";

                SqliteDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    ShopRegistration reg = new ShopRegistration();
                    reg.ID = reader.GetInt32(0);
                    reg.Name = reader.GetString(1);
                    reg.Owner = reader.GetString(2);
                    reg.OwnerName = reader.GetString(3);

                    if (reader.IsDBNull(4))
                    {
                        reg.Position = null;
                    }
                    else
                    {
                        reg.X = reader.GetInt32(4);
                        reg.Y = reader.GetInt32(5);
                        reg.Z = reader.GetInt32(6);
                    }


                    return reg;
                }
            }
            return null;
        }

        public void UpdateShopProduct(int registerID, BlockPos pos, int stallSlot, ItemStack product, int numItemsPerPurchase, ItemStack currency)
        {
            using (SqliteConnection connection = GetConnection())
            { 
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = registerID;
                cmd.Parameters.Add("@ProductName", SqliteType.Text).Value = product.GetName();
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = product.Collectible.Code.ToString();
                cmd.Parameters.Add("@ProductQuantity", SqliteType.Integer).Value = numItemsPerPurchase;
                cmd.Parameters.Add("@ProductAttributes", SqliteType.Text).Value = product.Attributes.ToJsonToken();
                cmd.Parameters.Add("@TotalStock", SqliteType.Integer).Value = product.StackSize;
                cmd.Parameters.Add("@CurrencyName", SqliteType.Text).Value = currency.GetName();
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = currency.Collectible.Code.ToString();
                cmd.Parameters.Add("@CurrencyQuantity", SqliteType.Integer).Value = currency.StackSize;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Text).Value = currency.Attributes.ToJsonToken();

                cmd.Parameters.Add("@StallSlot", SqliteType.Integer).Value = stallSlot;
                cmd.Parameters.Add("@X", SqliteType.Integer).Value = pos.X;
                cmd.Parameters.Add("@Y", SqliteType.Integer).Value = pos.Y;
                cmd.Parameters.Add("@Z", SqliteType.Integer).Value = pos.Z;





                cmd.CommandText = @"SELECT Count(*) FROM Products 
                                    WHERE  X = @X
                                        AND Y = @Y
                                        AND Z = @Z
                                        AND StallSlot = @StallSlot;";

                int numRows = Convert.ToInt32(cmd.ExecuteScalar());
                if (numRows == 1)
                {
                    cmd.CommandText = @"UPDATE Products SET 
                            ShopId = @ShopId,
                            ProductName = @ProductName,
                            ProductCode = @ProductCode,
                            ProductQuantity = @ProductQuantity,
                            ProductAttributes = @ProductAttributes,
                            TotalStock = @TotalStock,
                            CurrencyName = @CurrencyName,
                            CurrencyCode = @CurrencyCode,
                            CurrencyQuantity = @CurrencyQuantity,
                            CurrencyAttributes = @CurrencyAttributes
                        WHERE 
                            X = @X
                            AND Y = @Y
                            AND Z = @Z
                            AND StallSlot = @StallSlot;";
                    cmd.ExecuteNonQuery();
                } else
                {
                    cmd.CommandText = @"INSERT INTO Products VALUES( 
                            @X, @Y, @Z, @StallSlot, @ShopId, 
                            @ProductName, @ProductCode, @ProductQuantity, @ProductAttributes, @TotalStock,
                            @CurrencyName, @CurrencyCode, @CurrencyQuantity, @CurrencyAttributes)";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteShopProduct(BlockPos pos, int stallSlot)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();

                cmd.Parameters.Add("@StallSlot", SqliteType.Integer).Value = stallSlot;
                cmd.Parameters.Add("@X", SqliteType.Integer).Value = pos.X;
                cmd.Parameters.Add("@Y", SqliteType.Integer).Value = pos.Y;
                cmd.Parameters.Add("@Z", SqliteType.Integer).Value = pos.Z;

                cmd.CommandText = @"DELETE FROM Products WHERE StallSlot = @StallSlot AND X = @X AND Y = @Y AND Z = @Z";
                cmd.ExecuteNonQuery();
            }
        }

        Dictionary<int, ShopProductList> productListCache = new Dictionary<int, ShopProductList>();
        private long EXPIRE_TIME_MILLIS = 1000 * 60 * 10;

        //ShopId INTEGER,  -0
        //X INTEGER,  -1
        //Y INTEGER,  -2
        //Z INTEGER,  -3
        //StallSlot INTEGER,  -4
        //ProductName TEXT,  -5
        //ProductCode TEXT,  -6
        //ProductQuantity INTEGER,  -7
        //ProductAttributes TEXT,  -8
        //TotalStock INTEGER,  -9
        //CurrencyName TEXT,  -10
        //CurrencyCode TEXT,  -11
        //CurrencyQuantity INTEGER,  -12
        //CurrencyAttributes TEXT  -13

        public ShopProductList GetShopProducts(int ID)
        {
            if (productListCache.ContainsKey(ID))
            {
                ShopProductList listing =  productListCache[ID];
                // If the expiration timer is in the fiture, then simply return the cached copy.
                if (listing.ExpiresAt >= DateTime.UtcNow.Ticks)
                {
                    return listing;
                }
            }

            ShopProductList products = new ShopProductList();
            products.ExpiresAt = DateTime.UtcNow.Ticks + EXPIRE_TIME_MILLIS;
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM Stalls WHERE ID = @ID";

                SqliteDataReader reader = cmd.ExecuteReader();

                
                while (reader.Read())
                {
                    ShopProduct product = new ShopProduct();
                    product.ProductName = reader.GetString(5);
                    product.ProductCode = reader.GetString(6);
                    product.ProductQuantity = reader.GetInt32(7);
                    product.ProductAttributes = reader.GetString(8);
                    product.TotalStock = reader.GetInt32(9);
                    product.CurrencyName = reader.GetString(10);
                    product.CurrencyCode = reader.GetString(11);
                    product.CurrencyAmount = reader.GetInt32(12);
                    product.CurrencyAttributes = reader.GetString(13);
                    products.Products.Add(product);
                }
                
            }
            productListCache.Add(ID, products);
            return products;
        }
    }
}
