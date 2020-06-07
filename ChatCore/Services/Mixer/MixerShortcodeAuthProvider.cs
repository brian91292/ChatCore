using ChatCore.Interfaces;
using ChatCore.Models.OAuth;
using ChatCore.SimpleJSON;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatCore.Services.Mixer
{
    public class MixerShortcodeAuthProvider : IShortcodeAuthProvider
    {
        private string _scopes = "channel:update:self chat:connect chat:chat chat:whisper chat:bypass_links chat:bypass_slowchat chat:change_ban chat:timeout";

        public MixerShortcodeAuthProvider(ILogger<MixerShortcodeAuthProvider> logger, IDefaultBrowserLauncherService browserLauncher, HttpClient httpClient) 
        {
            _logger = logger;
            _httpClient = httpClient;
            _browserLauncher = browserLauncher;
        }
        private ILogger _logger;
        private HttpClient _httpClient;
        private IDefaultBrowserLauncherService _browserLauncher;

        public Task<OAuthCredentials> TryRefreshCredentials(string refreshToken)
        {
            return ExchangeCodeForCredentials(refreshToken, true);
        }

        public async Task<OAuthShortcodeRequest> RequestShortcode()
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
                    return new OAuthShortcodeRequest()
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

        private async Task<OAuthCredentials> ExchangeCodeForCredentials(string code, bool refresh = false)
        {
            var resp = await _httpClient.PostAsync("https://mixer.com/api/v1/oauth/token", new StringContent($"{{\"client_id\": \"{MixerDataProvider.MIXER_CLIENT_ID}\", \"{(refresh ? "refresh_token" : "code")}\": \"{code}\", \"grant_type\": \"{(refresh ? "refresh_token" : "authorization_code")}\"}}"));
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

        public async Task<OAuthCredentials> AwaitUserApproval(CancellationToken cancellationToken, OAuthShortcodeRequest request = null, bool launchBrowserProcess = false)
        {
            try
            {
                if (request == null)
                {
                    request = await RequestShortcode();
                    _logger.LogInformation($"Got grant! Code: {request.Code}, DeviceId: {request.DeviceId}");
                }
                if (request == null)
                {
                    _logger.LogWarning("Shortcode request is null! This should never happen!");
                    return null;
                }
                if (launchBrowserProcess)
                {
                    _logger.LogInformation("Launching process!");
                    _browserLauncher.Launch($"https://mixer.com/go?code={request.Code}");
                }

                while (DateTime.UtcNow < request.ExpiresAt && !cancellationToken.IsCancellationRequested)
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
            }
            catch(TaskCanceledException)
            {
                return null;
            }
            throw new Exception("An unknown exception occurred while waiting for OAuth grant");
        }

    }
}
