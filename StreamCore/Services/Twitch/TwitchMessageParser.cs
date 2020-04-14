using Microsoft.Extensions.Logging;
using StreamCore.Interfaces;
using StreamCore.Models;
using StreamCore.Models.Twitch;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StreamCore.Services.Twitch
{
    public class TwitchMessageParser : IChatMessageParser
    {
        private readonly Regex _twitchMessageRegex = new Regex(@"^(?:@(?<Tags>[^\r\n ]*) +|())(?::(?<HostName>[^\r\n ]+) +|())(?<MessageType>[^\r\n ]+)(?: +(?<ChannelName>[^:\r\n ]+[^\r\n ]*(?: +[^:\r\n ]+[^\r\n ]*)*)|())?(?: +:(?<Message>[^\r\n]*)| +())?[\r\n]*$", RegexOptions.Compiled | RegexOptions.Multiline);
        private readonly Regex _tagRegex = new Regex(@"(?<Tag>[^@^;^=]+)=(?<Value>[^;\s]+)", RegexOptions.Compiled | RegexOptions.Multiline);

        public TwitchMessageParser(ILogger<TwitchMessageParser> logger, TwitchDataProvider twitchDataProvider, IEmojiParser emojiParser)
        {
            _logger = logger;
            _twitchDataProvider = twitchDataProvider;
            _emojiParser = emojiParser;
        }

        private ILogger _logger;
        private TwitchDataProvider _twitchDataProvider;
        private IEmojiParser _emojiParser;
        private Dictionary<int, string> _userColors = new Dictionary<int, string>();
        private string GetNameColor(string name)
        {
            int nameHash = name.GetHashCode();
            if (!_userColors.TryGetValue(nameHash, out var nameColor))
            {
                // Generate a psuedo-random color based on the users display name
                Random rand = new Random(nameHash);
                int argb = (rand.Next(255) << 16) + (rand.Next(255) << 8) + rand.Next(255);
                string colorString = string.Format("#{0:X6}FF", argb);
                _userColors.Add(nameHash, colorString);
                nameColor = colorString;
            }
            return nameColor;
        }

        /// <summary>
        /// Takes a raw Twitch message and parses it into an IChatMessage
        /// </summary>
        /// <param name="rawMessage">The raw message sent from Twitch</param>
        /// <param name="parsedMessages">A list of chat messages that were parsed from the rawMessage</param>
        /// <returns>True if parsedMessages.Count > 0</returns>
        public bool ParseRawMessage(string rawMessage, ConcurrentDictionary<string, IChatChannel> channelInfo, IChatUser loggedInUser, out IChatMessage[] parsedMessages)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            //TODO: fix exception when user only sends a single emote with no spaces
            parsedMessages = null;
            var matches = _twitchMessageRegex.Matches(rawMessage);
            if (matches.Count == 0)
            {
                _logger.LogInformation($"Unhandled message: {rawMessage}");
                stopwatch.Stop();
                return false;
            }

            List<IChatMessage> messages = new List<IChatMessage>();
            //_logger.LogInformation($"Parsing message {rawMessage}");
            foreach (Match match in matches)
            {
                if (!match.Groups["MessageType"].Success)
                {
                    _logger.LogInformation($"Failed to get messageType for message {match.Value}");
                    continue;
                }

                //_logger.LogInformation($"Message: {match.Value}");

                string messageType = match.Groups["MessageType"].Value;
                string messageText = match.Groups["Message"].Success ? match.Groups["Message"].Value : "";
                string messageChannelName = match.Groups["ChannelName"].Success ? match.Groups["ChannelName"].Value.Trim(new char[] { '#' }) : "";

                if (!channelInfo.TryGetValue(messageChannelName, out var channel))
                {
                    //_logger.LogWarning($"Channel info has not been set yet for channel {messageChannelName}");
                }

                try
                {
                    IChatBadge[] userBadges = new IChatBadge[0];
                    List<IChatEmote> messageEmotes = new List<IChatEmote>();
                    TwitchRoomstate messageRoomstate = null;
                    HashSet<string> foundTwitchEmotes = new HashSet<string>();

                    bool isActionMessage = false, isHighlighted = false;
                    if (messageText.StartsWith("\u0001ACTION"))
                    {
                        messageText = messageText.Remove(messageText.Length - 1, 1).Remove(0, 8);
                        isActionMessage = true;
                    }

                    var messageMeta = new ReadOnlyDictionary<string, string>(_tagRegex.Matches(match.Value).Cast<Match>().Aggregate(new Dictionary<string, string>(), (dict, m) =>
                    {
                        dict[m.Groups["Tag"].Value] = m.Groups["Value"].Value;
                        return dict;
                    }));

                    int messageBits = messageMeta.TryGetValue("bits", out var bitsString) && int.TryParse(bitsString, out var bitsInt) ? bitsInt : 0;

                    if (messageMeta.TryGetValue("badges", out var badgeStr))
                    {
                        userBadges = badgeStr.Split(',').Aggregate(new List<IChatBadge>(), (list, m) =>
                        {
                            var badgeId = m.Replace("/", "");
                            if (_twitchDataProvider.TryGetBadgeInfo(badgeId, messageChannelName, out var badgeInfo))
                            {
                                list.Add(new TwitchBadge()
                                {
                                    Id = $"{badgeInfo.Type}_{badgeId}",
                                    Name = m.Split('/')[0],
                                    Uri = badgeInfo.Uri
                                });
                            }
                            return list;
                        }).ToArray();
                    }

                    if (messageType == "PRIVMSG" || messageType == "NOTIFY" || messageType == "USERNOTICE")
                    {
                        if (messageText.Length > 0)
                        {
                            if (messageMeta.TryGetValue("emotes", out var emoteStr))
                            {
                                // Parse all the normal Twitch emotes
                                messageEmotes = emoteStr.Split('/').Aggregate(new List<IChatEmote>(), (emoteList, emoteInstanceString) =>
                                {
                                    var emoteParts = emoteInstanceString.Split(':');
                                    foreach (var instanceString in emoteParts[1].Split(','))
                                    {
                                        var instanceParts = instanceString.Split('-');
                                        int startIndex = int.Parse(instanceParts[0]);
                                        int endIndex = int.Parse(instanceParts[1]);

                                        if (startIndex >= messageText.Length)
                                        {
                                            _logger.LogWarning($"Start index is greater than message length! RawMessage: {match.Value}, InstanceString: {instanceString}, EmoteStr: {emoteStr}, StartIndex: {startIndex}, MessageLength: {messageText.Length}, IsActionMessage: {isActionMessage}");
                                        }
                                        string emoteName = messageText.Substring(startIndex, endIndex - startIndex + 1);
                                        foundTwitchEmotes.Add(emoteName);
                                        emoteList.Add(new TwitchEmote()
                                        {
                                            Id = $"TwitchEmote_{emoteParts[0]}",
                                            Name = emoteName,//endIndex >= messageText.Length ? messageText.Substring(startIndex) : ,
                                            Uri = $"https://static-cdn.jtvnw.net/emoticons/v1/{emoteParts[0]}/3.0",
                                            StartIndex = startIndex,
                                            EndIndex = endIndex,
                                            IsAnimated = false,
                                            Bits = 0,
                                            Color = ""
                                        });
                                    }
                                    return emoteList;
                                });
                            }

                            // Parse all the third party (BTTV, FFZ, etc) emotes
                            StringBuilder currentWord = new StringBuilder();
                            for (int i = 0; i <= messageText.Length; i++)
                            {
                                if (i == messageText.Length || char.IsWhiteSpace(messageText[i]))
                                {
                                    if (currentWord.Length > 0)
                                    {
                                        var lastWord = currentWord.ToString();
                                        int startIndex = i - lastWord.Length;
                                        int endIndex = i - 1;



                                        if (!foundTwitchEmotes.Contains(lastWord))
                                        {

                                            // Make sure we haven't already matched a Twitch emote with the same string, just incase the user has a BTTV/FFZ emote with the same name
                                            if (messageBits > 0 && _twitchDataProvider.TryGetCheermote(lastWord, messageChannelName, out var cheermoteData, out var numBits) && numBits > 0)
                                            {
                                                //_logger.LogInformation($"Got cheermote! Total message bits: {messageBits}");
                                                var tier = cheermoteData.GetTier(numBits);
                                                if (tier != null)
                                                {
                                                    messageEmotes.Add(new TwitchEmote()
                                                    {
                                                        Id = $"TwitchCheermote_{cheermoteData.Prefix}{tier.MinBits}",
                                                        Name = lastWord,
                                                        Uri = tier.Uri,
                                                        StartIndex = startIndex,
                                                        EndIndex = endIndex,
                                                        IsAnimated = true,
                                                        Bits = numBits,
                                                        Color = tier.Color
                                                    });
                                                }
                                            }
                                            else if (_twitchDataProvider.TryGetThirdPartyEmote(lastWord, messageChannelName, out var emoteData))
                                            {
                                                messageEmotes.Add(new TwitchEmote()
                                                {
                                                    Id = $"{emoteData.Type}_{lastWord}",
                                                    Name = lastWord,
                                                    Uri = emoteData.Uri,
                                                    StartIndex = startIndex,
                                                    EndIndex = endIndex,
                                                    IsAnimated = emoteData.IsAnimated,
                                                    Bits = 0,
                                                    Color = ""
                                                });
                                            }
                                        }
                                        currentWord.Clear();
                                    }
                                }
                                else
                                {
                                    currentWord.Append(messageText[i]);
                                }
                            }

                            // Parse all emojis
                            messageEmotes.AddRange(_emojiParser.FindEmojis(messageText));

                            // Sort the emotes in descending order to make replacing them in the string later on easier
                            messageEmotes.Sort((a, b) =>
                            {
                                return b.StartIndex - a.StartIndex;
                            });
                        }
                    }
                    else if (messageType == "ROOMSTATE")
                    {
                        messageRoomstate = new TwitchRoomstate()
                        {
                            BroadcasterLang = messageMeta.TryGetValue("broadcaster-lang", out var lang) ? lang : "",
                            RoomId = messageMeta.TryGetValue("room-id", out var roomId) ? roomId : "",
                            EmoteOnly = messageMeta.TryGetValue("emote-only", out var emoteOnly) ? emoteOnly == "1" : false,
                            FollowersOnly = messageMeta.TryGetValue("followers-only", out var followersOnly) ? followersOnly != "-1" : false,
                            MinFollowTime = followersOnly != "-1" && int.TryParse(followersOnly, out var minFollowTime) ? minFollowTime : 0,
                            R9K = messageMeta.TryGetValue("r9k", out var r9k) ? r9k == "1" : false,
                            SlowModeInterval = messageMeta.TryGetValue("slow", out var slow) && int.TryParse(slow, out var slowModeInterval) ? slowModeInterval : 0,
                            SubscribersOnly = messageMeta.TryGetValue("subs-only", out var subsOnly) ? subsOnly == "1" : false
                        };
                        if (channel is TwitchChannel twitchChannel)
                        {
                            twitchChannel.Roomstate = messageRoomstate;
                        }
                    }

                    string messageSenderName = messageMeta.TryGetValue("display-name", out var name) ? name : match.Groups["HostName"].Success ? match.Groups["HostName"].Value.Split('!')[0] : "";
                    var newMessage = new TwitchMessage()
                    {
                        Id = messageMeta.TryGetValue("id", out var messageId) ? messageId : "", // TODO: default id of some sort?
                        Sender = new TwitchUser()
                        {
                            Id = messageMeta.TryGetValue("user-id", out var uid) ? uid : "",
                            Name = messageSenderName,
                            Color = messageMeta.TryGetValue("color", out var color) ? color : GetNameColor(messageSenderName),
                            IsModerator = badgeStr != null && badgeStr.Contains("moderator/"),
                            IsBroadcaster = badgeStr != null && badgeStr.Contains("broadcaster/"),
                            IsSubscriber = badgeStr != null && (badgeStr.Contains("subscriber/") || badgeStr.Contains("founder/")),
                            IsTurbo = badgeStr != null && badgeStr.Contains("turbo/"),
                            IsVip = badgeStr != null && badgeStr.Contains("vip/"),
                            Badges = userBadges
                        },
                        Channel = channel != null ? channel : new TwitchChannel()
                        {
                            Id = messageChannelName,
                            Roomstate = messageRoomstate
                        },
                        Emotes = messageEmotes.ToArray(), // TODO: implement cheermote parsing
                        Message = messageText,
                        IsActionMessage = isActionMessage,
                        IsSystemMessage = messageType == "NOTICE" || messageType == "USERNOTICE",
                        IsHighlighted = isHighlighted,
                        IsPing = !string.IsNullOrEmpty(messageText) && loggedInUser != null && messageText.Contains($"@{loggedInUser.Name}", StringComparison.OrdinalIgnoreCase),
                        Metadata = messageMeta,
                        Type = messageType
                    };

                    if(messageMeta.TryGetValue("msg-id", out var msgIdValue))
                    {
                        TwitchMessage systemMessage = null;
                        //_logger.LogInformation($"msg-id: {msgIdValue}");
                        //_logger.LogInformation($"Message: {match.Value}");
                        switch(msgIdValue)
                        {
                            case "skip-subs-mode-message":
                                systemMessage = (TwitchMessage)newMessage.Clone();
                                systemMessage.Message = "Redeemed Send a Message In Sub-Only Mode";
                                systemMessage.IsHighlighted = false;
                                systemMessage.IsSystemMessage = true;
                                systemMessage.Emotes = new IChatEmote[0];
                                messages.Add(systemMessage);
                                break;
                            case "highlighted-message":
                                systemMessage = (TwitchMessage)newMessage.Clone();
                                systemMessage.Message = "Redeemed Highlight My Message";
                                systemMessage.IsHighlighted = true;
                                systemMessage.IsSystemMessage = true;
                                systemMessage.Emotes = new IChatEmote[0];
                                messages.Add(systemMessage);
                                break;
                            case "sub":
                            case "resub":
                                if (messageMeta.TryGetValue("system-msg", out var systemMsgText))
                                {
                                    //_logger.LogInformation($"Message: {match.Value}");
                                    if (messageMeta.TryGetValue("msg-param-sub-plan", out var subPlanName))
                                    {
                                        systemMessage = (TwitchMessage)newMessage.Clone();
                                        systemMsgText = systemMsgText.Replace(@"\s", " ");
                                        if (subPlanName == "Prime")
                                        {
                                            systemMessage.Message = $"👑  {systemMsgText}";
                                        }
                                        else
                                        {
                                            systemMessage.Message = $"⭐  {systemMsgText}";
                                        }
                                        systemMessage.IsHighlighted = true;
                                        systemMessage.Emotes = _emojiParser.FindEmojis(systemMessage.Message).ToArray();
                                        messages.Add(systemMessage);
                                    }
                                }
                                newMessage.IsSystemMessage = false;
                                if (string.IsNullOrEmpty(newMessage.Message))
                                {
                                    // If there was no actual message, then we only need to queue up the system message
                                    continue;
                                }
                                break;
                        }
                    }

                    //_logger.LogInformation($"RawMsg: {rawMessage}");
                    //foreach(var kvp in newMessage.Metadata)
                    //{
                    //    _logger.LogInformation($"Tag: {kvp.Key}, Value: {kvp.Value}");
                    //}
                    messages.Add(newMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception while parsing Twitch message {messageText}");
                }
            }
            if (messages.Count > 0)
            {
                stopwatch.Stop();
                _logger.LogDebug($"Successfully parsed {messages.Count} messages in {(decimal)stopwatch.ElapsedTicks/TimeSpan.TicksPerMillisecond}ms");
                parsedMessages = messages.ToArray();
                return true;
            }
            _logger.LogInformation("No messages were parsed successfully.");
            return false;
        }
    }
}
