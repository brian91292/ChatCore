using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace StreamCore.Config
{
    public class ConfigBase<T> where T: ConfigBase<T>
    {
        private object _saveLock = new object();
        private string _configDirectory, _configFilePath;
        private FileSystemWatcher _watcher;
        private ObjectSerializer _configSerializer;
        private bool _saving = false;

        public event Action<T> OnConfigChanged;

        public ConfigBase(string configDirectory, string configName)
        {
            _configSerializer = new ObjectSerializer();
            _configDirectory = configDirectory;
            _configFilePath = Path.Combine(configDirectory, $"{configName}.ini");
            _watcher = new FileSystemWatcher()
            {
                Path = _configDirectory,
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(_configFilePath)
            };
            _watcher.Changed += _watcher_Changed;
            _watcher.EnableRaisingEvents = true;

            Load();
            Save(); // Save unconditionally after loading, incase we added new config options so they get written to file.
        }

        ~ConfigBase()
        {
            _watcher.Changed -= _watcher_Changed;
        }

        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!_saving)
            {
                Load();
                OnConfigChanged?.Invoke((T)this);
            }
        }

        public void Load()
        {
            lock (_saveLock)
            {
                try
                {
                    _configSerializer.Load(this, _configFilePath);
                }
                catch (Exception ex)
                {
                    //Logger.log.Error($"An unhandled exception occurred while trying to load config! {ex.ToString()}");
                }
            }
        }

        public void Save()
        {
            lock (_saveLock)
            {
                _saving = true;
                try
                {
                    _configSerializer.Save(this, _configFilePath);
                }
                catch (Exception ex)
                {
                    //Logger.log.Error($"An unhandled exception occurred while trying to save config! {ex.ToString()}");
                }
                _saving = false;
            }
        }
    }
}
