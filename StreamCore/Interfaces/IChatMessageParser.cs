using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IChatMessageParser
    {
        bool ParseRawMessage(string rawMessage, ConcurrentDictionary<string, IChatChannel> channelInfo, out IChatMessage[] parsedMessage);
    }
}
