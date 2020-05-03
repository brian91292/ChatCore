using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace ChatCore.Models.Twitch
{
    public class TwitchRoomstate
    {
        public string BroadcasterLang { get; internal set; }
        public string RoomId { get; internal set; }
        public bool EmoteOnly { get; internal set; }
        public bool FollowersOnly { get; internal set; }
        public bool SubscribersOnly { get; internal set; }
        public bool R9K { get; internal set; }

        /// <summary>
        /// The number of seconds a chatter without moderator privileges must wait between sending messages
        /// </summary>
        public int SlowModeInterval { get; internal set; }

        /// <summary>
        /// If FollowersOnly is true, this specifies the number of minutes a user must be following before they can chat.
        /// </summary>
        public int MinFollowTime { get; internal set; }

        public TwitchRoomstate() { }
        public TwitchRoomstate(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(BroadcasterLang), out var broadcasterLang)) { BroadcasterLang = broadcasterLang.Value; }
            if (obj.TryGetKey(nameof(RoomId), out var roomId )) { RoomId = roomId.Value; }
            if (obj.TryGetKey(nameof(EmoteOnly), out var emoteOnly)) { EmoteOnly = emoteOnly.AsBool; }
            if (obj.TryGetKey(nameof(FollowersOnly), out var followersOnly)) { FollowersOnly = followersOnly.AsBool; }
            if (obj.TryGetKey(nameof(SubscribersOnly), out var subscribersOnly)) { SubscribersOnly = subscribersOnly.AsBool; }
            if (obj.TryGetKey(nameof(R9K), out var r9k)) { R9K = r9k.AsBool; }
            if (obj.TryGetKey(nameof(SlowModeInterval), out var slowModeInterval)) { SlowModeInterval = slowModeInterval.AsInt; }
            if (obj.TryGetKey(nameof(MinFollowTime), out var minFollowTime)) { MinFollowTime = minFollowTime.AsInt; }
        }
        public JSONObject ToJson()
        {
            JSONObject obj = new JSONObject();
            obj.Add(nameof(BroadcasterLang), new JSONString(BroadcasterLang));
            obj.Add(nameof(RoomId), new JSONString(RoomId));
            obj.Add(nameof(EmoteOnly), new JSONBool(EmoteOnly));
            obj.Add(nameof(FollowersOnly), new JSONBool(FollowersOnly));
            obj.Add(nameof(SubscribersOnly), new JSONBool(SubscribersOnly));
            obj.Add(nameof(R9K), new JSONBool(R9K));
            return obj;
        }
    }
}
