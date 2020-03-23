using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StreamCore.Services
{
    public class MainSettingsProvider : ISettingsProvider
    {
        public bool RunWebApp { get; set; } = true;

        public MainSettingsProvider(ILogger<MainSettingsProvider> logger, IPathProvider pathProvider)
        {
            _logger = logger;
            _pathProvider = pathProvider;
            ObjectSerializer.Load(this, Path.Combine(_pathProvider.GetDataPath(), "settings.ini"));
        }

        public void Save()
        {
            ObjectSerializer.Save(this, Path.Combine(_pathProvider.GetDataPath(), "settings.ini"));
        }

        private ILogger _logger;
        private IPathProvider _pathProvider;
    }
}
