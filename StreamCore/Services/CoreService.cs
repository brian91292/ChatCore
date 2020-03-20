using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Services
{
    public class CoreService : IDisposable
    {
        private ILogger _logger;
        public CoreService(ILogger<CoreService> logger, IChatMessageHandler chatMessageHandler)
        {
            _logger = logger;
            _logger.LogInformation("Instantiated!");
        }

        public void Dispose()
        {

        }
    }
}
