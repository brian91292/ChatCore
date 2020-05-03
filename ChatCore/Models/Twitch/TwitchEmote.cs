using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.Twitch
{
    public class TwitchEmote : IChatEmote
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Uri { get; internal set; }
        public int StartIndex { get; internal set; }
        public int EndIndex { get; internal set; }
        public bool IsAnimated { get; internal set; }
        /// <summary>
        /// The number of bits associated with this emote (probably a cheermote)
        /// </summary>
        public int Bits { get; internal set; }
        /// <summary>
        /// If there are bits associated with this emote, this is the color the bits text should be.
        /// </summary>
        public string Color { get; internal set; }

        public TwitchEmote() { }
        public TwitchEmote(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(Name), out var name)) { Name = name.Value; }
            if (obj.TryGetKey(nameof(Uri), out var uri)) { Uri = uri.Value; }
            if (obj.TryGetKey(nameof(StartIndex), out var startIndex)) { StartIndex = startIndex.AsInt; }
            if (obj.TryGetKey(nameof(EndIndex), out var endIndex)) { EndIndex = endIndex.AsInt; }
            if (obj.TryGetKey(nameof(IsAnimated), out var isAnimated)) { IsAnimated = isAnimated.AsBool; }
            if (obj.TryGetKey(nameof(Bits), out var bits)) { Bits = bits.AsInt; }
            if (obj.TryGetKey(nameof(Color), out var color)) { Color = color.Value; }
        }
        public JSONObject ToJson()
        {
            JSONObject obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            obj.Add(nameof(Name), new JSONString(Name));
            obj.Add(nameof(Uri), new JSONString(Uri));
            obj.Add(nameof(StartIndex), new JSONNumber(StartIndex));
            obj.Add(nameof(EndIndex), new JSONNumber(EndIndex));
            obj.Add(nameof(IsAnimated), new JSONBool(IsAnimated));
            obj.Add(nameof(Bits), new JSONNumber(Bits));
            obj.Add(nameof(Color), new JSONString(Color));
            return obj;
        }
    }
}
