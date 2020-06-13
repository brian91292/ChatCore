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
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(UserName), out var userName)) { UserName = userName.Value; }
            if (obj.TryGetKey(nameof(DisplayName), out var displayName)) { DisplayName = displayName.Value; }
            if (obj.TryGetKey(nameof(Color), out var color)) { Color = color.Value; }
            if (obj.TryGetKey(nameof(IsBroadcaster), out var isBroadcaster)) { IsBroadcaster = isBroadcaster.AsBool; }
            if (obj.TryGetKey(nameof(IsModerator), out var isModerator)) { IsModerator = isModerator.AsBool; }
            if (obj.TryGetKey(nameof(Badges), out var badges))
            {
                List<IChatBadge> badgeList = new List<IChatBadge>();
                foreach (var badge in badges.AsArray)
                {
                    badgeList.Add(new UnknownChatBadge(badge.Value.ToString()));
                }
                Badges = badgeList.ToArray();
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
