using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace StreamCore.Services
{
    public class WebLoginProvider : IWebLoginProvider
    {
        public WebLoginProvider(ILogger<WebLoginProvider> logger, IUserAuthManager authManager)
        {
            _logger = logger;
            _authManager = authManager;
        }

        private ILogger _logger;
        private IUserAuthManager _authManager;
        private HttpListener _listener;
        private CancellationTokenSource _cancellationToken;

        public static string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>StreamCore v3 Login</title>" +
            "    <link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/spectre.css/0.5.8/spectre.min.css\">" +
            "  </head>" +
            "  <body>" +
            "    <main class=\"container flex-centered\">" +
            "      <form method=\"post\" action=\"update\">" +
            "        <br/>" +
            "        <h3 class=\"text-center\">Twitch Login</h3>" +
            "        <hr/>" +
            "        <div class=\"form-group\">" +
            "          <label class=\"form-label text-center\" for=\"say\">OAuth Token</label>" +
            "          <input class=\"form-input\" name=\"twitch_oauthtoken\" type=\"text\" id=\"say\" placeholder=\"oauth:12abc3defg4p678arw9aq2xasd0gwa43\" value=\"{0}\">" +
            "        </div>" +
            "        <div class=\"flex-centered\">" +
            "          <button class=\"btn\">Save</button>" +
            "        </div>" +
            "        <br/>" +
            "        <a href=\"https://twitchapps.com/tmi/\" target=\"_blank\" class=\"text-center\">Don't know where to find your oauth token? Click Here!</a>" +
            "      </form>" +
            "    </main>" +
            "  </body>" +
            "</html>";

        public void Start()
        {
            if (_listener == null)
            {
                _cancellationToken = new CancellationTokenSource();
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://localhost:8000/");
                Task.Run(async () =>
                {
                    _listener.Start();

                    while (!_cancellationToken.IsCancellationRequested)
                    {
                        HttpListenerContext ctx = await _listener.GetContextAsync();
                        HttpListenerRequest req = ctx.Request;
                        HttpListenerResponse resp = ctx.Response;

                        if (req.HttpMethod == "POST" && req.Url.AbsolutePath == "/update")
                        {
                            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
                            {
                                string postStr = reader.ReadToEnd();
                                Dictionary<string, string> postData = postStr.Split('&').Aggregate(new Dictionary<string, string>(), (dict, d) => { var split = d.Split('='); dict.Add(split[0], split[1]); return dict; });
                                try
                                {
                                    if(postData.TryGetValue("twitch_oauthtoken", out var twitchOauthToken))
                                    {
                                        twitchOauthToken = HttpUtility.UrlDecode(twitchOauthToken);
                                    }
                                    _authManager.Credentials = new LoginCredentials()
                                    {
                                        Twitch_OAuthToken =  twitchOauthToken.StartsWith("oauth:") ? twitchOauthToken : !string.IsNullOrEmpty(twitchOauthToken) ? $"oauth:{twitchOauthToken}" : ""
                                    };
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "An exception occurred in OnLoginDataUpdated callback");
                                }
                            }
                        }

                        byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, _authManager.Credentials.Twitch_OAuthToken));
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
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
