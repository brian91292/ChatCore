using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Services.Twitch
{
    public class TwitchService : StreamingServiceBase, IStreamingService
    {
        public Type ServiceType => typeof(TwitchService);

        public TwitchService(ILogger<TwitchService> logger)
        {
            _logger = logger;
        }

        private ILogger _logger;
    }
}
