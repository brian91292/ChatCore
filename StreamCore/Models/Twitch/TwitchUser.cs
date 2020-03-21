using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Models.Twitch
{
    public class TwitchUser : IChatUser
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Color { get; internal set; }
        public IChatBadge[] Badges { get; internal set; }
    }
}
