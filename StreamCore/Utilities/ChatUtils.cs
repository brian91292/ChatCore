using StreamCore.Interfaces;
using StreamCore.Models.Twitch;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore
{
    public static class ChatUtils
    {
        public static TwitchMessage AsTwitchMessage(this IChatMessage msg)
        {
            return msg as TwitchMessage;
        }

        public static TwitchChannel AsTwitchChannel(this IChatChannel channel)
        {
            return channel as TwitchChannel;
        }

        public static TwitchUser AsTwitchUser(this IChatUser user)
        {
            return user as TwitchUser;
        }

        public static TwitchBadge AsTwitchBadge(this IChatBadge badge)
        {
            return badge as TwitchBadge;
        }

        public static TwitchEmote AsTwitchEmote(this IChatEmote emote)
        {
            return emote as TwitchEmote;
        }
    }
}
