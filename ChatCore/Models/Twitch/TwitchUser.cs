using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.Twitch
{
    public class TwitchUser : IChatUser
    {
        public string Id { get; internal set; }
        public string UserName { get; internal set; }
        public string DisplayName { get; internal set; }
        public string Color { get; internal set; }
        public bool IsModerator { get; internal set; }
        public bool IsBroadcaster { get; internal set; }
        public bool IsSubscriber { get; internal set; }
        public bool IsTurbo { get; internal set; }
        public bool IsVip { get; internal set; }
        public IChatBadge[] Badges { get; internal set; }

        public TwitchUser() { }
        public TwitchUser(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(UserName), out var username)) { UserName = username.Value; }
            if (obj.TryGetKey(nameof(DisplayName), out var displayName)) { DisplayName = displayName.Value; }
            if (obj.TryGetKey(nameof(Color), out var color)) { Color = color.Value; }
            if (obj.TryGetKey(nameof(IsBroadcaster), out var isBroadcaster)) { IsBroadcaster = isBroadcaster.AsBool; }
            if (obj.TryGetKey(nameof(IsModerator), out var isModerator)) { IsModerator = isModerator.AsBool; }
            if (obj.TryGetKey(nameof(Badges), out var badges))
            {
                List<IChatBadge> badgeList = new List<IChatBadge>();
                foreach (var badge in badges.AsArray)
                {
                    badgeList.Add(new TwitchBadge(badge.ToString()));
                }
                Badges = badgeList.ToArray();
            }
            if (obj.TryGetKey(nameof(IsSubscriber), out var isSubscriber)) { IsSubscriber = isSubscriber.AsBool; }
            if (obj.TryGetKey(nameof(IsTurbo), out var isTurbo)) { IsTurbo = isTurbo.AsBool; }
            if (obj.TryGetKey(nameof(IsVip), out var isVip)) { IsVip = isVip.AsBool; }
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
            obj.Add(nameof(IsSubscriber), new JSONBool(IsSubscriber));
            obj.Add(nameof(IsTurbo), new JSONBool(IsTurbo));
            obj.Add(nameof(IsVip), new JSONBool(IsVip));
            return obj;
        }
    }
}
