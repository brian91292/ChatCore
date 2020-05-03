using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using ChatCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChatCore.Config;
using System.Threading.Tasks;

namespace ChatCore.Services
{
    class OldStreamCoreConfig
    {
        public string TwitchChannelName;
        public string TwitchUsername;
        public string TwitchOAuthToken;
    }

    public class UserAuthProvider : IUserAuthProvider
    {
        public event Action<LoginCredentials> OnCredentialsUpdated;

        public LoginCredentials Credentials { get; } = new LoginCredentials();

        // If this is set, old StreamCore config data will be read in from this file.
        internal static string OldConfigPath = null;

        public UserAuthProvider(ILogger<UserAuthProvider> logger, IPathProvider pathProvider)
        {
            _logger = logger;
            _pathProvider = pathProvider;
            _credentialsPath = Path.Combine(_pathProvider.GetDataPath(), "auth.ini");
            _credentialSerializer = new ObjectSerializer();
            _credentialSerializer.Load(Credentials, _credentialsPath);

            Task.Delay(1000).ContinueWith((task) =>
            {
                if (!string.IsNullOrEmpty(OldConfigPath) && File.Exists(OldConfigPath))
                {
                    _logger.LogInformation($"Trying to convert old StreamCore config at path {OldConfigPath}");
                    var old = new OldStreamCoreConfig();
                    _credentialSerializer.Load(old, OldConfigPath);
                    if (!string.IsNullOrEmpty(old.TwitchChannelName))
                    {
                        var oldName = old.TwitchChannelName.ToLower().Replace(" ", "");
                        if (!Credentials.Twitch_Channels.Contains(oldName))
                        {
                            Credentials.Twitch_Channels.Add(oldName);
                            _logger.LogInformation($"Added channel {oldName} from old StreamCore config.");
                        }
                    }
                    if (!string.IsNullOrEmpty(old.TwitchOAuthToken))
                    {
                        Credentials.Twitch_OAuthToken = old.TwitchOAuthToken;
                        _logger.LogInformation($"Pulled in old Twitch auth info from StreamCore config.");
                    }
                    var convertedPath = OldConfigPath + ".converted";
                    try
                    {
                        if (!File.Exists(convertedPath))
                        {
                            File.Move(OldConfigPath, convertedPath);
                        }
                        else
                        {
                            File.Delete(OldConfigPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "An exception occurred while trying to yeet old StreamCore config!");
                    }
                }
            });
        }

        private ILogger _logger;
        private IPathProvider _pathProvider;
        private string _credentialsPath;
        private ObjectSerializer _credentialSerializer;

        public void Save()
        {
            _credentialSerializer.Save(Credentials, _credentialsPath);
            OnCredentialsUpdated?.Invoke(Credentials);
        }
    }
}
