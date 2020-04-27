using ChatCore.Interfaces;
using ChatCore.Models.Twitch;
using ChatCore.SimpleJSON;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatCore.Services.Twitch
{
    public class TwitchCheermoteProvider : IChatResourceProvider<TwitchCheermoteData>
    {
        public ConcurrentDictionary<string, TwitchCheermoteData> Resources { get; } = new ConcurrentDictionary<string, TwitchCheermoteData>();
        public TwitchCheermoteProvider(ILogger<TwitchCheermoteProvider> logger, HttpClient httpClient)
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
                _logger.LogDebug($"Requesting Twitch {(isGlobal ? "global " : "")}cheermotes{(isGlobal ? "." : $" for channel {category}")}.");
                using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.twitch.tv/v5/bits/actions?client_id={TwitchDataProvider.TWITCH_CLIENT_ID}&channel_id={category}&include_sponsored=1"))
                {
                    var resp = await _httpClient.SendAsync(msg);
                    if (!resp.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Unsuccessful status code when requesting Twitch {(isGlobal ? "global " : "")}cheermotes{(isGlobal ? "." : " for channel " + category)}. {resp.ReasonPhrase}");
                        return false;
                    }
                    JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                    if (!json["actions"].IsArray)
                    {
                        _logger.LogError("badge_sets was not an object.");
                        return false;
                    }
                    int count = 0;
                    foreach (JSONNode node in json["actions"].AsArray.Values)
                    {
                        TwitchCheermoteData cheermote = new TwitchCheermoteData();
                        string prefix = node["prefix"].Value.ToLower();
                        foreach (JSONNode tier in node["tiers"].Values)
                        {
                            CheermoteTier newTier = new CheermoteTier();
                            newTier.MinBits = tier["min_bits"].AsInt;
                            newTier.Color = tier["color"].Value;
                            newTier.CanCheer = tier["can_cheer"].AsBool;
                            newTier.Uri = $"https://d3aqoihi2n8ty8.cloudfront.net/actions/{prefix}/dark/animated/{newTier.MinBits}/4.gif";
                            //_logger.LogInformation($"Cheermote: {prefix}{newTier.MinBits}, URI: {newTier.Uri}");
                            cheermote.Tiers.Add(newTier);
                        }
                        cheermote.Prefix = prefix;
                        cheermote.Tiers = cheermote.Tiers.OrderBy(t => t.MinBits).ToList();

                        string identifier = isGlobal ? prefix : $"{category}_{prefix}";
                        Resources[identifier] = cheermote;
                        count++;
                    }
                    _logger.LogDebug($"Success caching {count} Twitch {(isGlobal ? "global " : "")}cheermotes{(isGlobal ? "." : " for channel " + category)}.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while requesting Twitch {(isGlobal ? "global " : "")}cheermotes{(isGlobal ? "." : " for channel " + category)}.");
            }
            return false;
        }

        public bool TryGetResource(string identifier, string category, out TwitchCheermoteData data)
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
