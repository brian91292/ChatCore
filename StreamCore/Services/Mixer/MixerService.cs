using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Services.Mixer
{
    public class MixerService : StreamingServiceBase, IStreamingService
    {
        public MixerService(ILogger<MixerService> logger)
        {
            _logger = logger;
        }

        private ILogger _logger;

        public void SendTextMessage(string message, string channel = null)
        {
            throw new NotImplementedException();
        }

        public void SendTextMessage(string message, IChatChannel channel)
        {
            //if(channel is MixerChannel)
            //{
            //    SendTextMessage(message, channel.Id);
            //}
        }
    }
}
