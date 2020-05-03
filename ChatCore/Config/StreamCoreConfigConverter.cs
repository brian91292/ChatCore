using ChatCore.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChatCore.Config
{
    /// <summary>
    /// The purpose of this class is to provide a simple interface for converting an old StreamCore config file into ChatCore.
    /// </summary>
    public class StreamCoreConfigConverter<T> : ConfigBase<T> where T : ConfigBase<T>
    {
        public StreamCoreConfigConverter(string configDirectory, string configName, string oldStreamCoreConfigPath, bool saveTriggersConfigChangedEvent = false) : base(configDirectory, configName, saveTriggersConfigChangedEvent)
        {
            UserAuthProvider.OldConfigPath = oldStreamCoreConfigPath;
        }
    }

}
