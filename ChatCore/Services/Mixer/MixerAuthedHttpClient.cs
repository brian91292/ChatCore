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
                //_logger.LogInformation($"Refreshing Mixer auth token!");
                await _authProvider.TryRefreshMixerCredentials();
            }
            if (_authProvider.Credentials.Mixer_ExpiresAt > DateTime.UtcNow.AddMinutes(1))
            {
                //_logger.LogInformation($"Auth token expires in {(_authProvider.Credentials.Mixer_ExpiresAt - DateTime.UtcNow.AddMinutes(1)).ToString()}, Adding mixer auth header!");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authProvider.Credentials.Mixer_AccessToken);
                return true;
            }
            return false;
        }

        private HttpResponseMessage UnauthorizedResponse()
        {
            var resp = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            resp.Content = new StringContent("Mixer authentication credentials are required to complete the request.");
            return resp;
        }

        private HttpRequestMessage CloneRequest(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);
            clone.Content = req.Content;
            clone.Version = req.Version;
            foreach (KeyValuePair<string, object> prop in req.Properties)
            {
                clone.Properties.Add(prop);
            }
            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            return clone;
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            bool retry = true;
         retry:
            if (await AddAuthHeader(request))
            {
                var resp = await _httpClient.SendAsync(request);
                if(retry && resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    retry = false;
                    if (await _authProvider.TryRefreshMixerCredentials())
                    {
                        request = CloneRequest(request);
                        goto retry;
                    }
                }
                return resp;
            }
            return UnauthorizedResponse();
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool retry = true;
         retry:
            if (await AddAuthHeader(request))
            {
                var resp = await _httpClient.SendAsync(request, cancellationToken);
                if (retry && resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    retry = false;
                    if (await _authProvider.TryRefreshMixerCredentials())
                    {
                        request = CloneRequest(request);
                        goto retry;
                    }
                }
                return resp;
            }
            return UnauthorizedResponse();
        }
    }
}
