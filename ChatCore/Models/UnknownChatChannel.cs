using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models
{
    public class UnknownChatChannel : IChatChannel
    {
        public string Id { get; internal set; }

        public UnknownChatChannel() { }
        public UnknownChatChannel(string json)
        {
            JSONNode obj = JSON.Parse(json);
            if (obj.HasKey(nameof(Id))) { Id = obj[nameof(Id)].Value; }
        }
        public JSONObject ToJson()
        {
            JSONObject obj = new JSONObject();
            obj.Add(nameof(Id), new JSONString(Id));
            return obj;
        }
    }
}
