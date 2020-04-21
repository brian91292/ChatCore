using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Services
{
    public class StreamingServiceManager : IStreamingServiceManager, IDisposable
    {
        public bool IsRunning { get; set; } = false;
        public HashSet<Assembly> RegisteredAssemblies
        {
            get
            {
                HashSet<Assembly> assemblies = new HashSet<Assembly>();
                foreach(var service in _streamServiceManagers)
                {
                    assemblies.UnionWith(service.RegisteredAssemblies);
                }
                return assemblies;
            }
        }
        private object _lock = new object();

        public StreamingServiceManager(ILogger<StreamingServiceManager> logger, IStreamingService streamingService, IList<IStreamingServiceManager> streamServiceManagers)
        {
            _logger = logger;
            _streamingService = streamingService;
            _streamServiceManagers = streamServiceManagers;
        }

        private ILogger _logger;
        private IList<IStreamingServiceManager> _streamServiceManagers;
        private IStreamingService _streamingService;

        public void Start(Assembly assembly)
        {
            foreach (var service in _streamServiceManagers)
            {
                service.Start(assembly);
            }
            _logger.LogInformation($"Streaming services have been started");
        }

        public void Stop(Assembly assembly)
        {
            foreach (var service in _streamServiceManagers)
            {
                service.Stop(assembly);
            }
            _logger.LogInformation($"Streaming services have been stopped");
        }

        public void Dispose()
        {
            foreach(var service in _streamServiceManagers)
            {
                service.Stop(null);
            }
            _logger.LogInformation("Disposed");
        }

        public IStreamingService GetService()
        {
            return _streamingService;
        }
    }
}
