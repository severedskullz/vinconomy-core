using System;
using System.IO;
using Vintagestory.API.Config;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Server;
using Viconomy.Registry;
using Viconomy.Trading;

namespace Viconomy.src.Database
{
    public class ViconDatabase
    {
        private string ConnStr;

        public ViconDatabase(ICoreServerAPI api)
        {

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
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Shops (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Owner TEXT, OwnerName TEXT, X INTEGER, Y INTEGER, Z INTEGER);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Sales (ShopId INTEGER, Customer TEXT, Month INTEGER, Year INTEGER, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes TEXT);";
                cmd.ExecuteNonQuery();

                cmd.CommandText = "CREATE TABLE IF NOT EXISTS Stalls (ShopId INTEGER, X INTEGER, Y INTEGER, Z INTEGER, StallSlot INTEGER, ProductCode TEXT, ProductQuantity INTEGER, ProductAttributes TEXT, CurrencyCode TEXT, CurrencyQuantity INTEGER, CurrencyAttributes TEXT);";
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
                cmd.CommandText = "UPDATE Shops SET Name = @Name, Owner = @Owner, OwnerName = @OwnerName, X = @X, Y = @Y, Z = @Z WHERE ID = @ID;";
                cmd.Parameters.Add("@ID", SqliteType.Integer).Value = shop.ID;
                cmd.Parameters.Add("@Name", SqliteType.Text).Value = shop.Name;
                cmd.Parameters.Add("@Owner", SqliteType.Text).Value = shop.Owner;
                cmd.Parameters.Add("@OwnerName", SqliteType.Text).Value = shop.OwnerName;

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

                int lastId = Convert.ToInt32(cmd.ExecuteScalar());
                shop.ID = lastId;
            }
            return shop;
        }

        public ShopRegistration AddShop(ShopRegistration shop)
        {
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO Shops VALUES (NULL, @Name, @Owner, @OwnerName, @X, @Y, @Z); SELECT last_insert_rowid();";
                cmd.Parameters.Add("@Name", SqliteType.Text).Value = shop.Name;
                cmd.Parameters.Add("@Owner", SqliteType.Text).Value = shop.Owner;
                cmd.Parameters.Add("@OwnerName", SqliteType.Text).Value = shop.OwnerName;

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
                cmd.Parameters.Add("@ProductAttributes", SqliteType.Text).Value = DBNull.Value; //purchaseResult.purchasedItems.Attributes.ToJsonToken().GetHashCode();
                cmd.Parameters.Add("@CurrencyCode", SqliteType.Text).Value = purchaseResult.purchasedCurrencyUsed.Collectible.Code.ToString();
                cmd.Parameters.Add("@CurrencyQuantity", SqliteType.Text).Value = purchaseResult.purchasedCurrencyUsed.StackSize;
                cmd.Parameters.Add("@CurrencyAttributes", SqliteType.Text).Value = DBNull.Value; //purchaseResult.purchasedCurrencyUsed.Attributes.ToJsonToken().GetHashCode();

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
                    //throw new ArgumentOutOfRangeException("Somehow have more than 1 sale record for purchase");
                }

                connection.Close();
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
    }
}
