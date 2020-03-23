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

        public event Action<IChatMessage> OnMessageReceived;

        public StreamingServiceManager(ILogger<StreamingServiceManager> logger, IStreamingService streamingService, IList<IStreamingServiceManager> streamServiceManagers)
        {
            _logger = logger;
            _streamingService = streamingService;
            _streamServiceManagers = streamServiceManagers;
        }

        private ILogger _logger;
        private IList<IStreamingServiceManager> _streamServiceManagers;
        private IStreamingService _streamingService;

        public void Start()
        {
            foreach (var service in _streamServiceManagers)
            {
                service.Start();
            }
            _logger.LogInformation($"Streaming services have been started");
        }

        public void Stop()
        {
            foreach (var service in _streamServiceManagers)
            {
                service.Stop();
            }
            _logger.LogInformation($"Streaming services have been stopped");
        }

        public void Dispose()
        {
            foreach(var service in _streamServiceManagers)
            {
                service.Stop();
            }
            _logger.LogInformation("Disposed");
        }

        public IStreamingService GetService()
        {
            return _streamingService;
        }
    }
}
