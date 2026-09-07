using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Commercially.Vinconomy.Util
{
    public class VinUtils
    {

       

        public static string SerializeToJson(object payload)
        {

            JsonSerializer serializer = new JsonSerializer();
            StringBuilder stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder))
            {
                serializer.Serialize(stringWriter, payload);
            }
            var jsonStr = stringBuilder.ToString();
            return jsonStr;
        }

        public static T DeserializeFromJson<T>(string payload)
        {
            JsonSerializer serializer = new JsonSerializer();
            using (var stringReader = new StringReader(payload))
            {
                using (var jsonReader = new JsonTextReader(stringReader))
                {
                    return serializer.Deserialize<T>(jsonReader);
                }
            }
        }

        public static ItemStack ResolveBlockOrItem(ICoreAPI api, string code, int size)
        {
            AssetLocation location = new AssetLocation(code);
            Item item = api.World.GetItem(location);
            if (item != null)
            {
                return new ItemStack(item, size);
            }

            Block block = api.World.GetBlock(location);
            if (block != null)
            {
                return new ItemStack(block, size);
            }
            return null;
        }

        public static ItemStack DeserializeItem(ICoreAPI api, string code, int quantity, byte[] attributes)
        {
            if (code == null)
            {
                return null;
            }

            ItemStack productStack = ResolveBlockOrItem(api, code, quantity);

            if (productStack == null)
                return null;

            try
            {
                if (attributes != null)
                {
                    TreeAttribute attr = new TreeAttribute();
                    attr.FromBytes(attributes);

                    // Remove transition state from any food items. SQL entries are the last time it was inserted and isnt updated
                    attr.RemoveAttribute("transitionstate");

                    productStack.Attributes = attr;

                }
            }
            catch (Exception ex) { }
            return productStack;
        }

        public static void LoadChunk(ICoreServerAPI api, int x, int y, int z, Action onLoaded)
        {
            int cx = x / GlobalConstants.ChunkSize;
            int cy = y / GlobalConstants.ChunkSize;
            int cz = z / GlobalConstants.ChunkSize;
            IServerChunk chunk = api.WorldManager.GetChunk(cx, cy, cz);

            //BlockPos pos = new BlockPos(x, y, z);
            //ChunkPos.ToChunkIndex(x, y, z);
            //IServerChunk chunk = api.WorldManager.GetChunk(pos);

            //https://discord.com/channels/302152934249070593/351624415039193098/1185988966479052850
            //var chunkCoord = blockPos / 32;
            //var chunkIndex = MapUtil.Index3dL(chunkCoord, chunkMapSizeX, chunkMapSizeZ);
            // ChunkPosFromChunkIndex3D 

            if (chunk != null)
            {
                onLoaded.Invoke();
            } else
            {
                ChunkLoadOptions options = new ChunkLoadOptions();
                options.OnLoaded += onLoaded;

                api.WorldManager.LoadChunkColumnPriority(cx, cz, options);
            }

          
        }

        public static ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string defaultDomain)
        {
            CollectibleObject collObj;
            var loc = AssetLocation.Create(stackAttr.GetString("code"), defaultDomain);
            if (stackAttr.GetString("type") == "item")
            {
                collObj = world.GetItem(loc);
            }
            else
            {
                collObj = world.GetBlock(loc);
            }

            ItemStack stack = new ItemStack(collObj, (int)stackAttr.GetDecimal("quantity", 1));
            var attr = (stackAttr["attributes"] as TreeAttribute)?.Clone();
            if (attr != null) stack.Attributes = attr;

            return stack;
        }

        public static bool IsMealContainer(ItemStack stack, ICoreAPI api)
        {
            if (stack == null)
                return false;

            if (stack.Block is IBlockMealContainer meal)
                return true;

            // Cooking Pot - always empty, block type changes when it is turned into claypot-cooked
            if (stack.Block is BlockCookingContainer pot)
                return true;

            if (stack.Block is BlockContainer container && stack?.Block?.Attributes["mealContainer"]?.AsBool() == true)
                return true;

            return false;
        }

        public static float GetMealContainerServings(ItemStack stack, ICoreAPI api)
        {
            if (stack?.Block is IBlockMealContainer meal)
                return meal.GetQuantityServings(api.World, stack);

            return 0;
        }

        public static bool IsLiquidContainer(ItemStack stack)
        {
            return stack?.Block is BlockLiquidContainerBase container;
        }

        public static bool IsEmptyLiquidContainer(ItemStack stack)
        {
            return IsLiquidContainer(stack) && ((BlockLiquidContainerBase)stack.Block).GetCurrentLitres(stack) == 0;
        }

        public static bool IsEmptyContainer(ItemStack stack, ICoreAPI api)
        {
            if (stack == null)
                return false;

            if (stack.Block is IBlockMealContainer meal)
                return meal.GetQuantityServings(api.World, stack) == 0 && meal.GetNonEmptyContents(api.World, stack).Length == 0;

            // Cooking Pot - always empty, block type changes when it is turned into claypot-cooked
            if (stack.Block is BlockCookingContainer pot)
                return true;

            if (stack.Block is BlockContainer container && stack?.Block?.Attributes["mealContainer"]?.AsBool() == true)
                return container.GetNonEmptyContents(api.World, stack).Length == 0;

            return false;
        }

        public static ItemStack[] GetContainerContents(ItemStack stack, ICoreAPI api)
        {
            if (stack == null)
                return null;

            if (stack.Block is IBlockMealContainer meal)
                return meal.GetNonEmptyContents(api.World, stack);

            if (stack.Block is BlockContainer container)
                return container.GetNonEmptyContents(api.World, stack);

            return null;
        }

        public static string GetRecipeCode(ItemStack stack, ICoreAPI api)
        {
            if (stack == null)
                return null;

            if (stack.Block is IBlockMealContainer meal)
                return meal.GetRecipeCode(api.World, stack);

            return null;
        }

        public static bool IsMergableContents(IWorldAccessor world, ItemStack[] source, ItemStack[] compareTo)
        {
            if (source == null || compareTo == null ||  source.Length == 0 || compareTo.Length == 0) return true;

            return IsMatchingContents(world, source, compareTo);
        }

        public static bool IsMatchingContents(IWorldAccessor world, ItemStack[] source, ItemStack[] compareTo)
        {
            if (source.Length != compareTo.Length)
            {
                return false;
            }

            for (int i = 0; i < compareTo.Length; i++)
            {
                ItemStack bowlStack = compareTo[i];
                ItemStack containerStack = source[i];

                //if (bowlStack.Id != containerStack.Id)
                if(!source[i].Equals(world, compareTo[i], GlobalConstants.IgnoredStackAttributes))
                {
                    return false;
                }
            }
            return true;
        }

        public static void SendSingleBool(ICoreClientAPI capi, BlockPos BlockEntityPosition, int packetId, bool isToggled)
        {
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(isToggled);
                data = ms.ToArray();
            }
            capi.Network.SendBlockEntityPacket(BlockEntityPosition, packetId, data);
        }

        public static bool IsCreativePlayer(IPlayer player)
        {
            return player.WorldData.CurrentGameMode == EnumGameMode.Creative && player.HasPrivilege("gamemode");
        }

        public static void Rotate(ItemStack[][] slots)
        {
            int size = slots.Length;

            int n = slots.Length;
            for (int i = 0; i < n / 2; i++)
            {
                for (int j = i; j < n - i - 1; j++)
                {
                    ItemStack tmp = slots[i][j];
                    slots[i][j] = slots[n - 1 - j][i];
                    slots[n - 1 - j][i] = slots[n - 1 - i][n - 1 - j];
                    slots[n - 1 - i][n - 1 - j] = slots[j][n - 1 - i];
                    slots[j][n - 1 - i] = tmp;
                }
            }
        }

        public static byte[] AttributesToBytes(ItemStack stack)
        {
            // All of this because Anego won't escape strings in their Json Tokenizer code... :/
            byte[] productAttributes;
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    stack.Attributes.ToBytes(writer);
                    writer.Flush();
                }
                productAttributes = ms.ToArray();
            }
            return productAttributes;
        }

        public static ITreeAttribute AttributesFromBytes(byte[] attributes)
        {
            // All of this because Anego won't escape strings in their Json Tokenizer code... :/
            TreeAttribute attrs = new TreeAttribute();
            SerializerUtil.FromBytes(attributes, (binaryReader) =>
            {
                attrs.FromBytes(binaryReader);
            });
            return null;
        }

    }



}
