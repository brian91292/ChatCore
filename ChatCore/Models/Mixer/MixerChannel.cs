using ChatCore.Interfaces;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.Mixer
{
    public class MixerChannel : IChatChannel
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }

        public IWebSocketService Socket { get; internal set; }

        public MixerChannel() { }
        public MixerChannel(string json)
        {
            var obj = JSON.Parse(json);
            if(obj.TryGetKey(nameof(Id), out var id)) { Id = id.Value; }
            if(obj.TryGetKey(nameof(Name), out var name)) { Name = name.Value; }
        }

        public JSONObject ToJson()
        {
            JSONObject json = new JSONObject();
            json.Add(nameof(Id), new JSONString(Id));
            json.Add(nameof(Name), new JSONString(Name));
            return json;
        }
    }
}
