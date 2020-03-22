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
        protected ConcurrentDictionary<Assembly, Action<IChatMessage>> _onTextMessageReceivedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatMessage>>();
        public event Action<IChatMessage> OnMessageReceived
        {
            add => _onTextMessageReceivedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onTextMessageReceivedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatChannel>> _onJoinRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IChatChannel>>();
        public event Action<IChatChannel> OnJoinRoom
        {
            add => _onJoinRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onJoinRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatChannel>> _onLeaveRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IChatChannel>>();
        public event Action<IChatChannel> OnLeaveRoom
        {
            add => _onLeaveRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onLeaveRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatChannel>> _onRoomStateUpdatedCallbacks = new ConcurrentDictionary<Assembly, Action<IChatChannel>>();
        public event Action<IChatChannel> OnRoomStateUpdated
        {
            add => _onRoomStateUpdatedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onRoomStateUpdatedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }
    }
}
