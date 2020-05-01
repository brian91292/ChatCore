using ChatCore.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChatCore.Models
{
    public class ChatResourceData : IChatResourceData
    {
        public string Uri { get; internal set; }
        public bool IsAnimated { get; internal set; }
        public string Type { get; internal set; }
    }
}
