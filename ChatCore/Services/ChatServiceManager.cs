using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ChatCore.Services
{
    public class ChatServiceManager : IChatServiceManager, IDisposable
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

        public ChatServiceManager(ILogger<ChatServiceManager> logger, IChatService streamingService, IList<IChatServiceManager> streamServiceManagers)
        {
            _logger = logger;
            _streamingService = streamingService;
            _streamServiceManagers = streamServiceManagers;
        }

        private ILogger _logger;
        private IList<IChatServiceManager> _streamServiceManagers;
        private IChatService _streamingService;

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

        public IChatService GetService()
        {
            return _streamingService;
        }
    }
}
