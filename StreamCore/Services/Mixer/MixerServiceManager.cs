using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Services.Mixer
{
    public class MixerServiceManager : IStreamingServiceManager, IDisposable
    {
        public bool IsRunning { get; private set; } = false;
        public HashSet<Assembly> RegisteredAssemblies => new HashSet<Assembly>();
        private object _lock = new object();

        public MixerServiceManager(ILogger<MixerServiceManager> logger, MixerService mixerService)
        {
            _logger = logger;
            _mixerService = mixerService;
        }

        private ILogger _logger;
        private MixerService _mixerService;

        public void Start(Assembly assembly)
        {
            lock (_lock)
            {
                RegisteredAssemblies.Add(assembly);
                if (IsRunning)
                {
                    return;
                }
                // TODO: run mixer service
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
                if(assembly != null)
                {
                    RegisteredAssemblies.Remove(assembly);
                    if(RegisteredAssemblies.Count > 0)
                    {
                        return;
                    }
                }
                // TODO: shutdown mixer service
                IsRunning = false;
                _logger.LogInformation("Stopped");
            }
        }

        public void Dispose()
        {
            if (IsRunning)
            {
                Stop(null);
            }
            _logger.LogInformation("Disposed");
        }

        public IStreamingService GetService()
        {
            return _mixerService;
        }
    }
}
