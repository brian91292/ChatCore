using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IWebSocketService
    {
        bool IsConnected { get; }
        bool AutoReconnect { get; set; }
        int ReconnectDelay { get; set; }

        event Action OnOpen;
        event Action OnClose;
        event Action OnError;
        event Action<Assembly, string> OnMessageReceived;

        void Connect(string uri);
        void Disconnect();
        void SendMessage(string message);
    }
}
