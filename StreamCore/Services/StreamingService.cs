using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Services.Mixer;
using StreamCore.Services.Twitch;
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
                service.OnMessageReceived += HandleOnTextMessageReceived;
                service.OnJoinRoom += HandleOnJoinRoom;
                service.OnRoomStateUpdated += HandleOnRoomStateUpdated;
                service.OnLeaveRoom += HandleOnLeaveRoom;
            }
        }

        private ILogger _logger;
        private IList<IStreamingService> _streamingServices;
        private object _invokeLock = new object();

        private void HandleOnLeaveRoom(IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onLeaveRoomCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
            }
        }

        private void HandleOnRoomStateUpdated(IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onRoomStateUpdatedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
            }
        }

        private void HandleOnTextMessageReceived(IChatMessage message)
        {
            lock (_invokeLock)
            {
                _onTextMessageReceivedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), message, _logger);
            }
        }

        private void HandleOnJoinRoom(IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onJoinRoomCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
            }
        }

        public void SendTextMessage(string message, string channel)
        {
            foreach(var service in _streamingServices)
            {
                service.SendTextMessage(message, channel);
            }
        }

        public TwitchService GetTwitchService()
        {
            foreach (var service in _streamingServices)
            {
                if (service is TwitchService)
                {
                    return service as TwitchService;
                }
            }
            return null;
        }

        public MixerService GetMixerService()
        {
            foreach (var service in _streamingServices)
            {
                if (service is MixerService)
                {
                    return service as MixerService;
                }
            }
            return null;
        }
    }
}
