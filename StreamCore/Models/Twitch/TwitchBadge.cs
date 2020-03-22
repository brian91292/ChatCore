using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Models.Twitch
{
    public class TwitchBadge : IChatBadge
    {
        public string Name { get; internal set; }
        public string Uri { get; internal set; }
    }
}
