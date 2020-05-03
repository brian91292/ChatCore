using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models
{
    public class UnknownChatEmote : IChatEmote
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Uri { get; internal set; }
        public int StartIndex { get; internal set; }
        public int EndIndex { get; internal set; }
        public bool IsAnimated { get; internal set; }

        public UnknownChatEmote() { }
        public UnknownChatEmote(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.HasKey(nameof(Id))) { Id = obj[nameof(Id)].Value; }
            if (obj.HasKey(nameof(Name))) { Name = obj[nameof(Name)].Value; }
            if (obj.HasKey(nameof(Uri))) { Uri = obj[nameof(Uri)].Value; }
            if (obj.HasKey(nameof(StartIndex))) { StartIndex = obj[nameof(Id)].AsInt; }
            if (obj.HasKey(nameof(EndIndex))) { EndIndex = obj[nameof(EndIndex)].AsInt; }
            if (obj.HasKey(nameof(IsAnimated))) { IsAnimated = obj[nameof(IsAnimated)].AsBool; }
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
            return obj;
        }
    }
}
