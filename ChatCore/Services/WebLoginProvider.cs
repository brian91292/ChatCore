using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using ChatCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ChatCore.Services
{
    public class WebLoginProvider : IWebLoginProvider
    {
        public WebLoginProvider(ILogger<WebLoginProvider> logger, IUserAuthProvider authManager, MainSettingsProvider settings)
        {
            _logger = logger;
            _authManager = authManager;
            _settings = settings;
        }

        private ILogger _logger;
        private IUserAuthProvider _authManager;
        private MainSettingsProvider _settings;
        private HttpListener _listener;
        private CancellationTokenSource _cancellationToken;
        private static string pageData;
        private SemaphoreSlim _requestLock = new SemaphoreSlim(1, 1);

        public void Start()
        {
            if (pageData == null)
            {
                using (StreamReader reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ChatCore.Resources.Web.index.html")))
                {
                    pageData = reader.ReadToEnd();
                    //_logger.LogInformation($"PageData: {pageData}");
                }
            }
            if (_listener == null)
            {
                _cancellationToken = new CancellationTokenSource();
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_settings.WebAppPort}/");
                Task.Run(async () =>
                {
                    _listener.Start();
                    _listener.BeginGetContext(OnContext, null);
                });
            }
        }

        private async void OnContext(IAsyncResult res)
        {
            HttpListenerContext ctx = _listener.EndGetContext(res);
            _listener.BeginGetContext(OnContext, null);

            await _requestLock.WaitAsync();
            try
            {
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/submit")
                {
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                    {
                        string postStr = reader.ReadToEnd();
                        List<string> twitchChannels = new List<string>(), mixerChannels = new List<string>();

                        Dictionary<string, string> postDict = new Dictionary<string, string>();
                        foreach (var postData in postStr.Split('&'))
                        {
                            try
                            {
                                var split = postData.Split('=');
                                postDict[split[0]] = split[1];

                                switch (split[0])
                                {
                                    case "twitch_oauthtoken":
                                        var twitchOauthToken = HttpUtility.UrlDecode(split[1]);
                                        _authManager.Credentials.Twitch_OAuthToken = twitchOauthToken.StartsWith("oauth:") ? twitchOauthToken : !string.IsNullOrEmpty(twitchOauthToken) ? $"oauth:{twitchOauthToken}" : "";
                                        break;
                                    case "twitch_channel":
                                        string twitchChannel = split[1].ToLower();
                                        if (!string.IsNullOrWhiteSpace(twitchChannel) && !_authManager.Credentials.Twitch_Channels.Contains(twitchChannel))
                                        {
                                            _authManager.Credentials.Twitch_Channels.Add(twitchChannel);
                                        }
                                        _logger.LogInformation($"TwitchChannel: {twitchChannel}");
                                        twitchChannels.Add(twitchChannel);
                                        break;
                                    case "mixer_channel":
                                        string mixerChannel = split[1].ToLower();
                                        if (!string.IsNullOrWhiteSpace(mixerChannel) && !_authManager.Credentials.Mixer_Channels.Contains(mixerChannel))
                                        {
                                            _authManager.Credentials.Mixer_Channels.Add(mixerChannel);
                                        }
                                        _logger.LogInformation($"MixerChannel: {mixerChannel}");
                                        mixerChannels.Add(mixerChannel);
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "An exception occurred in OnLoginDataUpdated callback");
                            }
                        }
                        foreach (var channel in _authManager.Credentials.Twitch_Channels.ToArray())
                        {
                            // Remove any channels that weren't present in the post data
                            if (!twitchChannels.Contains(channel))
                            {
                                _authManager.Credentials.Twitch_Channels.Remove(channel);
                            }
                        }
                        foreach (var channel in _authManager.Credentials.Mixer_Channels.ToArray())
                        {
                            // Remove any channels that weren't present in the post data
                            if (!mixerChannels.Contains(channel))
                            {
                                _authManager.Credentials.Mixer_Channels.Remove(channel);
                            }
                        }
                        _authManager.Save();
                        _settings.SetFromDictionary(postDict);
                        _settings.Save();
                    }
                    resp.Redirect(req.UrlReferrer.OriginalString);
                    resp.Close();
                    return;
                }
                StringBuilder pageBuilder = new StringBuilder(pageData);
                StringBuilder twitchChannelHtmlString = new StringBuilder();
                for (int i = 0; i < _authManager.Credentials.Twitch_Channels.Count; i++)
                {
                    var channel = _authManager.Credentials.Twitch_Channels[i];
                    twitchChannelHtmlString.Append($"<span id=\"twitch_channel_{i}\" class=\"chip \">{channel}<input type=\"text\" class=\"form-input\" name=\"twitch_channel\" style=\"display: none; \" value=\"{channel}\" /><button type=\"button\" onclick=\"removeTwitchChannel('twitch_channel_{i}')\" class=\"btn btn-clear\" aria-label=\"Close\" role=\"button\"></button></span>");
                }
                StringBuilder mixerChannelHtmlString = new StringBuilder();
                for (int i = 0; i < _authManager.Credentials.Mixer_Channels.Count; i++)
                {
                    var channel = _authManager.Credentials.Mixer_Channels[i];
                    mixerChannelHtmlString.Append($"<span id=\"mixer_channel_{i}\" class=\"chip \">{channel}<input type=\"text\" class=\"form-input\" name=\"mixer_channel\" style=\"display: none; \" value=\"{channel}\" /><button type=\"button\" onclick=\"removeMixerChannel('mixer_channel_{i}')\" class=\"btn btn-clear\" aria-label=\"Close\" role=\"button\"></button></span>");
                }
                StringBuilder mixerLinkHtmlString = new StringBuilder();
                if(!string.IsNullOrEmpty(_authManager.Credentials.Mixer_RefreshToken))
                {
                    mixerLinkHtmlString.Append("<button class=\"btn btn-error\" onclick=\"window.location.href='mixer'\" type=\"button\">Click to unlink your Mixer account</button></br></br>");
                }
                else
                {
                    mixerLinkHtmlString.Append("<button class=\"btn btn-success\" onclick=\"window.location.href='mixer'\" type=\"button\">Click to link your Mixer account</button></br></br>");
                }
                var sectionHTML = _settings.GetSettingsAsHTML();
                pageBuilder.Replace("{WebAppSettingsHTML}", sectionHTML["WebApp"]);
                pageBuilder.Replace("{GlobalSettingsHTML}", sectionHTML["Global"]);
                pageBuilder.Replace("{TwitchSettingsHTML}", sectionHTML["Twitch"]);
                pageBuilder.Replace("{TwitchChannelHtml}", twitchChannelHtmlString.ToString());
                pageBuilder.Replace("{TwitchOAuthToken}", _authManager.Credentials.Twitch_OAuthToken);
                pageBuilder.Replace("{MixerSettingsHTML}", sectionHTML["Mixer"]);
                pageBuilder.Replace("{MixerChannelHtml}", mixerChannelHtmlString.ToString());
                pageBuilder.Replace("{MixerLinkHtml}", mixerLinkHtmlString.ToString());
                byte[] data = Encoding.UTF8.GetBytes(pageBuilder.ToString());
                resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during webapp request.");
            }
            finally
            {
                _requestLock.Release();
            }
        }
        public void Stop()
        {
            if (!(_cancellationToken is null))
            {
                _cancellationToken.Cancel();
                _logger.LogInformation("Stopped");
            }
        }
    }
}
