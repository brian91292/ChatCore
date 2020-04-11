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
        protected ConcurrentDictionary<Assembly, Action<IStreamingService, IChatMessage>> _onTextMessageReceivedCallbacks = new ConcurrentDictionary<Assembly, Action<IStreamingService, IChatMessage>>();
        public event Action<IStreamingService, IChatMessage> OnTextMessageReceived
        {
            add => _onTextMessageReceivedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onTextMessageReceivedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IStreamingService, IChatChannel>> _onJoinRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IStreamingService, IChatChannel>>();
        public event Action<IStreamingService, IChatChannel> OnJoinChannel
        {
            add => _onJoinRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onJoinRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IStreamingService, IChatChannel>> _onLeaveRoomCallbacks = new ConcurrentDictionary<Assembly, Action<IStreamingService, IChatChannel>>();
        public event Action<IStreamingService, IChatChannel> OnLeaveChannel
        {
            add => _onLeaveRoomCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onLeaveRoomCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IStreamingService, IChatChannel>> _onRoomStateUpdatedCallbacks = new ConcurrentDictionary<Assembly, Action<IStreamingService, IChatChannel>>();
        public event Action<IStreamingService, IChatChannel> OnRoomStateUpdated
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

        protected ConcurrentDictionary<Assembly, Action<IStreamingService, string>> _onChatClearedCallbacks = new ConcurrentDictionary<Assembly, Action<IStreamingService, string>>();
        public event Action<IStreamingService, string> OnChatCleared
        {
            add => _onChatClearedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onChatClearedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }

        protected ConcurrentDictionary<Assembly, Action<IStreamingService, string>> _onMessageClearedCallbacks = new ConcurrentDictionary<Assembly, Action<IStreamingService, string>>();
        public event Action<IStreamingService, string> OnMessageCleared
        {
            add => _onMessageClearedCallbacks.AddAction(Assembly.GetCallingAssembly(), value);
            remove => _onMessageClearedCallbacks.RemoveAction(Assembly.GetCallingAssembly(), value);
        }
    }
}
