using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Services.Twitch
{
    public class TwitchServiceManager : IStreamingServiceManager, IDisposable
    {

        public event Action<IChatMessage> OnMessageReceived;

        public bool IsRunning { get; private set; } = false;

        public TwitchServiceManager(ILogger<TwitchServiceManager> logger, TwitchService twitchService, IWebSocketService websocketService, Random rand)
        {
            _logger = logger;
            _twitchService = twitchService;
            _websocketService = websocketService;
            _rand = rand;

            _twitchService.SendCommandAction += _twitchService_SendCommand;
            _twitchService.SendTextMessageAction += _twitchService_SendTextMessageAction;
            _twitchService.SendRawMessageAction += _twitchService_SendRawMessageAction;
            _twitchService.JoinChannelAction += _twitchService_JoinChannelAction;
        }


        private ILogger _logger;
        private TwitchService _twitchService;
        private IWebSocketService _websocketService;
        private Random _rand;

        private void _twitchService_JoinChannelAction(Assembly assembly, string channel)
        {
            if (_websocketService.IsConnected)
            {
                string rawMessage = $"JOIN #{channel}";
                _websocketService.SendMessage(rawMessage);
                _websocketService_OnMessageReceived(assembly, rawMessage);
            }
        }

        private void _twitchService_SendRawMessageAction(Assembly assembly, string rawMessage)
        {
            if (_websocketService.IsConnected)
            {
                _websocketService.SendMessage(rawMessage);
                _websocketService_OnMessageReceived(assembly, rawMessage);
            }
        }

        private void _twitchService_SendTextMessageAction(Assembly assembly, string message, string channel)
        {
            if (_websocketService.IsConnected)
            {
                string rawMessage = $"PRIVMSG #{channel} :{message}";
                _websocketService.SendMessage(rawMessage);
                _websocketService_OnMessageReceived(assembly, rawMessage);
            }
        }

        private void _twitchService_SendCommand(Assembly assembly, string command, string channel)
        {
            if(_websocketService.IsConnected)
            {
                string rawMessage = $"PRIVMSG #{channel} :{command}";
                _websocketService.SendMessage(rawMessage);
                _websocketService_OnMessageReceived(assembly, rawMessage);
            }
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }
            IsRunning = true;
            _websocketService.OnOpen += _websocketService_OnOpen;
            _websocketService.OnClose += _websocketService_OnClose;
            _websocketService.OnMessageReceived += _websocketService_OnMessageReceived;
            _websocketService.Connect("wss://irc-ws.chat.twitch.tv:443");
            _logger.LogInformation("Started");
        }

        private void _websocketService_OnMessageReceived(Assembly assembly, string message)
        {
            _twitchService.HandleOnRawMessageReceived(assembly, message);
        }

        private void _websocketService_OnClose()
        {
            _logger.LogInformation("Twitch connection closed");
        }

        private void _websocketService_OnOpen()
        {
            _logger.LogInformation("Twitch connection opened");
            _websocketService.SendMessage("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership");
            _websocketService.SendMessage($"NICK justinfan{_rand.Next(10000, 1000000)}");
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            IsRunning = false;
            _websocketService.Disconnect();
            _logger.LogInformation("Stopped");
        }

        public void Dispose()
        {
            if(IsRunning)
            {
                Stop();
            }
            _logger.LogInformation("Disposed");
        }

        public IStreamingService GetService()
        {
            return _twitchService;
        }
    }
}
