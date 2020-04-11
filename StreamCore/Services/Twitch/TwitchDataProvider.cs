using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models.Twitch;
using StreamCore.SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StreamCore.Services.Twitch
{
    internal class TwitchChannelResource
    {
        public Dictionary<string, TwitchImageData> TwitchBadges;
        public Dictionary<string, TwitchImageData> BTTVEmotes;
        public Dictionary<string, TwitchImageData> FFZEmotes;
    }

    public class TwitchDataProvider
    {
        public TwitchDataProvider(ILogger<TwitchDataProvider> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        private ILogger _logger;
        private HttpClient _httpClient;

        internal Dictionary<string, TwitchChannelResource> TwitchChannelResources = new Dictionary<string, TwitchChannelResource>();
        internal Dictionary<string, TwitchImageData> TwitchGlobalBadges;
        internal Dictionary<string, TwitchImageData> BTTVGlobalEmotes;
        internal Dictionary<string, TwitchImageData> FFZGlobalEmotes;

        private object _lock = new object();

        public void TryRequestGlobalResources()
        {
            lock (_lock)
            {
                if (TwitchGlobalBadges is null && BTTVGlobalEmotes is null && FFZGlobalEmotes is null)
                {
                    TwitchGlobalBadges = new Dictionary<string, TwitchImageData>();
                    BTTVGlobalEmotes = new Dictionary<string, TwitchImageData>();
                    FFZGlobalEmotes = new Dictionary<string, TwitchImageData>();
                    Task.Run(async () =>
                    {
                        try
                        {
                            using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, $"https://badges.twitch.tv/v1/badges/global/display"))
                            {
                                var resp = await _httpClient.SendAsync(msg);
                                if (!resp.IsSuccessStatusCode)
                                {
                                    _logger.LogError($"Failed to receive global badge data. Status: {resp.StatusCode}, Error: {resp.Content.ReadAsStringAsync()}");
                                    goto BTTV;
                                }
                                JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                                if (!json["badge_sets"].IsObject)
                                {
                                    _logger.LogError("badge_sets was not an object.");
                                    goto BTTV;
                                }

                                foreach (KeyValuePair<string, JSONNode> kvp in json["badge_sets"])
                                {
                                    string badgeName = kvp.Key;
                                    foreach (KeyValuePair<string, JSONNode> version in kvp.Value.AsObject["versions"].AsObject)
                                    {
                                        string badgeVersion = version.Key;
                                        string finalName = $"{badgeName}{badgeVersion}";
                                        string uri = version.Value.AsObject["image_url_4x"].Value;
                                        //_logger.LogInformation($"Global Badge: {finalName}, URI: {uri}");
                                        TwitchGlobalBadges.Add(finalName, new TwitchImageData() { Uri = uri, IsAnimated = false, Type = "TwitchGlobalBadge" });
                                    }
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred while parsing Twitch global badges");
                        }

                    BTTV:
                        try
                        {
                            using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.betterttv.net/2/emotes"))
                            {
                                var resp = await _httpClient.SendAsync(msg);
                                if (!resp.IsSuccessStatusCode)
                                {
                                    _logger.LogError($"Failed to receive global BTTV emotes.");
                                    goto FFZ;
                                }

                                JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                                if (!json["emotes"].IsArray)
                                {
                                    _logger.LogError("emotes was not an array.");
                                    goto FFZ;
                                }

                                foreach (JSONObject o in json["emotes"].AsArray)
                                {
                                    string uri = $"https://cdn.betterttv.net/emote/{o["id"].Value}/3x";
                                    //_logger.LogInformation($"BTTV Global Emote: {o["code"].Value}, URI: {uri}");
                                    BTTVGlobalEmotes.Add(o["code"].Value, new TwitchImageData() { Uri = uri, IsAnimated = o["imageType"].Value == "gif", Type = "BTTVGlobalEmote" });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred while parsing BTTV global emotes");
                        }

                    FFZ:
                        try
                        {
                            using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.frankerfacez.com/v1/set/global"))
                            {
                                var resp = await _httpClient.SendAsync(msg);
                                if (!resp.IsSuccessStatusCode)
                                {
                                    _logger.LogError($"Failed to receive global FFZ emotes.");
                                    goto EXIT;
                                }

                                JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                                if (!json["sets"].IsObject)
                                {
                                    _logger.LogError("sets was not an object");
                                    goto EXIT;
                                }

                                foreach(JSONObject o in json["sets"]["3"]["emoticons"].AsArray)
                                {
                                    JSONObject urls = o["urls"].AsObject;
                                    string uri = urls[urls.Count - 1].Value;
                                    //_logger.LogInformation($"FFZ Global Emote: {o["name"].Value}, URI: {uri} (all urls: {urls.Value})");
                                    FFZGlobalEmotes.Add(o["name"].Value, new TwitchImageData() { Uri = uri, IsAnimated = false, Type = "FFZGlobalEmote" });
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred while parsing FFZ global emotes");
                        }
                    EXIT:
                        _logger.LogInformation("Finished caching global emotes.");
                    });
                }
            }
        }

        public void TryRequestChannelResources(IChatChannel channel)
        {
            lock (_lock)
            {
                if (channel != null && !TwitchChannelResources.ContainsKey(channel.Id))
                {
                    _logger.LogInformation($"Requesting channel badges for {channel.Id}");

                    var newChannelBadges = new Dictionary<string, TwitchImageData>();
                    var newBTTVEmotes = new Dictionary<string, TwitchImageData>();
                    var newFFZEmotes = new Dictionary<string, TwitchImageData>();
                    var newChannelResources = new TwitchChannelResource()
                    {
                        TwitchBadges = newChannelBadges,
                        BTTVEmotes = newBTTVEmotes,
                        FFZEmotes = newFFZEmotes
                    };
                    TwitchChannelResources.Add(channel.Id, newChannelResources);
                    Task.Run(async () =>
                    {
                        try
                        {
                            using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, $"https://badges.twitch.tv/v1/badges/channels/{channel.AsTwitchChannel().Roomstate.RoomId}/display"))
                            {
                                var resp = await _httpClient.SendAsync(msg);
                                if (!resp.IsSuccessStatusCode)
                                {
                                    // TODO: figure out why this is failing sometimes
                                    _logger.LogError($"Failed to receive channel badge data. Status: {resp.StatusCode}, RoomId: {channel.AsTwitchChannel().Roomstate.RoomId}, Error: {resp.Content.ReadAsStringAsync()}");
                                    goto BTTV;
                                }
                                JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                                if (!json["badge_sets"].IsObject)
                                {
                                    _logger.LogError("badge_sets was not an object.");
                                    goto BTTV;
                                }
                                foreach (KeyValuePair<string, JSONNode> kvp in json["badge_sets"])
                                {
                                    string badgeName = kvp.Key;
                                    foreach (KeyValuePair<string, JSONNode> version in kvp.Value.AsObject["versions"].AsObject)
                                    {
                                        string badgeVersion = version.Key;
                                        string finalName = $"{badgeName}{badgeVersion}";
                                        string uri = version.Value.AsObject["image_url_4x"].Value;
                                        //_logger.LogInformation($"Channel Badge: {finalName}, URI: {uri}");
                                        newChannelBadges.Add(finalName, new TwitchImageData { Uri = uri, IsAnimated = false, Type = "TwitchChannelBadge" });
                                    }
                                }
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred while parsing Twitch channel badges for {channel.Id}");
                        }

                    BTTV:
                        try
                        {
                            using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.betterttv.net/2/channels/{channel.Id}"))
                            {
                                var resp = await _httpClient.SendAsync(msg);
                                if (!resp.IsSuccessStatusCode)
                                {
                                    _logger.LogError($"Failed to receive BTTV emotes for channel {channel.Id}.");
                                    goto FFZ;
                                }

                                JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                                if (!json["emotes"].IsArray)
                                {
                                    _logger.LogError($"emotes was not an array.");
                                    goto FFZ;
                                }

                                foreach (JSONObject o in json["emotes"].AsArray)
                                {
                                    string uri = $"https://cdn.betterttv.net/emote/{o["id"].Value}/3x";
                                    //_logger.LogInformation($"BTTV Channel Emote: {o["code"].Value}, URI: {uri}");
                                    newBTTVEmotes.Add(o["code"].Value, new TwitchImageData() { Uri = uri, IsAnimated = o["imageType"].Value == "gif", Type = "BTTVChannelEmote" });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred while parsing BTTV emotes for channel {channel.Id}");
                        }

                    FFZ:
                        try
                        {
                            using (HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, $"https://api.frankerfacez.com/v1/room/{channel.Id}"))
                            {
                                var resp = await _httpClient.SendAsync(msg);
                                if (!resp.IsSuccessStatusCode)
                                {
                                    _logger.LogError($"Failed to receive FFZ emotes for channel {channel.Id}.");
                                    goto EXIT;
                                }

                                JSONNode json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                                if(!json["sets"].IsObject)
                                {
                                    _logger.LogError("sets was not an object");
                                    goto EXIT;
                                }

                                foreach (JSONObject o in json["sets"][json["room"]["set"].ToString()]["emoticons"].AsArray)
                                {
                                    JSONObject urls = o["urls"].AsObject;
                                    string uri = urls[urls.Count - 1].Value;
                                    //_logger.LogInformation($"FFZ Channel Emote: {o["name"].Value}, URI: {uri} (all urls: {urls.Value})");
                                    newFFZEmotes.Add(o["name"].Value, new TwitchImageData() { Uri = uri, IsAnimated = false, Type = "FFZChannelEmote" });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An exception occurred while parsing FFZ emotes for channel {channel.Id}");
                        }
                    EXIT:
                        _logger.LogInformation($"Finished caching emotes for channel {channel.Id}.");
                    });
                }
            }
        }

        internal bool TryGetThirdPartyEmote(string word, string channel, out TwitchImageData data)
        {
            bool isEmote = false;
            if (BTTVGlobalEmotes != null && BTTVGlobalEmotes.TryGetValue(word, out data))
            {
                //_logger.LogInformation($"BTTV Global Emote: {lastWord}");
                isEmote = true;
            }
            else if (FFZGlobalEmotes != null && FFZGlobalEmotes.TryGetValue(word, out data))
            {
                //_logger.LogInformation($"FFZ Global Emote: {lastWord}");
                isEmote = true;
            }
            else if (TwitchChannelResources.TryGetValue(channel, out var channelResources))
            {
                if (channelResources.BTTVEmotes.TryGetValue(word, out data))
                {
                    //_logger.LogInformation($"BTTV Channel Emote: {lastWord}");
                    isEmote = true;
                }
                else if (channelResources.FFZEmotes.TryGetValue(word, out data))
                {
                    //_logger.LogInformation($"FFZ Channel Emote: {lastWord}");
                    isEmote = true;
                }
                else
                {
                    data = new TwitchImageData();
                }
            }
            else
            {
                data = new TwitchImageData();
            }
            return isEmote;
        }

        public void TryReleaseChannelResources(IChatChannel channel)
        {
            lock (_lock)
            {
                if (channel != null && TwitchChannelResources.TryGetValue(channel.Id, out var channelResources))
                {
                    channelResources.BTTVEmotes.Clear();
                    channelResources.FFZEmotes.Clear();
                    channelResources.TwitchBadges.Clear();
                    TwitchChannelResources.Remove(channel.Id);
                }
            }
        }

        internal bool TryGetBadgeInfo(string badgeId, string channel, out TwitchImageData badge)
        {
            badge = null;
            if(!string.IsNullOrEmpty(channel))
            {
                if(TwitchChannelResources.TryGetValue(channel, out var channelResources) && channelResources.TwitchBadges.TryGetValue(badgeId, out var channelBadge))
                {
                    badge = channelBadge;
                }
            }
            if(badge == null && TwitchGlobalBadges != null && TwitchGlobalBadges.TryGetValue(badgeId, out var globalBadge))
            {
                badge = globalBadge;
            }
            return badge != null;
        }

        //public async Task<bool> RequestDataForChannel(string channel, int roomId)
        //{

        //}
    }
}
