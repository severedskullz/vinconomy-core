using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection.Metadata;
using Viconomy.Registry;
using Vinconomy.BlockEntities;
using Vinconomy.Network;
using Vinconomy.Registry;
using Vinconomy.Trading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vinconomy.Database
{
    public class ViconDatabase
    {
        private string ConnStr;
        ICoreServerAPI api;

        Dictionary<int, ShopProductList> productListCache = new Dictionary<int, ShopProductList>();
        private long EXPIRE_TIME_MILLIS = 1000 * 60 * 10;

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
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Shops (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Owner TEXT, OwnerName TEXT, X INTEGER, Y INTEGER, Z INTEGER, BroadcastWaypoint BOOLEAN, WaypointIcon TEXT, WaypointColor INTEGER, Description TEXT, ShortDescription TEXT, WebHook TEXT, StallAccess INTEGER NOT NULL DEFAULT 0);";
                cmd.ExecuteNonQuery();

                /*
                // No one should still be running 4.0 at this point... Keeping here for historical reasons, but lets not hit the DB  with a bad query each time we start.
                try
                {
                    // For backwards compatability to 4.0
                    cmd.CommandText = "ALTER TABLE Shops ADD COLUMN StallAccess INTEGER NOT NULL DEFAULT 0;";
                    cmd.ExecuteNonQuery();
                }
                catch{ }
                */

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Sales (ShopId INTEGER, Customer TEXT, Month INTEGER, Year INTEGER, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes TEXT);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Products ( X INTEGER, Y INTEGER, Z INTEGER, StallSlot INTEGER, ShopId INTEGER, ProductName TEXT, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes BLOB, TotalStock INTEGER, CurrencyName TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes BLOB, PRIMARY KEY (X,Y,Z, StallSlot));";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Products ( X INTEGER, Y INTEGER, Z INTEGER, StallSlot INTEGER, ShopId INTEGER, ProductName TEXT, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes BLOB, TotalStock INTEGER, CurrencyName TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes BLOB, PRIMARY KEY (X,Y,Z, StallSlot));";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS PendingSales (Id INTEGER PRIMARY KEY AUTOINCREMENT, X INTEGER, Y INTEGER, Z INTEGER, StallSlot INTEGER, ShopId INTEGER, Customer TEXT, ProductName TEXT, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes BLOB, CurrencyName TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes BLOB, Amount INTEGER);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS ShopPermissions (Id INTEGER, PlayerUid TEXT, PlayerName TEXT);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS CurrencyDefinitions (Id INTEGER PRIMARY KEY AUTOINCREMENT, ShopId INTEGER, CurrencyCode TEXT, CurrencyAttributes BLOB, IgnoreAttributes BOOLEAN, Supply INTEGER, IntervalType INTEGER, IntervalDuration INTEGER, IntervalPeriod INTEGER, IntervalAction INTEGER, IntervalActionValue INTEGER);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS ProductDefinitions (Id INTEGER PRIMARY KEY AUTOINCREMENT, ShopId INTEGER, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes BLOB, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes BLOB,  IgnoreAttributes BOOLEAN, Supply INTEGER, IntervalType INTEGER, IntervalDuration INTEGER, IntervalPeriod INTEGER, IntervalAction INTEGER, IntervalActionValue INTEGER, SupplyThreshold INTEGER, ThresholdScale INTEGER, CurrencyLowQuantity INTEGER, CurrencyHighQuantity INTEGER, IdealSupply INTEGER, MaxSupply INTEGER, SalesContribute BOOLEAN, UnlimitedSupply BOOLEAN, UnlimitedDemand BOOLEAN);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS PlayerCooldowns (ShopId INTEGER, PlayerUid TEXT, LastAccessed TIMESTAMP, PRIMARY KEY (ShopId, PlayerUid));";
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
                cmd.CommandText = @"UPDATE Shops SET Name = @Name, Owner = @Owner, OwnerName = @OwnerName, X = @X, Y = @Y, Z = @Z, BroadcastWaypoint = @BroadcastWaypoint, WaypointIcon = @WaypointIcon, WaypointColor = @WaypointColor, StallAccess = @StallAccess WHERE ID = @ID;";
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = shop.ID;
                cmd.Parameters.Add("@Name", SqliteType.Text).Value = shop.Name;
                cmd.Parameters.Add("@Owner", SqliteType.Text).Value = shop.Owner;
                cmd.Parameters.Add("@OwnerName", SqliteType.Text).Value = shop.OwnerName;
                cmd.Parameters.Add("@BroadcastWaypoint", SqliteType.Integer).Value = shop.IsWaypointBroadcasted;
                cmd.Parameters.Add("@StallAccess", SqliteType.Integer).Value = shop.StallPermissions;


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

                cmd = connection.CreateCommand();
                cmd.CommandText = @"DELETE FROM ShopPermissions WHERE ID = @ID;";
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = shop.ID;
                cmd.ExecuteNonQuery();

                foreach (ShopAccess entry in shop.Permissions.Values)
                {
                    cmd = connection.CreateCommand();
                    cmd.CommandText = @"INSERT INTO ShopPermissions VALUES( @ID, @PlayerUid, @PlayerName);";
                    cmd.Parameters.Add("@ID", SqliteType.Integer).Value = shop.ID;
                    cmd.Parameters.Add("@PlayerName", SqliteType.Text).Value = entry.PlayerName;
                    cmd.Parameters.Add("@PlayerUid", SqliteType.Text).Value = entry.PlayerUID;
                    cmd.ExecuteNonQuery();
                }

            }
            return shop;
        }

        public ShopRegistration AddShop(ShopRegistration shop)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO Shops VALUES (@ID, @Name, @Owner, @OwnerName, @X, @Y, @Z, false, NULL, NULL, NULL, NULL, NULL, 0); SELECT last_insert_rowid();";
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


        public void SavePurchase(GenericTradeResult purchaseResult, ItemStack product, ItemStack currency)
        {
            using (SqliteConnection connection = GetConnection())
            {
                GenericTradeRequest req = purchaseResult.Request;

                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = req.SellingEntity.ShopId;
                cmd.Parameters.Add("@Customer", SqliteType.Text).Value = req.Customer.PlayerUID;
                cmd.Parameters.Add("@Month", SqliteType.Integer).Value = req.Api.World.Calendar.Month;
                cmd.Parameters.Add("@Year", SqliteType.Integer).Value = req.Api.World.Calendar.Year;
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = product.Collectible.Code.ToString();
                cmd.Parameters.Add("@ProductQuantity", SqliteType.Text).Value = purchaseResult.TransferedProductTotal;
                cmd.Parameters.Add("@ProductAttributes", SqliteType.Text).Value = product.Attributes.ToJsonToken();
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = currency.Collectible.Code.ToString();
                cmd.Parameters.Add("@CurrencyQuantity", SqliteType.Text).Value = purchaseResult.TransferedCurrencyTotal;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Text).Value = currency.Attributes.ToJsonToken();

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
                }
                else if (numRows == 0)
                {
                    cmd.CommandText = "INSERT INTO Sales VALUES (@ShopId, @Customer, @Month, @Year, @ProductCode, @ProductQuantity, @ProductAttributes, @CurrencyCode, @CurrencyQuantity, @CurrencyAttributes);";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Somehow have more than 1 sale record for purchase");
                }

                connection.Close();
            }
        }

        public void SavePurchase(TradeResult purchaseResult)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = purchaseResult.sellingEntity.ShopId;
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
                    reg.ID = reader.GetInt32("Id");
                    reg.Name = reader.GetString("Name");
                    reg.Owner = reader.GetString("Owner");
                    reg.OwnerName = reader.GetString("OwnerName");
                   
                    if (reader.IsDBNull("X"))
                    {
                        reg.Position = null;
                    } else
                    {
                        reg.X = reader.GetInt32("X");
                        reg.Y = reader.GetInt32("Y");
                        reg.Z = reader.GetInt32("Z");
                    }

                    reg.IsWaypointBroadcasted = reader.GetBoolean("BroadcastWaypoint");
                    if (reg.IsWaypointBroadcasted)
                    {
                        reg.WaypointIcon = reader.GetString("WaypointIcon");
                        reg.WaypointColor = reader.GetInt32("WaypointColor");
                    }

                    if (!reader.IsDBNull("Description"))
                        reg.Description = reader.GetString("Description");
                    if (!reader.IsDBNull("ShortDescription"))
                        reg.ShortDescription = reader.GetString("ShortDescription");
                    if (!reader.IsDBNull("WebHook"))
                        reg.WebHook = reader.GetString("WebHook");

                    bool access = reader.GetBoolean("StallAccess");
                    reg.StallPermissions = access;

                    SqliteCommand permissions = connection.CreateCommand();
                    permissions.CommandText = "SELECT * FROM ShopPermissions WHERE ID = @ID";
                    permissions.Parameters.Add("@ID", SqliteType.Integer).Value = reg.ID;
                    SqliteDataReader permReader = permissions.ExecuteReader();

                    while (permReader.Read())
                    {
                        string uid = permReader.GetString("PlayerUid");
                        string name = permReader.GetString("PlayerName");
                        reg.AddAccess(uid, name);
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
                MemoryStream productStream = new MemoryStream();
                BinaryWriter productWriter = new BinaryWriter(productStream);
                product.Attributes.ToBytes(productWriter);
                byte[] productData = productStream.ToArray();

                MemoryStream currencyStream = new MemoryStream();
                BinaryWriter currencyWriter = new BinaryWriter(currencyStream);
                currency.Attributes.ToBytes(currencyWriter);
                byte[] currencyData = currencyStream.ToArray();

                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = registerID;
                cmd.Parameters.Add("@ProductName", SqliteType.Text).Value = product.GetName();
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = product.Collectible.Code.ToString();
                cmd.Parameters.Add("@ProductQuantity", SqliteType.Integer).Value = numItemsPerPurchase;
                cmd.Parameters.Add("@ProductAttributes", SqliteType.Blob).Value = productData;
                cmd.Parameters.Add("@TotalStock", SqliteType.Integer).Value = product.StackSize;
                cmd.Parameters.Add("@CurrencyName", SqliteType.Text).Value = currency.GetName();
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = currency.Collectible.Code.ToString();
                cmd.Parameters.Add("@CurrencyQuantity", SqliteType.Integer).Value = currency.StackSize;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Blob).Value = currencyData;

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



        public ShopProductList GetShopProducts(int ID)
        {
            if (productListCache.ContainsKey(ID))
            {
                ShopProductList listing =  productListCache[ID];
                // If the expiration timer is in the future, then simply return the cached copy.
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
                cmd.CommandText = "SELECT * FROM Products WHERE ShopID = @ShopId";
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = ID;
                SqliteDataReader reader = cmd.ExecuteReader();

                
                while (reader.Read())
                {
                    Registry.ShopProduct product = new Registry.ShopProduct();
                    product.ProductName = reader.GetString(5);
                    product.ProductCode = reader.GetString(6);
                    product.ProductQuantity = reader.GetInt32(7);
                    product.ProductAttributes = (byte[])reader.GetValue(8);
                    product.TotalStock = reader.GetInt32(9);
                    product.CurrencyName = reader.GetString(10);
                    product.CurrencyCode = reader.GetString(11);
                    product.CurrencyQuantity = reader.GetInt32(12);
                    product.CurrencyAttributes = (byte[])reader.GetValue(13);
                    products.Products.Add(product);
                }
                
            }

            productListCache[ID] = products;
            return products;
        }

        public void UpdateShopConfig(ShopRegistration shop)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE Shops SET Description = @Description, ShortDescription = @ShortDescription, WebHook = @WebHook WHERE ID = @ID;";
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = shop.ID;
                cmd.Parameters.Add("@Description", SqliteType.Text).Value = shop.Description;
                cmd.Parameters.Add("@ShortDescription", SqliteType.Text).Value = shop.ShortDescription;
                cmd.Parameters.Add("@WebHook", SqliteType.Text).Value = shop.WebHook;

                cmd.ExecuteNonQuery();
            }

        }

        private static CurrencyDefinition ReadCurrencyDefinition(SqliteDataReader reader)
        {
            /*
            Id INTEGER,
            ShopId INTEGER,
            CurrencyCode TEXT,
            CurrencyQuantity INTEGER,
            CurrencyAttributes BLOB,
            Amount INTEGER,
            IntervalType INTEGER,
            IntervalDuration INTEGER,
            IntervalPeriod INTEGER,
            IntervalAction INTEGER,
            IntervalActionValue INTEGER
            */

            CurrencyDefinition reg = new CurrencyDefinition();
            reg.Id = reader.GetInt32(0);
            reg.ShopId = reader.GetInt32(1);
            reg.CurrencyCode = reader.GetString(2);
            reg.CurrencyAttributes = (byte[])reader.GetValue(3);
            reg.IgnoreAttributes = reader.GetBoolean(4);
            reg.Supply = reader.GetInt32(5);
            reg.IntervalType = reader.GetInt32(6);
            reg.IntervalDuration = reader.GetInt32(7);
            reg.IntervalPeriod = reader.GetInt32(8);
            reg.IntervalAction = reader.GetInt32(9);
            reg.IntervalActionValue = reader.GetInt32(10);

            return reg;
        }

        private static EntryResultDefinition ReadCurrencyDefinitionAsResult(SqliteDataReader reader)
        {
        EntryResultDefinition reg = new EntryResultDefinition();
            reg.Id = reader.GetInt32(0);
            reg.ShopId = reader.GetInt32(1);
            reg.Code = reader.GetString(2);
            reg.Attributes = (byte[])reader.GetValue(3);
            reg.Supply = reader.GetInt32(5);
            reg.Type = BEVinconAdminRegister.TYPE_CURRENCY;
            return reg;
        }


        private static ProductDefinition ReadProductDefinition(SqliteDataReader reader)
        {
            /*
            Id INTEGER,
            ShopId INTEGER,
            ProductCode TEXT,
            ProductQuantity INTEGER,
            ProductAttributes BLOB,
            CurrencyCode TEXT,
            CurrencyQuantity INTEGER,
            CurrencyAttributes BLOB,
            Amount INTEGER,
            IntervalType INTEGER,
            IntervalDuration INTEGER,
            IntervalPeriod INTEGER,
            IntervalAction INTEGER,
            IntervalActionValue INTEGER,
            SupplyThreshold INTEGER,
            ThresholdScale INTEGER,
            CurrencyLowQuantity INTEGER,
            CurrencyHighQuantity INTEGER,
            IdealSupply INTEGER,
            MaxSupply INTEGER,
            SalesContribute BOOLEAN,
            UnlimitedSupply BOOLEAN,
            UnlimitedDemand BOOLEAN
            */

            ProductDefinition reg = new ProductDefinition();
            reg.Id = reader.GetInt32(0);
            reg.ShopId = reader.GetInt32(1);
            reg.ProductCode = reader.GetString(2);
            reg.ProductQuantity = reader.GetInt32(3);
            reg.ProductAttributes = (byte[])reader.GetValue(4);
            reg.CurrencyCode = reader.GetString(5);
            reg.CurrencyQuantity = reader.GetInt32(6);
            reg.CurrencyAttributes = (byte[])reader.GetValue(7);
            reg.IgnoreAttributes = reader.GetBoolean(8);
            reg.Supply = reader.GetInt32(9);
            reg.IntervalType = reader.GetInt32(10);
            reg.IntervalDuration = reader.GetInt32(11);
            reg.IntervalPeriod = reader.GetInt32(12);
            reg.IntervalAction = reader.GetInt32(13);
            reg.IntervalActionValue = reader.GetInt32(14);
            reg.SupplyThreshold = reader.GetInt32(15);
            reg.ThresholdScale = reader.GetInt32(16);
            reg.CurrencyLowQuantity = reader.GetInt32(17);
            reg.CurrencyHighQuantity = reader.GetInt32(18);
            reg.IdealSupply = reader.GetInt32(19);
            reg.MaxSupply = reader.GetInt32(20);
            reg.SalesContribute = reader.GetBoolean(21);
            reg.UnlimitedSupply = reader.GetBoolean(22);
            reg.UnlimitedDemand = reader.GetBoolean(23);
            return reg;
        }

        private static EntryResultDefinition ReadProductDefinitionAsResult(SqliteDataReader reader)
        {
            EntryResultDefinition reg = new EntryResultDefinition();
            reg.Id = reader.GetInt32(0);
            reg.ShopId = reader.GetInt32(1);
            reg.Code = reader.GetString(2);
            reg.Attributes = (byte[])reader.GetValue(4);
            reg.Supply = reader.GetInt32(9);
            reg.Type = BEVinconAdminRegister.TYPE_PRODUCT;
            return reg;
        }


        public List<EntryResultDefinition> GetProductDefinitionResults(int shopId, string item = null)
        {
            List<EntryResultDefinition> list = new List<EntryResultDefinition>();
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ShopID", SqliteType.Integer).Value = shopId;
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = item;
                cmd.CommandText = "SELECT * FROM ProductDefinitions WHERE ShopId = @ShopID";
                if (item != null && item != "")
                    cmd.CommandText += " AND ProductCode = @ProductCode";

                SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(ReadProductDefinitionAsResult(reader));
                }
            }
            return list;
        }

        public List<ProductDefinition> GetProductDefinitions(string item, int shopId)
        {
            List<ProductDefinition> list = new List<ProductDefinition>();
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = item;
                cmd.Parameters.Add("@ShopID", SqliteType.Integer).Value = shopId;
                cmd.CommandText = "SELECT * FROM ProductDefinitions WHERE ProductCode = @ProductCode AND ShopId = @ShopID";

                SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(ReadProductDefinition(reader));
                }
            }

            return list;
        }

        public ProductDefinition GetProductDefinition(int entryId, int shopId) 
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = entryId;
                cmd.Parameters.Add("@ShopID", SqliteType.Integer).Value = shopId;
                cmd.CommandText = "SELECT * FROM ProductDefinitions WHERE ID = @ID AND ShopId = @ShopID";

                SqliteDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return ReadProductDefinition(reader);
                }
            }

            return null;
        }

        public ProductDefinition CreateOrUpdateProductDefinition(ProductDefinition def)
        {
            using (SqliteConnection connection = GetConnection())
            {

                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = def.Id;
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = def.ShopId;
                cmd.Parameters.Add("@ProductCode", SqliteType.Text).Value = def.ProductCode;
                cmd.Parameters.Add("@ProductQuantity", SqliteType.Text).Value = def.ProductQuantity;
                cmd.Parameters.Add("@ProductAttributes", SqliteType.Text).Value = def.ProductAttributes;
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = def.CurrencyCode;
                cmd.Parameters.Add("@CurrencyQuantity", SqliteType.Text).Value = def.CurrencyQuantity;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Text).Value = def.CurrencyAttributes;
                cmd.Parameters.Add("@IgnoreAttributes", SqliteType.Integer).Value = def.IgnoreAttributes;
                cmd.Parameters.Add("@Supply", SqliteType.Integer).Value = def.Supply;
                cmd.Parameters.Add("@IntervalType", SqliteType.Integer).Value = def.IntervalType;
                cmd.Parameters.Add("@IntervalDuration", SqliteType.Integer).Value = def.IntervalDuration;
                cmd.Parameters.Add("@IntervalPeriod", SqliteType.Integer).Value = def.IntervalPeriod;
                cmd.Parameters.Add("@IntervalAction", SqliteType.Integer).Value = def.IntervalAction;
                cmd.Parameters.Add("@IntervalActionValue", SqliteType.Integer).Value = def.IntervalActionValue;
                cmd.Parameters.Add("@SupplyThreshold", SqliteType.Integer).Value = def.SupplyThreshold;
                cmd.Parameters.Add("@ThresholdScale", SqliteType.Integer).Value = def.ThresholdScale;
                cmd.Parameters.Add("@CurrencyLowQuantity", SqliteType.Integer).Value = def.CurrencyLowQuantity;
                cmd.Parameters.Add("@CurrencyHighQuantity", SqliteType.Integer).Value = def.CurrencyHighQuantity;
                cmd.Parameters.Add("@IdealSupply", SqliteType.Integer).Value = def.IdealSupply;
                cmd.Parameters.Add("@MaxSupply", SqliteType.Integer).Value = def.MaxSupply;
                cmd.Parameters.Add("@SalesContribute", SqliteType.Integer).Value = def.SalesContribute;
                cmd.Parameters.Add("@UnlimitedSupply", SqliteType.Integer).Value = def.UnlimitedSupply;
                cmd.Parameters.Add("@UnlimitedDemand", SqliteType.Integer).Value = def.UnlimitedDemand;


                if (def.Id >  0)
                {
                    cmd.CommandText = "UPDATE ProductDefinitions SET ProductCode = @ProductCode, ProductQuantity = @ProductQuantity, ProductAttributes = @ProductAttributes, CurrencyCode = @CurrencyCode,"+
                        "CurrencyQuantity = @CurrencyQuantity, CurrencyAttributes = @CurrencyAttributes, IgnoreAttributes = @IgnoreAttributes, Supply = @Supply, IntervalType = @IntervalType, IntervalDuration = @IntervalDuration,"+
                        "IntervalPeriod = @IntervalPeriod, IntervalAction = @IntervalAction, IntervalActionValue = @IntervalActionValue, SupplyThreshold = @SupplyThreshold, ThresholdScale = @ThresholdScale,"+
                        "CurrencyLowQuantity = @CurrencyLowQuantity, CurrencyHighQuantity = @CurrencyHighQuantity, IdealSupply = @IdealSupply, MaxSupply = @MaxSupply, SalesContribute = @SalesContribute, "+
                        "UnlimitedSupply = @UnlimitedSupply, UnlimitedDemand = @UnlimitedDemand WHERE ID = @Id AND ShopId = @ShopId;";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    cmd.CommandText = "INSERT INTO ProductDefinitions VALUES (NULL, @ShopId, @ProductCode, @ProductQuantity, @ProductAttributes, @CurrencyCode, @CurrencyQuantity, @CurrencyAttributes, @IgnoreAttributes," +
                       "@Supply, @IntervalType, @IntervalDuration, @IntervalPeriod, @IntervalAction, @IntervalActionValue, @SupplyThreshold, @ThresholdScale, @CurrencyLowQuantity, @CurrencyHighQuantity," +
                       "@IdealSupply, @MaxSupply, @SalesContribute, @UnlimitedSupply, @UnlimitedDemand); SELECT last_insert_rowid();";

                    int lastId = Convert.ToInt32(cmd.ExecuteScalar());
                    def.Id = lastId;
                }

                connection.Close();
            }

            return def;
        }

        public void DeleteProductDefinition(ProductDefinition def)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();

                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = def.Id;
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = def.ShopId;

                cmd.CommandText = @"DELETE FROM ProductDefinitions WHERE ID = @ID AND ShopId = @ShopId;";
                cmd.ExecuteNonQuery();
            }
        }


        public List<EntryResultDefinition> GetCurrencyDefinitionResults(int shopId, string item = null)
        {
            List<EntryResultDefinition> list = new List<EntryResultDefinition>();
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ShopID", SqliteType.Integer).Value = shopId;
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = item;
                cmd.CommandText = "SELECT * FROM CurrencyDefinitions WHERE ShopId = @ShopID";
                if (item != null && item != "")
                    cmd.CommandText += " AND CurrencyCode = @CurrencyCode";

                SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(ReadCurrencyDefinitionAsResult(reader));
                }
            }
            return list;
        }

        public List<CurrencyDefinition> GetCurrencyDefinitions(string item, int shopId)
        {
            List<CurrencyDefinition> list = new List<CurrencyDefinition>();
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = item;
                cmd.Parameters.Add("@ShopID", SqliteType.Integer).Value = shopId;
                cmd.CommandText = "SELECT * FROM CurrencyDefinitions WHERE CurrencyCode = @CurrencyCode AND ShopId = @ShopID";

                SqliteDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    list.Add(ReadCurrencyDefinition(reader));
                }

            }

            return list;
        }

        public CurrencyDefinition GetCurrencyDefinition(int entryId, int shopId)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = entryId;
                cmd.Parameters.Add("@ShopID", SqliteType.Integer).Value = shopId;
                cmd.CommandText = "SELECT * FROM CurrencyDefinitions WHERE ID = @ID AND ShopId = @ShopID";

                SqliteDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return ReadCurrencyDefinition(reader);
                }
            }

            return null;
        }



        public CurrencyDefinition CreateOrUpdateCurrencyDefinition(CurrencyDefinition def)
        {
            using (SqliteConnection connection = GetConnection())
            {

                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = def.Id;
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = def.ShopId;
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = def.CurrencyCode;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Text).Value = def.CurrencyAttributes;
                cmd.Parameters.Add("@IgnoreAttributes", SqliteType.Integer).Value = def.IgnoreAttributes;
                cmd.Parameters.Add("@Supply", SqliteType.Integer).Value = def.Supply;
                cmd.Parameters.Add("@IntervalType", SqliteType.Integer).Value = def.IntervalType;
                cmd.Parameters.Add("@IntervalDuration", SqliteType.Integer).Value = def.IntervalDuration;
                cmd.Parameters.Add("@IntervalPeriod", SqliteType.Integer).Value = def.IntervalPeriod;
                cmd.Parameters.Add("@IntervalAction", SqliteType.Integer).Value = def.IntervalAction;
                cmd.Parameters.Add("@IntervalActionValue", SqliteType.Integer).Value = def.IntervalActionValue;



                if (def.Id > 0)
                {
                    cmd.CommandText = "UPDATE CurrencyDefinitions SET CurrencyCode = @CurrencyCode, CurrencyAttributes = @CurrencyAttributes, IgnoreAttributes = @IgnoreAttributes, Supply = @Supply, IntervalType = @IntervalType, IntervalDuration = @IntervalDuration,"+
                        "IntervalPeriod = @IntervalPeriod, IntervalAction = @IntervalAction, IntervalActionValue = @IntervalActionValue WHERE ID = @Id AND ShopId = @ShopId;";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    cmd.CommandText = "INSERT INTO CurrencyDefinitions VALUES (NULL, @ShopId, @CurrencyCode, @CurrencyAttributes, @IgnoreAttributes, @Supply," +
                        "@IntervalType, @IntervalDuration, @IntervalPeriod, @IntervalAction, @IntervalActionValue); SELECT last_insert_rowid();";

                    int lastId = Convert.ToInt32(cmd.ExecuteScalar());
                    def.Id = lastId;
                }

                connection.Close();
            }

            return def;
        }

        public void DeleteCurrencyDefinition(CurrencyDefinition def)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();

                cmd.Parameters.Add("@Id", SqliteType.Integer).Value = def.Id;
                cmd.Parameters.Add("@ShopId", SqliteType.Integer).Value = def.ShopId;

                cmd.CommandText = "DELETE FROM CurrencyDefinitions WHERE ID = @ID AND ShopId = @ShopId;";
                cmd.ExecuteNonQuery();
            }

        }

    }
}
