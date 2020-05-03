using ChatCore.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChatCore.Interfaces
{
    public interface IChatBadge
    {
        string Id { get; }
        string Name { get; }
        string Uri { get; }
        JSONObject ToJson();
    }
}
