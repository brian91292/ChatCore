using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IChatMessageParser
    {
        bool ParseRawMessage(string rawMessage, out IChatMessage[] parsedMessage);
    }
}
