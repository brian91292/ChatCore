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
using StreamCore.Models.Twitch;

namespace StreamCore.Services
{
    /// <summary>
    /// A multiplexer for all the supported streaming services.
    /// </summary>
    public class StreamingService : StreamingServiceBase, IStreamingService
    {
        public string DisplayName { get; private set; } = "Generic";

        public StreamingService(ILogger<StreamingService> logger, IList<IStreamingService> streamingServices)
        {
            _logger = logger;
            _streamingServices = streamingServices;
            _twitchService = (TwitchService)streamingServices.First(s => s is TwitchService);
            _mixerService = (MixerService)streamingServices.First(s => s is MixerService);

            StringBuilder sb = new StringBuilder();
            foreach (var service in _streamingServices)
            {
                service.OnTextMessageReceived += Service_OnTextMessageReceived;
                service.OnJoinChannel += Service_OnJoinChannel;
                service.OnRoomStateUpdated += Service_OnRoomStateUpdated;
                service.OnLeaveChannel += Service_OnLeaveChannel;
                service.OnLogin += Service_OnLogin;
                service.OnChatCleared += Service_OnChatCleared;
                service.OnMessageCleared += Service_OnMessageCleared;

                if(sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(service.DisplayName);
            }
            DisplayName = sb.ToString();
        }

        private ILogger _logger;
        private IList<IStreamingService> _streamingServices;
        private TwitchService _twitchService;
        private MixerService _mixerService;
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

        public void SendTextMessage(string message, IChatChannel channel)
        {
            if(channel is TwitchChannel)
            {
                _twitchService.SendTextMessage(Assembly.GetCallingAssembly(), message, channel.Id);
            }
        }

        public TwitchService GetTwitchService()
        {
            return _twitchService;
        }

        public MixerService GetMixerService()
        {
            return _mixerService;
        }
    }
}
