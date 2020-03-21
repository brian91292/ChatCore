using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Services
{
    public class StreamingService : StreamingServiceBase, IStreamingService
    { 
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
            //_logger.LogDebug($"Message received from {message.Author}. Message: {message.Message}");
            foreach(var callback in _onMessageReceivedCallbacks.Values)
            {
                try
                {
                    callback?.Invoke(message);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "An exception occurred in HandleMessageReceived");
                }
            }
        }

        public void SendTextMessage(string message, string channel)
        {
            foreach(var service in _streamingServices)
            {
                service.SendTextMessage(message, channel);
            }
        }

        public void SendCommand(string command, string channel)
        {
            foreach(var service in _streamingServices)
            {
                service.SendCommand(command, channel);
            }
        }

        public void SendRawMessage(string rawMessage)
        {
            foreach (var service in _streamingServices)
            {
                service.SendRawMessage(rawMessage);
            }
        }

        public void JoinChannel(string channel)
        {
            foreach (var service in _streamingServices)
            {
                service.JoinChannel(channel);
            }
        }
    }
}
