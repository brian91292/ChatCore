using StreamCore.Interfaces;
using StreamCore.Models.Twitch;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Utilities
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
    }
}
