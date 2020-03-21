using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Models.Twitch
{
    public class TwitchMessage : IChatMessage
    {
        public string Message { get; internal set; }

        public string Author { get; internal set; }

        public string Type { get; internal set; }
    }
}
