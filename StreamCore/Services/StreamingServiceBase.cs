using StreamCore.Interfaces;
using StreamCore;
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
        public event Action<IChatMessage> OnTextMessageReceived
        {
            add => _onTextMessageReceivedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onTextMessageReceivedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatChannel>> _onJoinRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IChatChannel>>();
        public event Action<IChatChannel> OnJoinChannel
        {
            add => _onJoinRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onJoinRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IChatChannel>> _onLeaveRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IChatChannel>>();
        public event Action<IChatChannel> OnLeaveChannel
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

        protected ConcurrentDictionary<Assembly, Action<IStreamingService>> _onLoginCallbacks = new ConcurrentDictionary<Assembly, Action<IStreamingService>>();
        public event Action<IStreamingService> OnLogin
        {
            add => _onLoginCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onLoginCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<string>> _onChatClearedCallbacks = new ConcurrentDictionary<Assembly, Action<string>>();
        public event Action<string> OnChatCleared
        {
            add => _onChatClearedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onChatClearedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<string>> _onMessageClearedCallbacks = new ConcurrentDictionary<Assembly, Action<string>>();
        public event Action<string> OnMessageCleared
        {
            add => _onMessageClearedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onMessageClearedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }
    }
}
