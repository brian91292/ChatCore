using ChatCore.Interfaces;
using ChatCore.Models;
using ChatCore.SimpleJSON;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatCore.Services.Twitch
{
    public class FFZDataProvider : IChatResourceProvider<ChatResourceData>
    {
        public ConcurrentDictionary<string, ChatResourceData> Resources { get; } = new ConcurrentDictionary<string, ChatResourceData>();
        public FFZDataProvider(ILogger<FFZDataProvider> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        private ILogger _logger;
        private HttpClient _httpClient;

        public async Task<bool> TryRequestResources(string category)
        {
            bool isGlobal = string.IsNullOrEmpty(category);

            try
            {
                _logger.LogDebug($"Requesting FFZ {(isGlobal ? "global " : "")}emotes{(isGlobal ? "." : $" for channel {category}")}.");
                using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, isGlobal ? "https://api.frankerfacez.com/v1/set/global" : $"https://api.frankerfacez.com/v1/room/{category}"))
                {
                    var resp = await _httpClient.SendAsync(msg);
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Unsuccessful status code when requesting FFZ {(isGlobal ? "global " : "")}emotes{(isGlobal ? "." : " for channel " + category)}. {resp.ReasonPhrase}");
                        return false;
                    }

                    JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                    if (!json["sets"].IsObject)
                    {
                        _logger.LogError("sets was not an object");
                        return false;
                    }

                    int count = 0;
                    foreach (JSONObject o in isGlobal ? json["sets"]["3"]["emoticons"].AsArray : json["sets"][json["room"]["set"].ToString()]["emoticons"].AsArray)
                    {
                        JSONObject urls = o["urls"].AsObject;
                        string uri = urls[urls.Count - 1].Value;
                        string identifier = isGlobal ? o["name"].Value : $"{category}_{o["name"].Value}";
                        Resources[identifier] = new ChatResourceData() { Uri = uri, IsAnimated = false, Type = isGlobal ? "FFZGlobalEmote" : "FFZChannelEmote" };
                        count++;
                    }
                    _logger.LogDebug($"Success caching {count} FFZ {(isGlobal ? "global " : "")}emotes{(isGlobal ? "." : " for channel " + category)}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while requesting FFZ {(isGlobal ? "global " : "")}emotes{(isGlobal ? "." : " for channel " + category)}.");
            }
            return false;
        }

        public bool TryGetResource(string identifier, string category, out ChatResourceData data)
        {
            if (!string.IsNullOrEmpty(category) && Resources.TryGetValue($"{category}_{identifier}", out data))
            {
                return true;
            }
            if (Resources.TryGetValue(identifier, out data))
            {
                return true;
            }
            data = null;
            return false;
        }
    }
}
