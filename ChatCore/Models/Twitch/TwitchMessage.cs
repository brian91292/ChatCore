using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ChatCore.Models.Twitch
{
    public class TwitchMessage : IChatMessage, ICloneable
    {
        public string Id { get; internal set; }
        public string Message { get; internal set; }
        public bool IsSystemMessage { get; internal set; }
        public bool IsActionMessage { get; internal set; }
        public bool IsHighlighted { get; internal set; }
        public bool IsPing { get; internal set; }
        public IChatUser Sender { get; internal set; }
        public IChatChannel Channel { get; internal set; }
        public IChatEmote[] Emotes { get; internal set; }
        public ReadOnlyDictionary<string, string> Metadata { get; internal set; }
        /// <summary>
        /// The IRC message type for this TwitchMessage
        /// </summary>
        public string Type { get; internal set; }
        /// <summary>
        /// The number of bits in this message, if any.
        /// </summary>
        public int Bits { get; internal set; }

        public TwitchMessage() { }
        public TwitchMessage(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(IsSystemMessage), out var isSystemMessage)) { IsSystemMessage = isSystemMessage.AsBool; }
            if (obj.TryGetKey(nameof(IsActionMessage), out var isActionMessage)) { IsActionMessage = isActionMessage.AsBool; }
            if (obj.TryGetKey(nameof(IsHighlighted), out var isHighlighted)) { IsHighlighted = isHighlighted.AsBool; }
            if (obj.TryGetKey(nameof(IsPing), out var isPing)) { IsPing = isPing.AsBool; }
            if (obj.TryGetKey(nameof(Message), out var message)) { Message = message.Value; }
            if (obj.TryGetKey(nameof(Sender), out var sender)) { Sender = new TwitchUser(sender.ToString()); }
            if (obj.TryGetKey(nameof(Channel), out var channel)) { Channel = new TwitchChannel(channel.ToString()); }
            if (obj.TryGetKey(nameof(Emotes), out var emotes))
            {
                List<IChatEmote> emoteList = new List<IChatEmote>();
                foreach (var emote in emotes.AsArray)
                {
                    if(emote.Value.TryGetKey(nameof(IChatEmote.Id), out var emoteNode))
                    {
                        var emoteId = emoteNode.Value;
                        if (emoteId.StartsWith("Twitch") || emoteId.StartsWith("BTTV") || emoteId.StartsWith("FFZ"))
                        {
                            emoteList.Add(new TwitchEmote(emote.Value.ToString()));
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
            if (obj.TryGetKey(nameof(Type), out var type)) { Type = type.Value; }
            if (obj.TryGetKey(nameof(Bits), out var bits)) { Bits = bits.AsInt; }
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
            obj.Add(nameof(Type), new JSONString(Type));
            obj.Add(nameof(Bits), new JSONNumber(Bits));
            return obj;
        }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
