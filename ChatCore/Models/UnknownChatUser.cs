using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models
{
    public class UnknownChatUser : IChatUser
    {
        public string Id { get; internal set; }
        public string UserName { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Color { get; internal set; }
        public bool IsBroadcaster { get; internal set; }
        public bool IsModerator { get; internal set; }
        public IChatBadge[] Badges { get; internal set; }

        public UnknownChatUser() { }
        public UnknownChatUser(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.HasKey(nameof(Id))) { Id = obj[nameof(Id)].Value; }
            if (obj.HasKey(nameof(UserName))) { UserName = obj[nameof(UserName)].Value; }
            if (obj.HasKey(nameof(DisplayName))) { DisplayName = obj[nameof(DisplayName)].Value; }
            if (obj.HasKey(nameof(Color))) { Color = obj[nameof(Color)].Value; }
            if (obj.HasKey(nameof(IsBroadcaster))) { IsBroadcaster = obj[nameof(IsBroadcaster)].AsBool; }
            if (obj.HasKey(nameof(IsModerator))) { IsModerator = obj[nameof(IsModerator)].AsBool; }
            if (obj.HasKey(nameof(Badges)))
            {
                List<IChatBadge> badges = new List<IChatBadge>();
                foreach (var badge in obj["Badges"].AsArray)
                {
                    badges.Add(new UnknownChatBadge(badge.Value.ToString()));
                }
                Badges = badges.ToArray();
            }
        }
        public JSONObject ToJson()
        {
            JSONObject obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            obj.Add(nameof(UserName), new JSONString(UserName));
            obj.Add(nameof(DisplayName), new JSONString(DisplayName));
            obj.Add(nameof(Color), new JSONString(Color));
            obj.Add(nameof(IsBroadcaster), new JSONBool(IsBroadcaster));
            obj.Add(nameof(IsModerator), new JSONBool(IsModerator));
            JSONArray badges = new JSONArray();
            foreach (var badge in Badges)
            {
                badges.Add(badge.ToJson());
            }
            obj.Add(nameof(Badges), badges);
            return obj;
        }
    }
}
