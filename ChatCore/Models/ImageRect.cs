using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models
{
    public struct ImageRect
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public ImageRect(string json)
        {
            var obj = JSON.Parse(json);
            if (obj != null)
            {
                x = obj.TryGetKey(nameof(x), out var xJson) ? xJson.AsInt : 0;
                y = obj.TryGetKey(nameof(y), out var yJson) ? yJson.AsInt : 0;
                width = obj.TryGetKey(nameof(width), out var w) ? w.AsInt : 0;
                height = obj.TryGetKey(nameof(x), out var h) ? h.AsInt : 0;
            }
            x = 0;
            y = 0;
            width = 0;
            height = 0;
        }
        public JSONObject ToJson()
        {
            var json = new JSONObject();
            json.Add(nameof(x), new JSONNumber(x));
            json.Add(nameof(y), new JSONNumber(y));
            json.Add(nameof(width), new JSONNumber(width));
            json.Add(nameof(height), new JSONNumber(height));
            return json;
        }

        public static ImageRect None = new ImageRect()
        {
            x = 0,
            y = 0,
            width = 0,
            height = 0
        };
    }
}
