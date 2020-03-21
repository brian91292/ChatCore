using StreamCore.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StreamCore.Services
{
    public class StreamingServiceBase
    {
        protected ConcurrentDictionary<Assembly, Action<IChatMessage>> _onMessageReceivedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatMessage>>();
        public Action<IChatMessage> OnMessageReceived
        {
            get => _onMessageReceivedCallbacks.TryGetValue(Assembly.GetCallingAssembly(), out var callback) ? callback : null;
            set => _onMessageReceivedCallbacks[Assembly.GetCallingAssembly()] = value;
        }
    }
}
