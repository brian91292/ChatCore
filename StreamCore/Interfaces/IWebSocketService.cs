using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Interfaces
{
    public interface IWebSocketService
    {
        bool IsConnected { get; }
        event Action OnOpen;
        event Action OnClose;
        event Action OnError;
        event Action<Assembly, string> OnMessageReceived;
        void Connect(string uri, int port = 443);
        void Disconnect();
        void SendMessage(string message);
    }
}
