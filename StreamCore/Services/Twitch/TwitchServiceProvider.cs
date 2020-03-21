using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Services.Twitch
{
    public class TwitchServiceProvider : IStreamingServiceProvider, IDisposable
    {

        public event Action<IChatMessage> OnMessageReceived;

        public TwitchServiceProvider(ILogger<TwitchServiceProvider> logger, TwitchService twitchService)
        {
            _logger = logger;
            _twitchService = twitchService;
        }

        private ILogger _logger;
        private TwitchService _twitchService;

        public bool IsRunning { get; private set; } = false;

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;
            _logger.LogInformation("Started");
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            IsRunning = false;
            _logger.LogInformation("Stopped");
        }

        public void Dispose()
        {
            if(IsRunning)
            {
                Stop();
            }
            _logger.LogInformation("Disposed");
        }

        public IStreamingService GetService()
        {
            return _twitchService;
        }
    }
}
