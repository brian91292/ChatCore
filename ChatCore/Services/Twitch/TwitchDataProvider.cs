using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using ChatCore.Models.Twitch;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ChatCore.Models;
using System.Threading;

namespace ChatCore.Services.Twitch
{
    public class TwitchDataProvider
    {
        internal const string TWITCH_CLIENT_ID = "jg6ij5z8mf8jr8si22i5uq8tobnmde";

        public TwitchDataProvider(ILogger<TwitchDataProvider> logger, TwitchBadgeProvider twitchBadgeProvider, TwitchCheermoteProvider twitchCheermoteProvider, BTTVDataProvider bttvDataProvider, FFZDataProvider ffzDataProvider)
        {
            _logger = logger;
            _twitchBadgeProvider = twitchBadgeProvider;
            _twitchCheermoteProvider = twitchCheermoteProvider;
            _bttvDataProvider = bttvDataProvider;
            _ffzDataProvider = ffzDataProvider;
        }

        private ILogger _logger;
        private TwitchBadgeProvider _twitchBadgeProvider;
        private TwitchCheermoteProvider _twitchCheermoteProvider;
        private BTTVDataProvider _bttvDataProvider;
        private FFZDataProvider _ffzDataProvider;

        private HashSet<string> _channelDataCached = new HashSet<string>();
        private SemaphoreSlim _globalLock = new SemaphoreSlim(1,1), _channelLock = new SemaphoreSlim(1,1);

        public void TryRequestGlobalResources()
        {
            Task.Run(async () =>
            {
                await _globalLock.WaitAsync();
                try
                {
                    await _twitchBadgeProvider.TryRequestResources(null);
                    await _bttvDataProvider.TryRequestResources(null);
                    await _ffzDataProvider.TryRequestResources(null);
                    //_logger.LogInformation("Finished caching global emotes/badges.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An exception occurred while trying to request global Twitch resources.");
                }
                finally
                {
                    _globalLock.Release();
                }
            });
        }

        public void TryRequestChannelResources(IChatChannel channel)
        {
            Task.Run(async () =>
            {
                await _channelLock.WaitAsync();
                try
                {
                    if (!_channelDataCached.Contains(channel.Id))
                    {
                        string roomId = channel.AsTwitchChannel().Roomstate.RoomId;
                        await _twitchBadgeProvider.TryRequestResources(roomId);
                        await _twitchCheermoteProvider.TryRequestResources(roomId);
                        await _bttvDataProvider.TryRequestResources(channel.Id);
                        await _ffzDataProvider.TryRequestResources(channel.Id);
                        _channelDataCached.Add(channel.Id);
                        //_logger.LogInformation($"Finished caching emotes for channel {channel.Id}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An exception occurred while trying to request Twitch channel resources for {channel.Id}.");
                }
                finally
                {
                    _channelLock.Release();
                }
            });
        }

        public async void TryReleaseChannelResources(IChatChannel channel)
        {
            await _channelLock.WaitAsync();
            try
            {
                // TODO: readd a way to actually clear channel resources
                _channelDataCached.Remove(channel.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An exception occurred while trying to release Twitch channel resources for {channel.Id}.");
            }
            finally
            {
                _channelLock.Release();
            }
        }


        internal bool TryGetThirdPartyEmote(string word, string channel, out ChatResourceData data)
        {
            if (_bttvDataProvider.TryGetResource(word, channel, out data))
            {
                return true;
            }
            else if (_ffzDataProvider.TryGetResource(word, channel, out data))
            {
                return true;
            }
            data = null;
            return false;
        }

        internal bool TryGetCheermote(string word, string roomId, out TwitchCheermoteData data, out int numBits)
        {
            numBits = 0;
            data = null;
            if (string.IsNullOrEmpty(roomId))
            {
                return false;
            }
            if (!char.IsLetter(word[0]) || !char.IsDigit(word[word.Length - 1]))
            {
                return false;
            }
            int prefixLength = -1;
            for (int i = word.Length - 1; i > 0; i--)
            {
                if (!char.IsDigit(word[i]))
                {
                    prefixLength = i + 1;
                    break;
                }
            }
            if (prefixLength == -1)
            {
                return false;
            }
            string prefix = word.Substring(0, prefixLength).ToLower();
            if (!_twitchCheermoteProvider.TryGetResource(prefix, roomId, out data))
            {
                return false;
            }
            numBits = int.TryParse(word.Substring(prefixLength), out var intVal) ? intVal : 0;
            return true;
        }

        internal bool TryGetBadgeInfo(string badgeId, string roomid, out ChatResourceData badge)
        {
            if(_twitchBadgeProvider.TryGetResource(badgeId, roomid, out badge))
            {
                return true;
            }
            badge = null;
            return false;
        }
    }
}
