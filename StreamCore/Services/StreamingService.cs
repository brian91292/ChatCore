using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;

namespace StreamCore.Services
{
    public class StreamingService : StreamingServiceBase, IStreamingService
    { 
        public StreamingService(ILogger<StreamingService> logger, IList<IStreamingService> streamingServices)
        {
            _logger = logger;
            _streamingServices = streamingServices;
            foreach (var service in _streamingServices)
            {
                service.OnMessageReceived += HandleMessageReceived;
                service.OnJoinChannel += HandleOnJoinChannel;
                service.OnChannelStateUpdated += HandleChannelStateUpdated;
                service.OnLeaveChannel += HandleOnLeaveChannel;
            }
        }

        private ILogger _logger;
        private IList<IStreamingService> _streamingServices;

        private void HandleOnLeaveChannel(IChatChannel channel)
        {
            _onLeaveChannelCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
        }

        private void HandleChannelStateUpdated(IChatChannel channel)
        {
            _onChannelStateUpdatedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
        }

        private void HandleMessageReceived(IChatMessage message)
        {
            _onMessageReceivedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), message, _logger);
        }

        private void HandleOnJoinChannel(IChatChannel channel)
        {
            _onJoinChannelCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
        }

        public void SendTextMessage(string message, string channel)
        {
            foreach(var service in _streamingServices)
            {
                service.SendTextMessage(message, channel);
            }
        }
    }
}
