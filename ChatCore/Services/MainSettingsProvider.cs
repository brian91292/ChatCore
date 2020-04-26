using Microsoft.Extensions.Logging;
using ChatCore.Config;
using ChatCore.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Numerics;

namespace ChatCore.Services
{
    public class MainSettingsProvider
    {
        [ConfigSection("WebApp")]
        [HTMLIgnore]
        [ConfigMeta(Comment = "Set to true to disable the webapp entirely.")]
        public bool DisableWebApp = false;
        [ConfigMeta(Comment = "Whether or not to launch the webapp in your default browser when ChatCore is started.")]
        public bool LaunchWebAppOnStartup = true;
        [ConfigMeta(Comment = "The port the webapp will run on.")]
        public int WebAppPort = 8338;

        [ConfigSection("Global")]
        [ConfigMeta(Comment = "When enabled, emojis will be parsed.")]
        public bool ParseEmojis = true;

        [ConfigSection("Twitch")]
        [ConfigMeta(Comment = "When enabled, BetterTwitchTV emotes will be parsed.")]
        public bool ParseBTTVEmotes = true;
        [ConfigMeta(Comment = "When enabled, FrankerFaceZ emotes will be parsed.")]
        public bool ParseFFZEmotes = true;
        [ConfigMeta(Comment = "When enabled, Twitch emotes will be parsed.")]
        public bool ParseTwitchEmotes = true;
        [ConfigMeta(Comment = "When enabled, Twitch cheermotes will be parsed.")]
        public bool ParseCheermotes = true;

        public MainSettingsProvider(ILogger<MainSettingsProvider> logger, IPathProvider pathProvider)
        {
            _logger = logger;
            _pathProvider = pathProvider;
            _configSerializer = new ObjectSerializer();
            string path = Path.Combine(_pathProvider.GetDataPath(), "settings.ini");
            _configSerializer.Load(this, path);
            _configSerializer.Save(this, path);
        }

        private ILogger _logger;
        private IPathProvider _pathProvider;
        private ObjectSerializer _configSerializer;

        public void Save()
        {
            _configSerializer.Save(this, Path.Combine(_pathProvider.GetDataPath(), "settings.ini"));
        }

        public Dictionary<string, string> GetSettingsAsHTML()
        {
            return _configSerializer.GetSettingsAsHTML(this);
        }

        public void SetFromDictionary(Dictionary<string, string> postData)
        {
            _configSerializer.SetFromDictionary(this, postData);
        }
    }
}
