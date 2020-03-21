using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models.Twitch;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace StreamCore.Services.Twitch
{
    public class TwitchService : StreamingServiceBase, IStreamingService
    {
        public Type ServiceType => typeof(TwitchService);

        public TwitchService(ILogger<TwitchService> logger, TwitchMessageParser messageParser)
        {
            _logger = logger;
            _messageParser = messageParser;
        }

        private ILogger _logger;
        private IChatMessageParser _messageParser;
        
        internal void HandleOnRawMessageReceived(Assembly assembly, string message)
        {
            if(_messageParser.ParseRawMessage(message, out var parsedMessages))
            {
                foreach(TwitchMessage twitchMessage in parsedMessages)
                {
                    switch(twitchMessage.Type)
                    {
                        case "PING":
                            SendRawMessageAction?.Invoke(null, "PONG :tmi.twitch.tv");
                            _logger.LogInformation("Pong!");
                            continue;
                        case "001":  // sucessful login
                            JoinChannelAction?.Invoke(null, "brian91292");
                            continue;
                        default:
                            _logger.LogInformation($"Message: {twitchMessage.Message}");
                            break;
                    }
                }
            }
        }

        internal event Action<Assembly, string> SendRawMessageAction;
        public void SendRawMessage(string rawMessage)
        {
            SendRawMessageAction?.Invoke(Assembly.GetCallingAssembly(), rawMessage);
        }

        internal event Action<Assembly, string, string> SendTextMessageAction;
        public void SendTextMessage(string message, string channel)
        {
            SendTextMessageAction?.Invoke(Assembly.GetCallingAssembly(), message, channel);
        }

        internal event Action<Assembly, string, string> SendCommandAction;
        public void SendCommand(string command, string channel)
        {
            SendCommandAction?.Invoke(Assembly.GetCallingAssembly(), command, channel);
        }

        internal event Action<Assembly, string> JoinChannelAction;
        public void JoinChannel(string channel)
        {
            JoinChannelAction?.Invoke(Assembly.GetCallingAssembly(), channel);
        }
    }
}
