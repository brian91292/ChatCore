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


        private void Service_OnMessageCleared(IStreamingService svc, string messageId)
        {
            lock (_invokeLock)
            {
                _onMessageClearedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, messageId, _logger);
            }
        }

        private void Service_OnChatCleared(IStreamingService svc, string userId)
        {
            lock (_invokeLock)
            {
                _onChatClearedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, userId, _logger);
            }
        }

        private void Service_OnLogin(IStreamingService svc)
        {
            lock (_invokeLock)
            {
                _onLoginCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, _logger);
            }
        }

        private void Service_OnLeaveChannel(IStreamingService svc, IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onLeaveRoomCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, channel, _logger);
            }
        }

        private void Service_OnRoomStateUpdated(IStreamingService svc, IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onRoomStateUpdatedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, channel, _logger);
            }
        }

        private void Service_OnTextMessageReceived(IStreamingService svc, IChatMessage message)
        {
            lock (_invokeLock)
            {
                _onTextMessageReceivedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, message, _logger);
            }
        }

        private void Service_OnJoinChannel(IStreamingService svc, IChatChannel channel)
        {
            lock (_invokeLock)
            {
                _onJoinRoomCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, channel, _logger);
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
