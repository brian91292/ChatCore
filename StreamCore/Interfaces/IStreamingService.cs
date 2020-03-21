using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingService
    {
        Type ServiceType { get; }
        event Action<IChatMessage> OnMessageReceived;

        void JoinChannel(string channel);
        void SendTextMessage(string message, string channel = null);
        void SendCommand(string command, string channel = null);
    }
}
