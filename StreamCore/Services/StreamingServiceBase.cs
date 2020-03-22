using StreamCore.Interfaces;
using StreamCore.Utilities;
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
            add => _onMessageReceivedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onMessageReceivedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatChannel>> _onJoinChannelCallbacks = new ConcurrentDictionary<Assembly, Action<IChatChannel>>();
        public event Action<IChatChannel> OnJoinChannel
        {
            add => _onJoinChannelCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onJoinChannelCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatChannel>> _onLeaveChannelCallbacks = new ConcurrentDictionary<Assembly, Action<IChatChannel>>();
        public event Action<IChatChannel> OnLeaveChannel
        {
            add => _onLeaveChannelCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onLeaveChannelCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatChannel>> _onChannelStateUpdatedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatChannel>>();
        public event Action<IChatChannel> OnChannelStateUpdated
        {
            add => _onChannelStateUpdatedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onChannelStateUpdatedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }
    }
}
