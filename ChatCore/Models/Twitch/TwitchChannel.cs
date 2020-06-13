using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace ChatCore.Models.Twitch
{
    public class TwitchChannel : IChatChannel
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public TwitchRoomstate Roomstate { get; internal set; }

        public TwitchChannel() { }
        public TwitchChannel(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if (obj.TryGetKey(nameof(Roomstate), out var roomstate)) { Roomstate = new TwitchRoomstate(roomstate.ToString()); }
        }
        public JSONObject ToJson()
        {
            JSONObject obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            obj.Add(nameof(Roomstate), Roomstate.ToJson());
            return obj;
        }
    }
}
