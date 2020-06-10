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

        private async Task<bool> AddAuthHeader(HttpRequestMessage request)
        {
            bool accessTokenExists = !string.IsNullOrWhiteSpace(_authProvider.Credentials.Mixer_AccessToken);
            bool refreshTokenExists = !string.IsNullOrWhiteSpace(_authProvider.Credentials.Mixer_RefreshToken);
            if ((accessTokenExists && _authProvider.Credentials.Mixer_ExpiresAt <= DateTime.UtcNow.AddMinutes(1)) || (!accessTokenExists && refreshTokenExists))
            {
                _logger.LogInformation($"Refreshing Mixer auth token!");
                await _authProvider.TryRefreshMixerCredentials();
            }
            if (_authProvider.Credentials.Mixer_ExpiresAt > DateTime.UtcNow.AddMinutes(1))
            {
                _logger.LogInformation($"Auth token expires in {(_authProvider.Credentials.Mixer_ExpiresAt - DateTime.UtcNow.AddMinutes(1)).ToString()}, Adding mixer auth header!");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authProvider.Credentials.Mixer_AccessToken);
                return true;
            }
            return false;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            if (await AddAuthHeader(request))
            {
                return await _httpClient.SendAsync(request);
            }
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            resp.Content = new StringContent("Mixer authentication credentials are required to complete the request.");
            return resp;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (await AddAuthHeader(request))
            {
                return await _httpClient.SendAsync(request, cancellationToken);
            }
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            resp.Content = new StringContent("Mixer authentication credentials are required to complete the request.");
            return resp;
        }
    }
}
