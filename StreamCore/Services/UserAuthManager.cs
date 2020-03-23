using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models;
using StreamCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
                ObjectSerializer.Save(_credentials, _credentialsPath);
                OnCredentialsUpdated?.Invoke(_credentials);
            }
        }

        public UserAuthManager(ILogger<UserAuthManager> logger)
        {
            _logger = logger;
            _credentialsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".streamcore", "auth.ini");
            ObjectSerializer.Load(_credentials, _credentialsPath);
        }

        private ILogger _logger;
        private string _credentialsPath;
    }
}
