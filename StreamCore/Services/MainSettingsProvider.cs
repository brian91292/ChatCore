using Microsoft.Extensions.Logging;
using StreamCore.Config;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StreamCore.Services
{
    public class MainSettingsProvider : ISettingsProvider
    {
        public bool RunWebApp { get; set; } = true;
        private ObjectSerializer _configSerializer;

        public MainSettingsProvider(ILogger<MainSettingsProvider> logger, IPathProvider pathProvider)
        {
            _logger = logger;
            _pathProvider = pathProvider;
            _configSerializer = new ObjectSerializer(this);
            _configSerializer.Load(Path.Combine(_pathProvider.GetDataPath(), "settings.ini"));
        }

        public void Save()
        {
            _configSerializer.Save(Path.Combine(_pathProvider.GetDataPath(), "settings.ini"));
        }

        private ILogger _logger;
        private IPathProvider _pathProvider;
    }
}
