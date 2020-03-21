using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Services.Twitch
{
    public class TwitchService : IStreamingService, IDisposable
    {
        public event Action<IChatMessage> OnMessageReceived;

        public Type ServiceType => typeof(TwitchService);

        public TwitchService(ILogger<TwitchService> logger)
        {
            _logger = logger;
        }

        private ILogger _logger;

        public void Dispose()
        {
            _logger.LogInformation("Disposed");
        }
    }
}
