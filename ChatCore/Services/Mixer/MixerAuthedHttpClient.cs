using ChatCore.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatCore.Services.Mixer
{
    public class MixerAuthedHttpClient
    {
        public bool IsLoggedIn { get; }
        public MixerAuthedHttpClient(ILogger<MixerAuthedHttpClient> logger, IUserAuthProvider authProvider, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
            _authProvider = authProvider;
        }
        private ILogger _logger;
        private HttpClient _httpClient;
        private IUserAuthProvider _authProvider;

        private async Task AddAuthHeader(HttpRequestMessage request)
        {
            if (_authProvider.Credentials.Mixer_ExpiresAt <= DateTime.UtcNow.AddMinutes(1))
            {
                _logger.LogInformation($"Refreshing Mixer auth token!");
                await _authProvider.TryRefreshMixerCredentials();
            }
            if (_authProvider.Credentials.Mixer_ExpiresAt > DateTime.UtcNow.AddMinutes(1))
            {
                _logger.LogInformation($"Adding mixer auth header!");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authProvider.Credentials.Mixer_AccessToken);
            }
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            await AddAuthHeader(request);
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await AddAuthHeader(request);
            return await _httpClient.SendAsync(request, cancellationToken);
        }
    }
}
