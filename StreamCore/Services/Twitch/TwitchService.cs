using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
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

        public TwitchService(ILogger<TwitchService> logger)
        {
            _logger = logger;
        }

        private ILogger _logger;

        private readonly Regex _twitchMessageRegex = new Regex(@"^(?:@(?<Tags>[^\r\n ]*) +|())(?::(?<HostName>[^\r\n ]+) +|())(?<MessageType>[^\r\n ]+)(?: +(?<ChannelName>[^:\r\n ]+[^\r\n ]*(?: +[^:\r\n ]+[^\r\n ]*)*)|())?(?: +:(?<Message>[^\r\n]*)| +())?[\r\n]*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex _tagRegex = new Regex(@"(?<Tag>[^@^;^=]+)=(?<Value>[^;\s]+)", RegexOptions.Compiled | RegexOptions.Multiline);

        
        internal void HandleOnRawMessageReceived(Assembly assembly, string message)
        {
            var matches = _twitchMessageRegex.Matches(message);
            if(matches.Count == 0)
            {
                _logger.LogInformation($"Unhandled message: {message}");
                return;
            }

            //_logger.LogInformation($"Parsing message {message}");
            foreach(Match match in matches)
            {
                if (!match.Groups["MessageType"].Success)
                {
                    _logger.LogInformation($"Failed to get messageType for message {message}");
                    return;
                }

                string type = match.Groups["MessageType"].Value;
                string matchMessage = "";
                switch(type)
                {
                    case "PING":
                        SendRawMessageAction?.Invoke(null, "PONG :tmi.twitch.tv");
                        _logger.LogInformation("Pong!");
                        continue;
                    case "001":  // sucessful login
                        JoinChannelAction?.Invoke(null, "brian91292");
                        continue;
                    default:
                        matchMessage = match.Groups["Message"].Success ? match.Groups["Message"].Value : "None";
                        _logger.LogInformation($"Received message of type {type}. Message: {matchMessage}");

                        break;
                }
            }

            //foreach(var kvp in _onMessageReceivedCallbacks)
            //{
            //    if(kvp.Key != assembly)
            //    {
            //        try
            //        {
            //            kvp.Value?.Invoke()
            //        }
            //    }
            //}
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
