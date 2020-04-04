using StreamCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Models.Twitch
{
    public class TwitchEmote : IChatEmote
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        public string Uri { get; internal set; }
        public int StartIndex { get; internal set; }
        public int EndIndex { get; internal set; }
        public bool IsAnimated { get; internal set; }

        //public EmoteInstance[] Instances { get; internal set; }
    }
}
