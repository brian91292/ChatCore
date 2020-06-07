using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChatCore.Models.Mixer
{
    public class MixerChannelDetails
    {
        public string authKey = "";
        public string[] endpoints = new string[0];
        public string[] permissions = new string[0];

        public MixerChannelDetails() { }
        public MixerChannelDetails(string json)
        {
            FromJson(json);
        }

        public string ToJson()
        {
            JSONObject json = new JSONObject();
            json.Add("authKey", new JSONString(authKey));
            json.Add("endpoints", new JSONArray(endpoints));
            json.Add("permissions", new JSONArray(permissions));
            return json.ToString();
        }

        public void FromJson(string jsonData)
        {
            var json = JSON.Parse(jsonData);
            if (json == null)
            {
                return;
            }
            if (json.TryGetKey("authKey", out var ak))
            {
                authKey = ak.Value;
            }
            if (json.TryGetKey("endpoints", out var ep))
            {
                endpoints = ep.AsArray.List.Select(r => r.Value).ToArray();
            }
            if (json.TryGetKey("permissions", out var p))
            {
                permissions = p.AsArray.List.Select(r => r.Value).ToArray();
            }
        }
    }
}
