using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.Twitch
{
    public class TwitchBadge : IChatBadge
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Uri { get; internal set; }

        public TwitchBadge() { }
        public TwitchBadge(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.HasKey(nameof(Id))) { Id = obj[nameof(Id)].Value; }
            if (obj.HasKey(nameof(Name))) { Name = obj[nameof(Name)].Value; }
            if (obj.HasKey(nameof(Uri))) { Uri = obj[nameof(Uri)].Value; }
        }
        public JSONObject ToJson()
        {
            JSONObject obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            obj.Add(nameof(Name), new JSONString(Name));
            obj.Add(nameof(Uri), new JSONString(Uri));
            return obj;
        }
    }
}
