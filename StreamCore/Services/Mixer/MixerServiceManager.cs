using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Services.Mixer
{
    public class MixerServiceManager : IStreamingServiceManager, IDisposable
    {

        public event Action<IChatMessage> OnMessageReceived;

        public MixerServiceManager(ILogger<MixerServiceManager> logger, MixerService mixerService)
        {
            _logger = logger;
            _mixerService = mixerService;
        }

        private ILogger _logger;
        private MixerService _mixerService;

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
            if (IsRunning)
            {
                Stop();
            }
            _logger.LogInformation("Disposed");
        }

        public IStreamingService GetService()
        {
            return _mixerService;
        }
    }
}
