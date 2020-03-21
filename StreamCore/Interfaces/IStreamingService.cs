using System;
using System.Collections.Generic;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IStreamingService
    {
        Type ServiceType { get; }
        Action<IChatMessage> OnMessageReceived { get; set; }
        void SendTextMessage(string message, string channel = null);

        void SendCommand(string command, string channel = null);
    }
}
