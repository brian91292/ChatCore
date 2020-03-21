using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Services
{
    public class StreamingService : IStreamingService, IDisposable
    {
        public event Action<IChatMessage> OnMessageReceived;

        public Type ServiceType => typeof(StreamingService);

        public StreamingService(ILogger<StreamingService> logger, IList<IStreamingService> streamingServices)
        {
            _logger = logger;
            _streamingServices = streamingServices;
            foreach (var service in _streamingServices)
            {
                service.OnMessageReceived += HandleMessageReceived;
            }
        }

        private ILogger _logger;
        private IList<IStreamingService> _streamingServices;

        private void HandleMessageReceived(IChatMessage message)
        {
            _logger.LogInformation($"Message received from {message.Author}. Message: {message.Message}");
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposed");
        }
    }
}
