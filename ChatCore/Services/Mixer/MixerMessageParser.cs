using ChatCore.Interfaces;
using ChatCore.Models.Mixer;
using ChatCore.SimpleJSON;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ChatCore.Services.Mixer
{
    public class MixerMessageParser : IChatMessageParser
    {
        public MixerMessageParser(ILogger<MixerMessageParser> logger, MainSettingsProvider settings, IEmojiParser emojiParser)
        {
            _logger = logger;
            _settings = settings;
            _emojiParser = emojiParser;
        }
        private ILogger _logger;
        MainSettingsProvider _settings;
        IEmojiParser _emojiParser;

        public bool ParseRawMessage(string rawMessage, ConcurrentDictionary<string, IChatChannel> channelInfo, IChatUser loggedInUser, out IChatMessage[] parsedMessage)
        {
            var parsedMessages = new List<IChatMessage>();
            parsedMessage = parsedMessages.ToArray();
            try
            {
                if (string.IsNullOrEmpty(rawMessage))
                {
                    return false;
                }
                var json = JSON.Parse(rawMessage);
                if (json == null)
                {
                    return false;
                }
                if (!json.TryGetKey("type", out var t))
                {
                    return false;
                }
                var type = t.Value;
                var messageMeta = new Dictionary<string, string>();
                if (json.TryGetKey("data", out var messageJsonData))
                {
                    foreach (var key in messageJsonData.Keys)
                    {
                        messageMeta.Add(key, messageJsonData[key].Value);
                    }
                }
                switch (type)
                {
                    case "reply":
                        parsedMessages.Add(new MixerMessage()
                        {
                            Type = type,
                            Message = rawMessage,
                            Id = json.TryGetKey("id", out var id) ? id.AsInt.ToString() : "",
                            Metadata = new ReadOnlyDictionary<string, string>(messageMeta)
                        });
                        return true;
                    case "event":
                        break;
                    default:
                        return false;
                }


                // Only events will make it through to this point
                if (!json.TryGetKey(type, out var et))
                {
                    return false;
                }
                var eventType = et.Value;
                var userName = messageJsonData.TryGetKey("user_name", out var uname) ? uname.Value : "";

                List<IChatEmote> messageEmotes = new List<IChatEmote>();
                StringBuilder messageText = new StringBuilder();
                if (messageJsonData.TryGetKey("message", out var messageJson))
                {
                    // If the mixer msg data contains a message, parse it out
                    foreach (var msg in messageJson["message"].AsArray)
                    {
                        if (!msg.Value.TryGetKey("type", out var pt))
                        {
                            // Don't even evaluate typeless message parts (is this even possible?)
                            continue;
                        }
                        var partType = pt.Value;
                        if (!msg.Value.TryGetKey("text", out var mt))
                        {
                            // Also skip textless parts
                            continue;
                        }
                        var text = mt.Value;
                        switch (partType)
                        {
                            case "emoticon":
                                if (msg.Value.TryGetKey("pack", out var p))
                                {
                                    var em = new MixerEmote()
                                    {
                                        Id = "MixerEmote_" + text,
                                        Name = text,
                                        IsAnimated = false,
                                        StartIndex = messageText.Length,
                                        EndIndex = messageText.Length + text.Length,
                                        Uri = p.Value
                                    };
                                    //_logger.LogInformation($"Emote: \"{text}\", MessageLen: {messageText.Length}, TextLen: {text.Length}, Start: {em.StartIndex}, End: {em.EndIndex}");
                                    messageEmotes.Add(em);
                                }
                                break;
                        }
                        //_logger.LogInformation($"Appending \"{text}\", type: {partType}");
                        messageText.Append(text);
                    }

                    if (_settings.ParseEmojis)
                    {
                        // Parse all emojis
                        messageEmotes.AddRange(_emojiParser.FindEmojis(messageText.ToString()));
                    }

                    // Sort the emotes in descending order to make replacing them in the string later on easier
                    messageEmotes.Sort((a, b) =>
                    {
                        return b.StartIndex - a.StartIndex;
                    });

                    _logger.LogInformation($"Message: \"{messageText.ToString()}\", Length: {messageText.Length}");
                }

                bool isModerator = false, isBroadcaster = false, isSubscriber = false;
                if (messageJsonData.TryGetKey("user_roles", out var ur))
                {
                    foreach (var role in ur.AsArray)
                    {
                        switch (role.Value.Value)
                        {
                            case "Owner":
                                isBroadcaster = true;
                                break;
                            case "Subscriber":
                                isSubscriber = true;
                                break;
                            case "Mod":
                                isModerator = true;
                                break;
                        }
                    }
                }

                var userBadges = new List<IChatBadge>();
                if (messageJsonData.TryGetKey("user_avatar", out var ua))
                {
                    var avatar = ua.Value;
                    if(ua.IsNull || avatar == "null")
                    {
                        avatar = "https://mixer.com/_latest/assets/images/main/avatars/default.png";
                    }
                    userBadges.Add(new MixerBadge()
                    {
                        Name = userName + "_UserAvatar",
                        Id = $"Mixer_{userName}_UserAvatar",
                        Uri = avatar + "?width=128&height=128"
                    });
                }

                IChatChannel mixerChannel = null;
                if (messageJsonData.TryGetKey("channel", out var c))
                {
                    string channelId = c.AsInt.ToString();
                    if (!channelInfo.TryGetValue(channelId, out mixerChannel))
                    {
                        _logger.LogInformation("Channel info was not cached! This should never happen!");
                        return false;
                    }
                }


                var newMessage = new MixerMessage()
                {
                    Id = messageJsonData.TryGetKey("id", out var mid) ? mid.Value : "",
                    Sender = new MixerUser()
                    {
                        Id = messageJsonData.TryGetKey("user_id", out var uid) ? uid.AsInt.ToString() : "",
                        UserName = userName,
                        DisplayName = userName,
                        Color = ChatUtils.GetNameColor(userName),
                        IsModerator = isModerator,
                        IsBroadcaster = isBroadcaster,
                        Badges = userBadges.ToArray()
                    },
                    Channel = mixerChannel,
                    Emotes = messageEmotes.ToArray(),
                    // TODO: ping, highlighted, action/system messages
                    Message = messageText.ToString(),
                    Type = eventType
                };
                parsedMessages.Add(newMessage);
                parsedMessage = parsedMessages.ToArray();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception while parsing Mixer message {rawMessage}");
            }
            return false;
        }
    }
}
