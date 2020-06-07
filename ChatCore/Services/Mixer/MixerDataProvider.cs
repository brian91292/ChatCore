using ChatCore.Models.Mixer;
using ChatCore.SimpleJSON;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatCore.Services.Mixer
{
    public class MixerDataProvider
    {
        internal const string MIXER_CLIENT_ID = "370e40b0cf5f3e2036a812ba1650d1be27d5b3f05bf98d7d";

        public MixerDataProvider(ILogger<MixerDataProvider> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        private ILogger _logger;
        private HttpClient _httpClient;

        /// <summary>
        /// Grabs a channel id based on a username, or null if the channel does not exist.
        /// </summary>
        /// <param name="username">The username to retrieve the channel id for.</param>
        /// <returns>The channel id, or null if it does not exist.</returns>
        public async Task<string> GetChannelIdFromUsername(string username)
        {
            var resp = await _httpClient.GetAsync($"https://mixer.com/api/v1/channels/{username}?fields=id");
            if(resp.IsSuccessStatusCode)
            {
                var json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                if (json != null)
                {
                    return json["id"].Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Grabs details about a specific Mixer channel by id, returns null if the channel id doesn't exist
        /// </summary>
        /// <param name="channelId">The channel id to retrieve details for.</param>
        /// <returns>The channel details, or null if it does not exist.</returns>
        public async Task<MixerChannelDetails> GetChannelDetails(string channelId)
        {
            if(string.IsNullOrEmpty(channelId))
            {
                return null;
            }
            var resp = await _httpClient.GetAsync($"https://mixer.com/api/v1/chats/{channelId}");
            if (resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync();
                _logger.LogInformation($"Raw: {raw}");
                return new MixerChannelDetails(raw);
            }
            return null;
        }
    }
}
