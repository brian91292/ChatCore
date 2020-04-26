using ChatCore.Interfaces;
using ChatCore.Models;
using ChatCore.SimpleJSON;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChatCore.Services.Twitch
{
    public class BTTVDataProvider : IChatResourceProvider<ChatResourceData>
    {
        public ConcurrentDictionary<string, ChatResourceData> Resources { get; } = new ConcurrentDictionary<string, ChatResourceData>();
        public BTTVDataProvider(ILogger<BTTVDataProvider> logger, HttpClient httpClient)
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
                _logger.LogDebug($"Requesting BTTV {(isGlobal ? "global " : "")}emotes{(isGlobal ? "." : $" for channel {category}")}.");
                using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, isGlobal ? "https://api.betterttv.net/2/emotes" : $"https://api.betterttv.net/2/channels/{category}"))
                {
                    var resp = await _httpClient.SendAsync(msg);
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Unsuccessful status code when requesting BTTV {(isGlobal ? "global " : "")}emotes{(isGlobal ? "." : " for channel " + category)}. {resp.ReasonPhrase}");
                        return false;
                    }

                    JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                    if (!json["emotes"].IsArray)
                    {
                        _logger.LogError("emotes was not an array.");
                        return false;
                    }

                    int count = 0;
                    foreach (JSONObject o in json["emotes"].AsArray)
                    {
                        string uri = $"https://cdn.betterttv.net/emote/{o["id"].Value}/3x";
                        string identifier = isGlobal ? o["code"].Value : $"{category}_{o["code"].Value}";
                        Resources.TryAdd(identifier, new ChatResourceData() { Uri = uri, IsAnimated = o["imageType"].Value == "gif", Type = isGlobal ? "BTTVGlobalEmote" : "BTTVChannelEmote" });
                        count++;
                    }
                    _logger.LogDebug($"Success caching {count} BTTV {(isGlobal ? "global " : "")}emotes{(isGlobal ? "." : " for channel " + category)}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while requesting BTTV {(isGlobal ? "global " : "")}emotes{(isGlobal ? "." : " for channel " + category)}.");
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
