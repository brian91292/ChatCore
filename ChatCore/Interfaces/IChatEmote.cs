using ChatCore.Models;
using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IChatEmote
    {
        string Id { get; }
        string Name { get; }
        string Uri { get; }
        int StartIndex { get; }
        int EndIndex { get; }
        bool IsAnimated { get; }
        JSONObject ToJson();
    }
}
