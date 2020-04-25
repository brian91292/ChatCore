using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using ChatCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChatCore.Config;

namespace ChatCore.Services
{
    public class UserAuthProvider : IUserAuthProvider
    {
        public event Action<LoginCredentials> OnCredentialsUpdated;

        public LoginCredentials Credentials { get; } = new LoginCredentials();

        public UserAuthProvider(ILogger<UserAuthProvider> logger, IPathProvider pathProvider)
        {
            _logger = logger;
            _pathProvider = pathProvider;
            _credentialsPath = Path.Combine(_pathProvider.GetDataPath(), "auth.ini");
            _credentialSerializer = new ObjectSerializer();
            _credentialSerializer.Load(Credentials, _credentialsPath);
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
