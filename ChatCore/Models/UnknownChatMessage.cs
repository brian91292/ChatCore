using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ChatCore.Models
{
    public class UnknownChatMessage : IChatMessage
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
        public ReadOnlyDictionary<string, string> Metadata { get; internal set; }

        public UnknownChatMessage(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.HasKey(nameof(Id))) { Id = obj[nameof(Id)].Value; }
            if (obj.HasKey(nameof(IsSystemMessage))) { IsSystemMessage = obj[nameof(IsSystemMessage)].AsBool; }
            if (obj.HasKey(nameof(IsActionMessage))) { IsActionMessage = obj[nameof(IsActionMessage)].AsBool; }
            if (obj.HasKey(nameof(IsHighlighted))) { IsHighlighted = obj[nameof(IsHighlighted)].AsBool; }
            if (obj.HasKey(nameof(IsPing))) { IsPing = obj[nameof(IsPing)].AsBool; }
            if (obj.HasKey(nameof(Message))) { Message = obj[nameof(Message)].Value; }
            if (obj.HasKey(nameof(Sender))) { Sender = new UnknownChatUser(obj[nameof(Sender)].ToString()); }
            if (obj.HasKey(nameof(Channel))) { Channel = new UnknownChatChannel(obj[nameof(Channel)].ToString()); }
            if (obj.HasKey(nameof(Emotes)))
            {
                List<IChatEmote> emotes = new List<IChatEmote>();
                foreach (var emote in obj[nameof(Emotes)].AsArray)
                {
                    if (emote.Value.HasKey(nameof(IChatEmote.Id)))
                    {
                        emotes.Add(new UnknownChatEmote(emote.Value.ToString()));
                    }
                }
                Emotes = emotes.ToArray();
            }
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
