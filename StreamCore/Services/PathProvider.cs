using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StreamCore.Services
{
    public class PathProvider : IPathProvider
    {
        public string GetDataPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".streamcore");
        }

        public string GetResourcePath()
        {
            return Path.Combine(GetDataPath(), "resources");
        }
    }
}
