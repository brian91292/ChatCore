using ChatCore.Interfaces;
using ChatCore.Models.Mixer;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ChatCore.Services.Mixer
{
    public class MixerMessageParser : IChatMessageParser
    {
        public bool ParseRawMessage(string rawMessage, ConcurrentDictionary<string, IChatChannel> channelInfo, IChatUser loggedInUser, out IChatMessage[] parsedMessage)
        {
            var parsedMessages = new List<IChatMessage>();
            parsedMessage = parsedMessages.ToArray();
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
            foreach (var key in json.Keys)
            {
                messageMeta.Add(key, json[key].Value);
            }

            var mixerMessage = new MixerMessage()
            {
                Type = type,
                Metadata = new ReadOnlyDictionary<string, string>(messageMeta)
            };
            switch (type)
            {
                case "reply":
                    mixerMessage.Message = rawMessage;
                    mixerMessage.Id = json.TryGetKey("id", out var id) ? id.AsInt.ToString() : "";
                    parsedMessages.Add(mixerMessage);
                    return true;
                default:
                    break;
            }
            return false;
        }
    }
}
