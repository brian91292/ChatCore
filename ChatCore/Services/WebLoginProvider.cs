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

                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Web server is listening for requests...");
                        HttpListenerContext ctx = await _listener.GetContextAsync();
                        HttpListenerRequest req = ctx.Request;
                        HttpListenerResponse resp = ctx.Response;

                        if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/submit")
                        {
                            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                            {
                                string postStr = reader.ReadToEnd();
                                List<string> twitchChannels = new List<string>();

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

                                                break;
                                            case "twitch_channel":
                                                if (!string.IsNullOrWhiteSpace(split[1]))
                                                {
                                                    _logger.LogInformation($"Channel: {split[1]}");
                                                    twitchChannels.Add(split[1]);
                                                }
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "An exception occurred in OnLoginDataUpdated callback");
                                    }
                                }

                                _authManager.Credentials.Twitch_Channels_Array = twitchChannels.ToArray();
                                _authManager.Save();
                                _settings.SetFromDictionary(postDict);
                                _settings.Save();
                            }
                            resp.Redirect(req.UrlReferrer.OriginalString);
                            resp.Close();
                            continue;
                        }
                        try
                        {
                            StringBuilder pageBuilder = new StringBuilder(pageData);
                            StringBuilder channelHtmlString = new StringBuilder();
                            for (int i = 0; i < _authManager.Credentials.Twitch_Channels_Array.Length; i++)
                            {
                                //<span id="twitch_channel_1" class="chip">
                                //  ChannelName
                                //  <input type="text" class="form-input" name="twitch_channel" style="display:none;" value="yeeter" />
                                //  <button type="button" onclick="removeChannel('twitch_channel_1')" class="btn btn-clear" aria-label="Close" role="button"></button>
                                //</span>
                                var channel = _authManager.Credentials.Twitch_Channels_Array[i];
                                channelHtmlString.Append($"<span id=\"twitch_channel_{i}\" class=\"chip \">{channel}<input type=\"text\" class=\"form-input\" name=\"twitch_channel\" style=\"display: none; \" value=\"{channel}\" /><button type=\"button\" onclick=\"removeChannel('twitch_channel_{i}')\" class=\"btn btn-clear\" aria-label=\"Close\" role=\"button\"></button></span>");
                            }
                            var sectionHTML = _settings.GetSettingsAsHTML();
                            pageBuilder.Replace("{WebAppSettingsHTML}", sectionHTML["WebApp"]);
                            pageBuilder.Replace("{GlobalSettingsHTML}", sectionHTML["Global"]);
                            pageBuilder.Replace("{TwitchSettingsHTML}", sectionHTML["Twitch"]);
                            pageBuilder.Replace("{TwitchChannelHtml}", channelHtmlString.ToString());
                            pageBuilder.Replace("{TwitchOAuthToken}", _authManager.Credentials.Twitch_OAuthToken);

                            byte[] data = Encoding.UTF8.GetBytes(pageBuilder.ToString());
                            resp.ContentType = "text/html";
                            resp.ContentEncoding = Encoding.UTF8;
                            resp.ContentLength64 = data.LongLength;
                            await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception while trying to prepare html for web login provider.");
                        }
                    }
                });
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
