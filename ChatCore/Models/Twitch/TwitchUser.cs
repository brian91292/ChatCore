using ChatCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models.Twitch
{
    public class TwitchUser : IChatUser
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Color { get; internal set; }
        public bool IsModerator { get; internal set; }
        public bool IsBroadcaster { get; internal set; }
        public bool IsSubscriber { get; internal set; }
        public bool IsTurbo { get; internal set; }
        public bool IsVip { get; internal set; }
        public IChatBadge[] Badges { get; internal set; }
    }
}
