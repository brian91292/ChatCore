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
        public event Action<IChatMessage> OnMessageReceived
        {
            add
            {
                var assembly = Assembly.GetCallingAssembly();
                if (!_onMessageReceivedCallbacks.TryGetValue(assembly, out var action))
                {
                    action = new Action<IChatMessage>(value);
                    _onMessageReceivedCallbacks[assembly] = action;
                }
                action += value;
            }
            remove
            {
                var assembly = Assembly.GetCallingAssembly();
                if (!_onMessageReceivedCallbacks.TryGetValue(assembly, out var action))
                {
                    return;
                }
                action -= value;
            }
        }
    }
}
