using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
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
            if (obj.HasKey(nameof(BroadcasterLang))) { BroadcasterLang = obj[nameof(BroadcasterLang)].Value; }
            if (obj.HasKey(nameof(RoomId))) { RoomId = obj[nameof(RoomId)].Value; }
            if (obj.HasKey(nameof(EmoteOnly))) { EmoteOnly = obj[nameof(EmoteOnly)].AsBool; }
            if (obj.HasKey(nameof(FollowersOnly))) { FollowersOnly = obj[nameof(FollowersOnly)].AsBool; }
            if (obj.HasKey(nameof(SubscribersOnly))) { SubscribersOnly = obj[nameof(SubscribersOnly)].AsBool; }
            if (obj.HasKey(nameof(R9K))) { R9K = obj[nameof(R9K)].AsBool; }
            if (obj.HasKey(nameof(SlowModeInterval))) { SlowModeInterval = obj[nameof(SlowModeInterval)].AsInt; }
            if (obj.HasKey(nameof(MinFollowTime))) { MinFollowTime = obj[nameof(MinFollowTime)].AsInt; }
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
