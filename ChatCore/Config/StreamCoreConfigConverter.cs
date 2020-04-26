using ChatCore.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChatCore.Config
{

    public class StreamCoreConfigConverter<T> : ConfigBase<T> where T : ConfigBase<T>
    {
        public StreamCoreConfigConverter(string configDirectory, string configName, string oldStreamCoreConfigPath, bool saveTriggersConfigChangedEvent = false) : base(configDirectory, configName, saveTriggersConfigChangedEvent)
        {
            if (File.Exists(oldStreamCoreConfigPath)) 
            {
                UserAuthProvider.OldConfigPath = oldStreamCoreConfigPath;
            }
        }
    }

}
