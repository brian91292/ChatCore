using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Services
{
    public class StreamServiceProvider : IStreamingServiceManager, IDisposable
    {
        public bool IsRunning { get; set; } = false;

        public event Action<IChatMessage> OnMessageReceived;

        public StreamServiceProvider(ILogger<StreamServiceProvider> logger, IStreamingService streamingService, IList<IStreamingServiceManager> streamServiceProviders)
        {
            _logger = logger;
            _streamingService = streamingService;
            _streamServiceProviders = streamServiceProviders;
        }

        private ILogger _logger;
        private IList<IStreamingServiceManager> _streamServiceProviders;
        private IStreamingService _streamingService;

        public void Start()
        {
            foreach (var service in _streamServiceProviders)
            {
                service.Start();
            }
            _logger.LogInformation($"Streaming services have been started");
        }

        public void Stop()
        {
            foreach (var service in _streamServiceProviders)
            {
                service.Stop();
            }
            _logger.LogInformation($"Streaming services have been stopped");
        }

        public void Dispose()
        {
            foreach(var service in _streamServiceProviders)
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
