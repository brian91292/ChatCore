using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Services.Mixer
{
    public class MixerService : IStreamingService, IDisposable
    {
        public event Action<IChatMessage> OnMessageReceived;

        public Type ServiceType => typeof(MixerService);

        public MixerService(ILogger<MixerService> logger)
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
