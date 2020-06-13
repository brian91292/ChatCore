using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ChatCore.Models.Mixer
{
    public class MixerMessage : IChatMessage
    {
        public string Id { get; internal set; }

        public bool IsSystemMessage { get; internal set; }

        public bool IsActionMessage { get; internal set; }

        public bool IsHighlighted { get; internal set; }

        public bool IsPing { get; internal set; }

        public string Message { get; internal set; }

        public IChatUser Sender { get; internal set; }

        public IChatChannel Channel { get; internal set; }

        public IChatEmote[] Emotes { get; internal set; }

        public string Type { get; internal set; }

        public ReadOnlyDictionary<string, string> Metadata { get; internal set; }
        public MixerMessage() { }
        public MixerMessage(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(IsSystemMessage), out var isSystemMessage)) { IsSystemMessage = isSystemMessage.AsBool; }
            if (obj.TryGetKey(nameof(IsActionMessage), out var isActionMessage)) { IsActionMessage = isActionMessage.AsBool; }
            if (obj.TryGetKey(nameof(IsHighlighted), out var isHighlighted)) { IsHighlighted = isHighlighted.AsBool; }
            if (obj.TryGetKey(nameof(IsPing), out var isPing)) { IsPing = isPing.AsBool; }
            if (obj.TryGetKey(nameof(Message), out var message)) { Message = message.Value; }
            if (obj.TryGetKey(nameof(Sender), out var sender)) { Sender = new MixerUser(sender.ToString()); }
            if (obj.TryGetKey(nameof(Channel), out var channel)) { Channel = new MixerChannel(channel.ToString()); }
            if (obj.TryGetKey(nameof(Emotes), out var emotes))
            {
                List<IChatEmote> emoteList = new List<IChatEmote>();
                foreach (var emote in emotes.AsArray)
                {
                    if (emote.Value.TryGetKey(nameof(IChatEmote.Id), out var emoteNode))
                    {
                        var emoteId = emoteNode.Value;
                        if (emoteId.StartsWith("Mixer"))
                        {
                            emoteList.Add(new MixerEmote(emote.Value.ToString()));
                        }
                        else if (emoteId.StartsWith("Emoji"))
                        {
                            emoteList.Add(new Emoji(emote.Value.ToString()));
                        }
                        else
                        {
                            emoteList.Add(new UnknownChatEmote(emote.Value.ToString()));
                        }
                    }
                }
                Emotes = emoteList.ToArray();
            }
            if(obj.TryGetKey(nameof(Type), out var type)) { Type = type.Value; }
        }

        public JSONObject ToJson()
        {
            JSONObject obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            obj.Add(nameof(IsSystemMessage), new JSONBool(IsSystemMessage));
            obj.Add(nameof(IsActionMessage), new JSONBool(IsActionMessage));
            obj.Add(nameof(IsActionMessage), new JSONBool(IsActionMessage));
            obj.Add(nameof(IsHighlighted), new JSONBool(IsHighlighted));
            obj.Add(nameof(IsPing), new JSONBool(IsPing));
            obj.Add(nameof(Message), new JSONString(Message));
            obj.Add(nameof(Sender), Sender.ToJson());
            obj.Add(nameof(Channel), Channel.ToJson());
            JSONArray emotes = new JSONArray();
            foreach (var emote in Emotes)
            {
                emotes.Add(emote.ToJson());
            }
            obj.Add(nameof(Emotes), emotes);
            return obj;
        }
    }
}
