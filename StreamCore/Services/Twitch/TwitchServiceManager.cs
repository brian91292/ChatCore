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

        public event Action<IChatMessage> OnMessageReceived;

        public bool IsRunning { get; private set; } = false;

        public TwitchServiceManager(ILogger<TwitchServiceManager> logger, TwitchService twitchService)
        {
            _logger = logger;
            _twitchService = twitchService;
        }

        private ILogger _logger;
        private TwitchService _twitchService;

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            _twitchService.Start();
            IsRunning = true;
            _logger.LogInformation("Started");
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            _twitchService.Stop();
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
