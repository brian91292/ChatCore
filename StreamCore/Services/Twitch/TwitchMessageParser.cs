using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models.Twitch;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace StreamCore.Services.Twitch
{
    public class TwitchMessageParser : IChatMessageParser
    {
        private readonly Regex _twitchMessageRegex = new Regex(@"^(?:@(?<Tags>[^\r\n ]*) +|())(?::(?<HostName>[^\r\n ]+) +|())(?<MessageType>[^\r\n ]+)(?: +(?<ChannelName>[^:\r\n ]+[^\r\n ]*(?: +[^:\r\n ]+[^\r\n ]*)*)|())?(?: +:(?<Message>[^\r\n]*)| +())?[\r\n]*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex _tagRegex = new Regex(@"(?<Tag>[^@^;^=]+)=(?<Value>[^;\s]+)", RegexOptions.Compiled | RegexOptions.Multiline);

        public TwitchMessageParser(ILogger<TwitchMessageParser> logger)
        {
            _logger = logger;
        }

        private ILogger _logger;

        public bool ParseRawMessage(string rawMessage, out IChatMessage[] parsedMessages)
        {
            parsedMessages = null;
            var matches = _twitchMessageRegex.Matches(rawMessage);
            if (matches.Count == 0)
            {
                _logger.LogInformation($"Unhandled message: {rawMessage}");
                return false;
            }

            List<IChatMessage> messages = new List<IChatMessage>();

            //_logger.LogInformation($"Parsing message {message}");
            foreach (Match match in matches)
            {
                if (!match.Groups["MessageType"].Success)
                {
                    _logger.LogInformation($"Failed to get messageType for message {rawMessage}");
                    continue;
                }

                string messageType = match.Groups["MessageType"].Value;
                messages.Add(new TwitchMessage()
                {
                    Author = "", // TODO: Implement author
                    Message = match.Groups["Message"].Success ? match.Groups["Message"].Value : "", 
                    Type = messageType
                });
            }
            if (messages.Count > 0)
            {
                parsedMessages = messages.ToArray();
                return true;
            }
            return false;
        }
    }
}
