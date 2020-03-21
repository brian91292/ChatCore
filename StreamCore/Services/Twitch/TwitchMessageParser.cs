using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models.Twitch;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        /// <summary>
        /// Takes a raw Twitch message and parses it into an IChatMessage
        /// </summary>
        /// <param name="rawMessage">The raw message sent from Twitch</param>
        /// <param name="parsedMessages">A list of chat messages that were parsed from the rawMessage</param>
        /// <returns>True if parsedMessages.Count > 0</returns>
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
                var messageMeta = new ReadOnlyDictionary<string, string>(_tagRegex.Matches(rawMessage).Cast<Match>().Aggregate(new Dictionary<string, string>(), (dict, m) => { dict[m.Groups["Tag"].Value] = m.Groups["Value"].Value; return dict; }));

                var newMessage = new TwitchMessage()
                {
                    Sender = new TwitchUser()
                    {
                        Id = messageMeta.TryGetValue("user-id", out var uid) ? uid : "",
                        Name = messageMeta.TryGetValue("display-name", out var name) ? name : "",
                        Color = messageMeta.TryGetValue("color", out var color) ? color : "", // TODO: generate random color if one doesn't exist
                        //Badges = messageMeta.TryGetValue("badges", out var badges) ? badges : "" // TODO: implement badges
                    },
                    Channel = new TwitchChannel()
                    {
                        Id = match.Groups["ChannelName"].Success ? match.Groups["ChannelName"].Value : ""
                    },
                    Message = match.Groups["Message"].Success ? match.Groups["Message"].Value : "",
                    Metadata = messageMeta,
                    Type = messageType
                };

                //_logger.LogInformation($"RawMsg: {rawMessage}");
                //foreach(var kvp in newMessage.Metadata)
                //{
                //    _logger.LogInformation($"Tag: {kvp.Key}, Value: {kvp.Value}");
                //}
                messages.Add(newMessage);
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
