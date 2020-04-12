using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StreamCore.Config;

namespace StreamCore.Services
{
    public class UserAuthManager : IUserAuthManager
    {
        public event Action<LoginCredentials> OnCredentialsUpdated;

        private LoginCredentials _credentials = new LoginCredentials();
        public LoginCredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
                _credentialSerializer.Save(_credentials, _credentialsPath);
                OnCredentialsUpdated?.Invoke(_credentials);
            }
        }
        private ObjectSerializer _credentialSerializer;

        public UserAuthManager(ILogger<UserAuthManager> logger, IPathProvider pathProvider)
        {
            _logger = logger;
            _pathProvider = pathProvider;
            _credentialsPath = Path.Combine(_pathProvider.GetDataPath(), "auth.ini");
            _credentialSerializer = new ObjectSerializer();
            _credentialSerializer.Load(_credentials, _credentialsPath);
        }

        private ILogger _logger;
        private IPathProvider _pathProvider;
        private string _credentialsPath;
    }
}
