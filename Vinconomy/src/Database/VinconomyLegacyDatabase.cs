using Commercially.Common.Database;
using Commercially.Vinconomy.Interfaces;
using Commercially.Vinconomy.Util;
using Microsoft.Data.Sqlite;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Commercially.Vinconomy.Database
{
    public class VinconomyLegacyDatabase : BaseDatabase
    {

        public VinconomyLegacyDatabase(ICoreServerAPI api) : base(api, "vinconomy")
        {
        }

        public override void InitializeDB()
        {
            
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

        public void ReimburseShopsForPlayer(IPlayer player, out int totalReimbursed)
        {
            totalReimbursed = 0;
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM Shops WHERE Owner = @Owner;";
                cmd.Parameters.Add("@Owner", SqliteType.Text).Value = player.PlayerUID;
                int numShops = Convert.ToInt32(cmd.ExecuteScalar());
                totalReimbursed = numShops;

                AssetLocation asset = new AssetLocation("vinconomy:shopcoupon");
                Item item = Api.World.GetItem(asset);
                ItemStack coupons = new ItemStack(item, numShops);
                Api.World.SpawnItemEntity(coupons, new Vec3d(player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z));
            }
        }

        public void ReimburseStallsForPlayer(IPlayer player, out int totalReimbursed)
        {
            totalReimbursed = 0;
            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM (SELECT 1 FROM Products WHERE ShopId IN (SELECT Id FROM Shops WHERE Owner = @Owner) GROUP BY X, Y, Z);";
                cmd.Parameters.Add("@Owner", SqliteType.Text).Value = player.PlayerUID;
                int numStalls = Convert.ToInt32(cmd.ExecuteScalar());
                totalReimbursed = numStalls;

                AssetLocation asset = new AssetLocation("vinconomy:stallcoupon");
                Item item = Api.World.GetItem(asset);
                ItemStack coupons = new ItemStack(item, numStalls);
                Api.World.SpawnItemEntity(coupons, new Vec3d(player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z));
            }
        }

        public void ReimburseProductForPlayer(IPlayer player, out int totalReimbursed, out int totalItemsSkipped)
        {
            totalItemsSkipped = 0;
            totalReimbursed = 0;

            using (SqliteConnection connection = GetConnection())
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM Products WHERE ShopId IN (SELECT Id FROM Shops WHERE Owner = @Owner)";
                cmd.Parameters.Add("@Owner", SqliteType.Text).Value = player.PlayerUID;
                SqliteDataReader reader = cmd.ExecuteReader();


                while (reader.Read())
                {

                    string productCode = reader.GetString(6);
                    int productQuantity = reader.GetInt32(7);
                    byte[] productAttributes = (byte[])reader.GetValue(8);
                    int totalStock = reader.GetInt32(9);

                    AssetLocation asset = new AssetLocation(productCode);
                    Item item = Api.World.GetItem(asset);
                    ItemStack product = null;
                    if (item != null)
                    {
                        product = new ItemStack(item, productQuantity);
                    }
                    else
                    {
                        Block block = Api.World.GetBlock(asset);
                        if (block != null)
                        {
                            product = new ItemStack(block, productQuantity);
                        }

                    }

                    if (product?.ResolveBlockOrItem(Api.World) == true)
                    {
                        ITreeAttribute attributes = VinUtils.AttributesFromBytes(productAttributes);
                        if (attributes != null)
                        {
                            product.Attributes = attributes;
                        }

                        Api.World.SpawnItemEntity(product, new Vec3d(player.Entity.Pos.X, player.Entity.Pos.Y, player.Entity.Pos.Z));
                        totalReimbursed++;
                    } else
                    {
                        Api.World.Logger.Error($"Could not reimburse {totalStock}x {productCode} for player {player.PlayerName}");
                        totalItemsSkipped++;
                    }
                }
            }

        }
    }
}