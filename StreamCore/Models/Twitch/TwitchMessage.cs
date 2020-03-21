using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace StreamCore.Models.Twitch
{
    public class TwitchMessage : IChatMessage
    {
        public string Message { get; internal set; }

        public string Type { get; internal set; }

        public IChatUser Sender { get; internal set; }

        public IChatChannel Channel { get; internal set; }

        public ReadOnlyDictionary<string, string> Metadata { get; internal set; }
    }
}
