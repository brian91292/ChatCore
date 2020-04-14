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
        public int WebAppPort { get; set; } = 8338;

        private ObjectSerializer _configSerializer;

        public MainSettingsProvider(ILogger<MainSettingsProvider> logger, IPathProvider pathProvider)
        {
            _logger = logger;
            _pathProvider = pathProvider;
            _configSerializer = new ObjectSerializer();
            string path = Path.Combine(_pathProvider.GetDataPath(), "settings.ini");
            if (!File.Exists(path))
            {
                _configSerializer.Save(this, path);
            }
            else
            {
                _configSerializer.Load(this, path);
            }

        }

        public void Save()
        {
            _configSerializer.Save(this, Path.Combine(_pathProvider.GetDataPath(), "settings.ini"));
        }

        private ILogger _logger;
        private IPathProvider _pathProvider;
    }
}
