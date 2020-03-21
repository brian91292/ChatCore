using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingService
    {
        Type ServiceType { get; }
        Action<IChatMessage> OnMessageReceived { get; set; }

        void JoinChannel(string channel);
        void SendRawMessage(string rawMessage);
        void SendTextMessage(string message, string channel = null);
        void SendCommand(string command, string channel = null);
    }
}
