using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;
using static Vintagestory.Common.VSWebClient;

namespace Viconomy.Util
{
    public class VinUtils
    {
        /*
         * https://stackoverflow.com/questions/42519/how-do-you-rotate-a-two-dimensional-array
        def rotate(matrix):
            size = len(matrix)
            layer_count = size / 2

            for layer in range(0, layer_count):
                first = layer
                last = size - first - 1

                for element in range(first, last):
                    offset = element - first

                    top = matrix[first][element]
                    right_side = matrix[element][last]
                    bottom = matrix[last][last - offset]
                    left_side = matrix[last - offset][first]

                    matrix[first][element] = left_side
                    matrix[element][last] = top
                    matrix[last][last - offset] = right_side
                    matrix[last - offset][first] = bottom
        */
        public static void Rotate(ItemStack[][] slots)
        {
            int size = slots.Length;

            int n = slots.Length;
            for (int i = 0; i < n / 2; i++)
            {
                for (int j = i; j < n - i - 1; j++)
                {
                    /*
                    ItemStack tmp = slots[i][j];
                    slots[i][j] = slots[j][n - i - 1];
                    slots[j][n - i - 1] = slots[n - i - 1][n - j - 1];
                    slots[n - i - 1][n - j - 1] = slots[n - j - 1][i];
                    slots[n - j - 1][i] = tmp;
                    */

                    ItemStack tmp = slots[i][j];
                    slots[i][j] = slots[n - 1 - j][i];
                    slots[n - 1 - j][i] = slots[n - 1 - i][n - 1 - j];
                    slots[n - 1 - i][n - 1 - j] = slots[j][n - 1 - i];
                    slots[j][n - 1 - i] = tmp;
                }
            }
        }

        public static Task GetAsync(string rootUrl, PostCompleteHandler onFinished, string apiKey = null)
        {
            return Task.Run(async delegate
            {
                try
                {
                    HttpRequestMessage request = CreateRequest(rootUrl, HttpMethod.Get, null, apiKey);
                    HttpResponseMessage res = await Inst.SendAsync(request, CancellationToken.None);
                    string response = await res.Content.ReadAsStringAsync();
                    HandleResponse(onFinished, res, response);
                }
                catch (Exception ex)
                {
                    CompletedArgs args = new CompletedArgs
                    {
                        State = CompletionState.Error,
                        ErrorMessage = ex.Message
                    };
                    onFinished(args);
                }

            });
        }

        public static Task PostAsync(string rootUrl, string body, PostCompleteHandler onFinished, string apiKey = null)
        {
            return Task.Run(async delegate
            {
                try
                {
                    HttpRequestMessage request = CreateRequest(rootUrl, HttpMethod.Post, body, apiKey);
                    HttpResponseMessage res = await Inst.SendAsync(request, CancellationToken.None);
                    string response = await res.Content.ReadAsStringAsync();
                    if (onFinished != null)
                        HandleResponse(onFinished, res, response);
                }
                catch (Exception ex)
                {
                    CompletedArgs args = new CompletedArgs
                    {
                        State = CompletionState.Error,
                        ErrorMessage = ex.Message
                    };
                    if (onFinished != null) onFinished(args);
                }

            });
        }



        public static Task PutAsync(string rootUrl, string body, PostCompleteHandler onFinished, string apiKey = null)
        {
            return Task.Run(async delegate
            {
                try
                {
                    HttpRequestMessage request = CreateRequest(rootUrl, HttpMethod.Put, body, apiKey);
                    HttpResponseMessage res = await Inst.SendAsync(request, CancellationToken.None);
                    string response = await res.Content.ReadAsStringAsync();
                    HandleResponse(onFinished, res, response);
                }
                catch (Exception ex)
                {
                    CompletedArgs args = new CompletedArgs
                    {
                        State = CompletionState.Error,
                        ErrorMessage = ex.Message
                    };
                    onFinished(args);
                }

            });
        }

        public static Task PatchAsync(string rootUrl, string body, PostCompleteHandler onFinished, string apiKey = null)
        {
            return Task.Run(async delegate
            {
                try
                {
                    HttpRequestMessage request = CreateRequest(rootUrl, HttpMethod.Patch, body, apiKey);
                    HttpResponseMessage res = await Inst.SendAsync(request, CancellationToken.None);
                    string response = await res.Content.ReadAsStringAsync();
                    HandleResponse(onFinished, res, response);
                }
                catch (Exception ex)
                {
                    CompletedArgs args = new CompletedArgs
                    {
                        State = CompletionState.Error,
                        ErrorMessage = ex.Message
                    };
                    onFinished(args);
                }

            });
        }

        public static Task DeleteAsync(string rootUrl, PostCompleteHandler onFinished, string apiKey = null)
        {
            return Task.Run(async delegate
            {
                try
                {
                    HttpRequestMessage request = CreateRequest(rootUrl, HttpMethod.Delete, null, apiKey);
                    HttpResponseMessage res = await Inst.SendAsync(request, CancellationToken.None);
                    string response = await res.Content.ReadAsStringAsync();
                    HandleResponse(onFinished, res, response);
                }
                catch (Exception ex)
                {
                    CompletedArgs args = new CompletedArgs
                    {
                        State = CompletionState.Error,
                        ErrorMessage = ex.Message
                    };
                    onFinished(args);
                }

            });
        }

        private static HttpRequestMessage CreateRequest(string rootUrl, HttpMethod method, string body, string apiKey)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, rootUrl) { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionOrLower };
            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json"); ;
            }
            if (apiKey != null)
            {
                request.Headers.Add("X-API-KEY", apiKey);
            }

            return request;
        }

        private static void HandleResponse(PostCompleteHandler onFinished, HttpResponseMessage res, string response)
        {
            CompletedArgs args = new CompletedArgs
            {
                State = ((!res.IsSuccessStatusCode) ? CompletionState.Error : CompletionState.Good),
                StatusCode = (int)res.StatusCode,
                Response = response,
                ErrorMessage = res.ReasonPhrase
            };
            onFinished(args);
        }

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

        public static ItemStack DeserializeProduct(ICoreAPI api, string code, int quantity, byte[] attributes)
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
    }



}
