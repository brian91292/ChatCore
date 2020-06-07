using ChatCore.SimpleJSON;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ChatCore.Services.Mixer
{
    public class OAuthCredentials
    {
        public string AccessToken;
        public string RefreshToken;
        public DateTime ExpiresAt;
    }


    public interface IOAuthShortcodeData
    {
        string Code { get; set; }
        string DeviceId { get; set; }
        int PollFrequencyMs { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class MixerOAuthShortcodeData : IOAuthShortcodeData
    {
        public string Code { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public int PollFrequencyMs { get; set; } = 5000;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow;
    }

    public interface IShortcodeAuthProvider
    {
        Task<IOAuthShortcodeData> RequestShortcode();
        Task<OAuthCredentials> WaitForGrant(IOAuthShortcodeData request = null);
    }

    public class MixerShortcodeAuthProvider : IShortcodeAuthProvider
    {
        private string _scopes = "interactive:robot:self";

        public MixerShortcodeAuthProvider(ILogger<MixerShortcodeAuthProvider> logger, HttpClient httpClient) 
        {
            _logger = logger;
            _httpClient = httpClient;
        }
        protected ILogger _logger;
        protected HttpClient _httpClient;

        public async Task<IOAuthShortcodeData> RequestShortcode()
        {
            _logger.LogInformation("Requesting grant!");
            try
            {
                var msg = new HttpRequestMessage(HttpMethod.Post, "https://mixer.com/api/v1/oauth/shortcode");
                msg.Headers.Host = "mixer.com";
                msg.Content = new StringContent($"{{\"client_id\": \"{MixerDataProvider.MIXER_CLIENT_ID}\", \"scope\": \"{_scopes}\"}}");
                var resp = await _httpClient.SendAsync(msg);

                if (resp.IsSuccessStatusCode)
                {
                    var json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                    if (json == null)
                    {
                        _logger.LogWarning("Json is null!");
                        return null;
                    }
                    _logger.LogInformation("Returning");
                    return new MixerOAuthShortcodeData()
                    {
                        Code = json.TryGetKey("code", out var c) ? c.Value : "",
                        DeviceId = json.TryGetKey("handle", out var h) ? h.Value : "",
                        ExpiresAt = json.TryGetKey("expires_in", out var e) ? DateTime.UtcNow.AddMinutes(e.AsInt) : DateTime.UtcNow
                    };
                }
                else
                {
                    _logger.LogError($"An error occurred while requesting shortcode! StatusCode: {resp.StatusCode}, Error: {await resp.Content.ReadAsStringAsync()}");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "An error occurred while requesting grant in MixerShortcodeAuthProvider");
            }
            _logger.LogWarning("Resp is null!");
            return null;
        }

        private async Task<OAuthCredentials> ExchangeCodeForCredentials(string code)
        {
            var resp = await _httpClient.PostAsync("https://mixer.com/api/v1/oauth/token", new StringContent($"{{\"client_id\": \"{MixerDataProvider.MIXER_CLIENT_ID}\", \"code\": \"{code}\", \"grant_type\": \"authorization_code\"}}"));
            if(resp.IsSuccessStatusCode)
            {
                var json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                if (json == null)
                {
                    return null;
                }
                return new OAuthCredentials()
                {
                    RefreshToken = json.TryGetKey("refresh_token", out var r) ? r.Value : "",
                    AccessToken = json.TryGetKey("access_token", out var a) ? a.Value : "",
                    ExpiresAt = json.TryGetKey("expires_in", out var e) ? DateTime.UtcNow.AddMinutes(e.AsInt) : DateTime.UtcNow
                };
            }
            return null;
        }

        public async Task<OAuthCredentials> WaitForGrant(IOAuthShortcodeData request = null)
        {
            if (request == null) {
                request = await RequestShortcode();
                _logger.LogInformation($"Got grant! Code: {request.Code}, DeviceId: {request.DeviceId}");
            }
            if (request == null) {
                _logger.LogWarning("Shortcode request is null! This should never happen!");
                return null;
            }
            _logger.LogInformation("Launching process!");
            Process.Start($"https://mixer.com/go?code={request.Code}");

            while (DateTime.UtcNow < request.ExpiresAt)
            {
                _logger.LogInformation($"Waiting for grant!");
                var resp = await _httpClient.GetAsync($"https://mixer.com/api/v1/oauth/shortcode/check/{request.DeviceId}");
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var json = JSON.Parse(await resp.Content.ReadAsStringAsync());
                    if (json == null)
                    {
                        return null;
                    }
                    if (json.TryGetKey("code", out var code))
                    {
                        return await ExchangeCodeForCredentials(code);
                    }
                    break;
                }
                await Task.Delay(request.PollFrequencyMs);
            }
            throw new Exception("An unknown exception occurred while waiting for OAuth grant");
        }

    }
}
