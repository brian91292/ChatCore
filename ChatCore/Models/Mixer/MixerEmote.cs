using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.Mixer
{
    public class MixerEmote : IChatEmote
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Uri { get; internal set; }
        public int StartIndex { get; internal set; }
        public int EndIndex { get; internal set; }
        public bool IsAnimated { get; internal set; }
        public EmoteType Type { get; internal set; } = EmoteType.SingleImage;
        public ImageRect UVs { get; internal set; }

        public MixerEmote() { }
        public MixerEmote(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(Name), out var name)) { Name = name.Value; }
            if (obj.TryGetKey(nameof(Uri), out var uri)) { Uri = uri.Value; }
            if (obj.TryGetKey(nameof(StartIndex), out var startIndex)) { StartIndex = startIndex.AsInt; }
            if (obj.TryGetKey(nameof(EndIndex), out var endIndex)) { EndIndex = endIndex.AsInt; }
            if (obj.TryGetKey(nameof(IsAnimated), out var isAnimated)) { IsAnimated = isAnimated.AsBool; }
            if (obj.TryGetKey(nameof(Type), out var type)) { Type = Enum.TryParse<EmoteType>(type.Value, out var typeEnum) ? typeEnum : EmoteType.SingleImage; }
            if (obj.TryGetKey(nameof(UVs), out var uvs)) { UVs = new ImageRect(uvs.Value); }
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
            obj.Add(nameof(Type), new JSONString(Type.ToString()));
            obj.Add(nameof(UVs), UVs.ToJson());
            return obj;
        }
    }
}
