using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Services.Mixer;
using StreamCore.Services.Twitch;
using StreamCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Linq;

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
                service.OnTextMessageReceived += Service_OnTextMessageReceived;
                service.OnJoinChannel += Service_OnJoinChannel;
                service.OnRoomStateUpdated += Service_OnRoomStateUpdated;
                service.OnLeaveChannel += Service_OnLeaveChannel;
                service.OnLogin += Service_OnLogin;
                service.OnChatCleared += Service_OnChatCleared;
                service.OnMessageCleared += Service_OnMessageCleared;
            }
        }


        private ILogger _logger;
        private IList<IStreamingService> _streamingServices;
        private object _invokeLock = new object();


        private void Service_OnMessageCleared(string messageId)
        {
            lock (_invokeLock)
            {
                _onMessageClearedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), messageId, _logger);
            }
        }

        private void Service_OnChatCleared(string userId)
        {
            lock (_invokeLock)
            {
                _onChatClearedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), userId, _logger);
            }
        }

        private void Service_OnLogin(IStreamingService svc)
        {
            lock (_invokeLock)
            {
                _onLoginCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, _logger);
            }
        }

        private void Service_OnLeaveChannel(IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onLeaveRoomCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
            }
        }

        private void Service_OnRoomStateUpdated(IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onRoomStateUpdatedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
            }
        }

        private void Service_OnTextMessageReceived(IChatMessage message)
        {
            lock (_invokeLock)
            {
                _onTextMessageReceivedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), message, _logger);
            }
        }

        private void Service_OnJoinChannel(IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onJoinRoomCallbacks.InvokeAll(Assembly.GetCallingAssembly(), channel, _logger);
            }
        }

        public TwitchService GetTwitchService()
        {
            return (TwitchService)_streamingServices.FirstOrDefault(s => s is TwitchService);
        }

        public MixerService GetMixerService()
        {
            return (MixerService)_streamingServices.FirstOrDefault(s => s is MixerService);
        }
    }
}
