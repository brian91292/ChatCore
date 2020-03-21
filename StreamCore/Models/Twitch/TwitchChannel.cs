using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Models.Twitch
{
    internal class TwitchChannel : IChatChannel
    {
        public string Id { get; internal set; }
    }
}
