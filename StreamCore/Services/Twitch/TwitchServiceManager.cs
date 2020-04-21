using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Services.Twitch
{
    public class TwitchServiceManager : IStreamingServiceManager, IDisposable
    {
        public bool IsRunning { get; private set; } = false;
        public HashSet<Assembly> RegisteredAssemblies => new HashSet<Assembly>();
        private object _lock = new object();

        public TwitchServiceManager(ILogger<TwitchServiceManager> logger, TwitchService twitchService)
        {
            _logger = logger;
            _twitchService = twitchService;
        }

        private ILogger _logger;
        private TwitchService _twitchService;

        public void Start(Assembly assembly)
        {
            lock (_lock)
            {
                RegisteredAssemblies.Add(assembly);
                if (IsRunning)
                {
                    return;
                }
                _twitchService.Start();
                IsRunning = true;
                _logger.LogInformation("Started");
            }
        }

        public void Stop(Assembly assembly)
        {
            lock (_lock)
            {
                if (!IsRunning)
                {
                    return;
                }
                if (assembly != null)
                {
                    RegisteredAssemblies.Remove(assembly);
                    if (RegisteredAssemblies.Count > 0)
                    {
                        return;
                    }
                }
                _twitchService.Stop();
                IsRunning = false;
                _logger.LogInformation("Stopped");
            }
        }

        public void Dispose()
        {
            if(IsRunning)
            {
                Stop(null);
            }
            _logger.LogInformation("Disposed");
        }

        public IStreamingService GetService()
        {
            return _twitchService;
        }
    }
}
